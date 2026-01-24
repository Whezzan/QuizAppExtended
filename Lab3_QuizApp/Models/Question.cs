using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuizAppExtended.Models
{
    [BsonIgnoreExtraElements]
    public class Question
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Query")]
        public string Query { get; set; }

        [BsonElement("CorrectAnswer")]
        public string CorrectAnswer { get; set; }

        [BsonElement("IncorrectAnswers")]
        public string[] IncorrectAnswers { get; set; }

        // New: link a question to a category in Categories collection
        [BsonElement("CategoryId")]
        public string? CategoryId { get; set; }

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

