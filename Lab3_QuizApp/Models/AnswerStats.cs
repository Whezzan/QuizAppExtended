namespace QuizAppExtended.Models
{
    public class AnswerStats
    {
        public int TotalAnswers { get; set; }
        public Dictionary<string, int> CountsByAnswer { get; set; } = new Dictionary<string, int>();
    }
}