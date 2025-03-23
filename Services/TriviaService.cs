using Discord;
using Discord.Commands;
using Discord.Interactions;
using Newtonsoft.Json;
using QuestQuokka.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class TriviaService : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _db;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Random _rng = new Random();
        private readonly Dictionary<ulong, (CancellationTokenSource, string)> _activeQuestions = new();

        private static readonly Dictionary<int, string> Categories = new()
        {
            { 9, "General Knowledge" },
            { 10, "Books" },
            { 11, "Film" },
            { 17, "Science & Nature" }
        };

        public TriviaService(DatabaseService db)
        {
            _db = db;
        }

        [Command("trivia")]
        public async Task TriviaCommand(string category = null)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    var categoryBuilder = new ComponentBuilder();
                    foreach (var cat in Categories.Take(5))
                    {
                        categoryBuilder.WithButton(cat.Value, $"trivia_category_{cat.Key}", row: 0);
                    }
                    await ReplyAsync("**Choose a category:**", components: categoryBuilder.Build());
                    return;
                }

                var categoryId = Categories.FirstOrDefault(c => c.Value.Equals(category, StringComparison.OrdinalIgnoreCase)).Key;
                var apiUrl = $"https://opentdb.com/api.php?amount=1&encode=url3986{(categoryId > 0 ? $"&category={categoryId}" : "")}";

                var response = await _httpClient.GetStringAsync(apiUrl);
                var result = JsonConvert.DeserializeObject<OpenTdbResponse>(response);

                if (result?.Results == null || result.Results.Count == 0)
                {
                    await ReplyAsync("❌ Failed to fetch trivia question. Try again later!");
                    return;
                }

                var question = result.Results[0];
                var correctAnswer = Uri.UnescapeDataString(question.CorrectAnswer);
                var incorrectAnswers = question.IncorrectAnswers.Select(ans => Uri.UnescapeDataString(ans)).ToList();

                var options = new List<string> { correctAnswer };
                options.AddRange(incorrectAnswers);
                Shuffle(options);
                
                int correctIndex = options.IndexOf(correctAnswer);
                var questionId = Guid.NewGuid().ToString();

                var builder = new ComponentBuilder();
                for (int i = 0; i < options.Count; i++)
                {
                    builder.WithButton(options[i], $"trivia_{questionId}_{i}_{correctIndex}", row: i / 2);
                }

                var message = await ReplyAsync($"**{Uri.UnescapeDataString(question.Question)}**", 
                    components: builder.Build());

                var cts = new CancellationTokenSource();
                _activeQuestions[message.Id] = (cts, questionId);
                
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await Task.Delay(30000, cts.Token);
                        await message.ModifyAsync(m => m.Components = new ComponentBuilder().Build());
                        _activeQuestions.Remove(message.Id);
                    }
                    catch (TaskCanceledException) { /* Timeout completed */ }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Trivia Error: {ex}");
                await ReplyAsync("❌ Error processing trivia question!");
            }
        }

        [ComponentInteraction("trivia_*")]
        public async Task HandleTriviaAnswer(string id)
        {
            var parts = id.Split('_');
            if (parts.Length < 4 || !_activeQuestions.TryGetValue(Context.Interaction.Message.Id, out var questionData))
            {
                await RespondAsync("❌ This question has expired!", ephemeral: true);
                return;
            }

            var (cts, questionId) = questionData;
            if (parts[1] != questionId)
            {
                await RespondAsync("❌ This answer is for an expired question!", ephemeral: true);
                return;
            }

            cts.Cancel();
            _activeQuestions.Remove(Context.Interaction.Message.Id);

            var selectedIndex = int.Parse(parts[2]);
            var correctIndex = int.Parse(parts[3]);

            if (selectedIndex == correctIndex)
            {
                await _db.UpdateScore(Context.User.Id, 10);
                await RespondAsync("✅ Correct! +10 points!", ephemeral: true);
            }
            else
            {
                await RespondAsync($"❌ Incorrect! The correct answer was: {correctIndex + 1}", ephemeral: true);
            }
        }

        [ComponentInteraction("trivia_category_*")]
        public async Task HandleCategorySelection(string categoryId)
        {
            if (!int.TryParse(categoryId, out var id) || !Categories.ContainsKey(id))
            {
                await RespondAsync("❌ Invalid category!", ephemeral: true);
                return;
            }

            await DeferAsync();
            await TriviaCommand(Categories[id]);
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
            public List<TriviaQuestion> Results { get; set; }
        }
    }
}
