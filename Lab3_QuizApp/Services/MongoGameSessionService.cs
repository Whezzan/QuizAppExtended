using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizAppExtended.Models;

namespace QuizAppExtended.Services
{
    internal partial class MongoGameSessionService
    {
        private readonly IMongoCollection<GameSession> _collection;

        public MongoGameSessionService(string connectionString, string databaseName, string collectionName = "GameSessions")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<GameSession>(collectionName);
        }

        public async Task EnsureCreatedAsync()
        {
            var db = _collection.Database;
            var collectionName = _collection.CollectionNamespace.CollectionName;

            var existing = await db.ListCollectionNames().ToListAsync();
            if (!existing.Contains(collectionName))
            {
                await db.CreateCollectionAsync(collectionName);
            }

            var startedAtIndex = Builders<GameSession>.IndexKeys.Descending(s => s.StartedAtUtc);
            var startedAtIndexModel = new CreateIndexModel<GameSession>(startedAtIndex);

            try
            {
                await _collection.Indexes.CreateOneAsync(startedAtIndexModel);
            }
            catch (MongoCommandException)
            {
                // Ignore index creation errors
            }
        }

        public async Task InsertAsync(GameSession session)
        {
            if (string.IsNullOrWhiteSpace(session.Id))
            {
                session.Id = ObjectId.GenerateNewId().ToString();
            }

            await _collection.InsertOneAsync(session);
        }

        public Task<List<GameSession>> GetLatestAsync(int take = 50)
            => _collection.Find(_ => true).SortByDescending(s => s.StartedAtUtc).Limit(take).ToListAsync();

        public Task<List<GameSession>> GetTop5ByPackAsync(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                return Task.FromResult(new List<GameSession>());
            }

            var filter = Builders<GameSession>.Filter.Eq(s => s.PackId, packId);

            return _collection.Find(filter)
                .SortByDescending(s => s.CorrectCount)
                .ThenBy(s => s.TotalTimeSeconds)
                .Limit(5)
                .ToListAsync();
        }

        public async Task<AnswerStats> GetAnswerStatsAsync(string packId, string questionText)
        {
            if (string.IsNullOrWhiteSpace(packId) || string.IsNullOrWhiteSpace(questionText))
            {
                return new AnswerStats();
            }

            var filter = Builders<GameSession>.Filter.Eq(s => s.PackId, packId);
            var sessions = await _collection.Find(filter).ToListAsync();

            var all = sessions
                .SelectMany(s => s.Answers ?? new List<GameSessionAnswer>())
                .Where(a => string.Equals(a.QuestionText, questionText, StringComparison.Ordinal))
                .Where(a => !string.IsNullOrWhiteSpace(a.SelectedAnswer))
                .Select(a => a.SelectedAnswer)
                .ToList();

            var stats = new AnswerStats
            {
                TotalAnswers = all.Count
            };

            foreach (var g in all.GroupBy(x => x))
            {
                stats.CountsByAnswer[g.Key] = g.Count();
            }

            return stats;
        }
    }
}
