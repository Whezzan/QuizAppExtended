using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace QuizAppExtended.Models
{
    public class GameSession
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string PlayerName { get; set; } = string.Empty;

        public string? PackId { get; set; }
        public string? PackName { get; set; }

        public DateTime StartedAtUtc { get; set; }
        public DateTime EndedAtUtc { get; set; }

        public int TotalTimeSeconds { get; set; }
        public int CorrectCount { get; set; }
        public int QuestionCount { get; set; }

        public List<GameSessionAnswer> Answers { get; set; } = new();
    }

    public class GameSessionAnswer
    {
        public int QuestionIndex { get; set; }
        public string QuestionText { get; set; } = string.Empty;

        public string SelectedAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
        public int TimeSpentSeconds { get; set; }
    }
}
