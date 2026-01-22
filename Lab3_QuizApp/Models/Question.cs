using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace QuizAppExtended.Models
{
    [BsonIgnoreExtraElements]
    internal class Question
    {
        [BsonElement("Query")]
        public string Query { get; set; }

        [BsonElement("CorrectAnswer")]
        public string CorrectAnswer { get; set; }

        [BsonElement("IncorrectAnswers")]
        public string[] IncorrectAnswers { get; set; }

        [JsonConstructor]
        public Question(string query, string correctAnswer, string[] incorrectAnswers)
        {
            Query = query;
            CorrectAnswer = correctAnswer;
            IncorrectAnswers = incorrectAnswers;
        }

        public Question(string query, string correctAnswer,
            string incorrectAnswer1, string incorrectAnswer2, string incorrectAnswer3)
        {
            Query = query;
            CorrectAnswer = correctAnswer;
            IncorrectAnswers = new string[3] { incorrectAnswer1, incorrectAnswer2, incorrectAnswer3 };
        }
    }
}

