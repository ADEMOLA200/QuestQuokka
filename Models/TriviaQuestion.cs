namespace QuestQuokka.Models
{
    public class TriviaQuestion
    {
        public string Question { get; set; }
        public string CorrectAnswer { get; set; }
        public List<string> IncorrectAnswers { get; set; }

        public TriviaQuestion()
        {
            IncorrectAnswers = new List<string>();
        }
    }
}
