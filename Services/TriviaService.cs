using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using QuestQuokka.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class TriviaService
    {
        private readonly DatabaseService _db;
        private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
        private readonly Random _rng = new();
        
        private static readonly ConcurrentDictionary<ulong, (CancellationTokenSource, string, List<string>)> _activeQuestions = new();

        public TriviaService(DatabaseService db) => _db = db;

        public async Task TriviaCommand(SocketInteraction interaction)
        {
            await interaction.DeferAsync();

            var apiUrl = "https://opentdb.com/api.php?amount=1&encode=url3986";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await _httpClient.GetStringAsync(apiUrl, cts.Token);
            var result = JsonConvert.DeserializeObject<OpenTdbResponse>(response);
            
            if (result?.Results == null || result.Results.Count == 0)
            {
                await interaction.FollowupAsync("❌ Failed to fetch trivia question", ephemeral: true);
                return;
            }

            var question = result.Results[0];
            var correctAnswer = Uri.UnescapeDataString(question.CorrectAnswer);
            var incorrectAnswers = question.IncorrectAnswers.Select(Uri.UnescapeDataString).ToList();
            var options = new List<string> { correctAnswer };
            options.AddRange(incorrectAnswers);
            Shuffle(options);
            
            int correctIndex = options.IndexOf(correctAnswer);
            var questionId = Guid.NewGuid().ToString();
            var builder = new ComponentBuilder();
            
            for (int i = 0; i < options.Count; i++)
            {
                builder.WithButton(options[i], $"trivia_answer_{questionId}_{i}_{correctIndex}", row: i / 2);
            }

            var message = await interaction.FollowupAsync($"**{Uri.UnescapeDataString(question.Question)}**", components: builder.Build());
            var messageCts = new CancellationTokenSource();
            
            _activeQuestions.TryAdd(message.Id, (messageCts, questionId, options));

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300000, messageCts.Token);
                    await message.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
                    _activeQuestions.TryRemove(message.Id, out _);
                }
                catch (TaskCanceledException) { }
            });
        }

        public async Task HandleTriviaAnswer(SocketInteraction interaction, string id)
        {
            await interaction.DeferAsync(ephemeral: true);
            var parts = id.Split('_');
            Console.WriteLine($"[DEBUG] Trivia answer button ID: {id} (parts.Length = {parts.Length})");
            
            if (parts.Length != 3)
            {
                await interaction.FollowupAsync("❌ Invalid question format!", ephemeral: true);
                return;
            }

            if (interaction is not SocketMessageComponent component)
            {
                await interaction.FollowupAsync("❌ Error processing interaction!", ephemeral: true);
                return;
            }

            if (!_activeQuestions.TryGetValue(component.Message.Id, out var questionData))
            {
                await interaction.FollowupAsync("❌ This question has expired!", ephemeral: true);
                return;
            }

            var (cts, questionId, options) = questionData;
            if (parts[0] != questionId)
            {
                await interaction.FollowupAsync("❌ This answer is for an expired question!", ephemeral: true);
                return;
            }

            cts.Cancel();
            _activeQuestions.TryRemove(component.Message.Id, out _);

            var selectedIndex = int.Parse(parts[1]);
            var correctIndex = int.Parse(parts[2]);
            
            if (selectedIndex == correctIndex)
            {
                await _db.UpdateScore(interaction.User.Id, 10);
                await interaction.FollowupAsync("✅ Correct! +10 points!", ephemeral: true);
            }
            else
            {
                await interaction.FollowupAsync($"❌ Incorrect! The correct answer was: {options[correctIndex]}", ephemeral: true);
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public class OpenTdbResponse
        {
            [JsonProperty("results")]
            public List<TriviaQuestion> Results { get; set; } = new();
        }
    }
}
