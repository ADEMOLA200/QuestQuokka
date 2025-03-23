using Discord;
using Discord.Commands;
using Discord.Interactions;
using Newtonsoft.Json;
using QuestQuokka.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class TriviaService : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseContext _db;
    private readonly HttpClient _httpClient = new HttpClient();

    public TriviaService(DatabaseContext db)
    {
        _db = db;
    }

    [Command("trivia")]
    public async Task TriviaCommand()
    {
        var response = await _httpClient.GetStringAsync("https://opentdb.com/api.php?amount=1");
        var result = JsonConvert.DeserializeObject<OpenTdbResponse>(response);

        if (result?.Results == null || result.Results.Count == 0)
        {
            await ReplyAsync("❌ Failed to fetch a trivia question. Please try again later!");
            return;
        }

        var question = result.Results[0];

        var options = new List<string> { question.CorrectAnswer };
        options.AddRange(question.IncorrectAnswers);
        options = Shuffle(options);

        var builder = new ComponentBuilder();
        for (int i = 0; i < options.Count; i++)
        {
            builder.WithButton(options[i], $"trivia_{i}", row: i / 2);
        }

        await ReplyAsync($"**Trivia Question**: {question.Question}", components: builder.Build());
    }

    private List<string> Shuffle(List<string> list)
    {
        var rng = new Random();
        return list.OrderBy(x => rng.Next()).ToList();
    }

    public class OpenTdbResponse
    {
        public List<TriviaQuestion> Results { get; set; }
    }
}
