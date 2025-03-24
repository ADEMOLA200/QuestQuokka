using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuestQuokka.Models
{
    public class TriviaQuestion
    {
        [JsonProperty("question")]
        public string Question { get; set; } = string.Empty;

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [JsonProperty("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; } = new();
    }
}
