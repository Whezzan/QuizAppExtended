using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizAppExtended.Models;

namespace QuizAppExtended.Services
{
    internal class MongoGameSessionService
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
    }
}
