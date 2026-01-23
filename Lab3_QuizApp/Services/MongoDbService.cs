using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizAppExtended.Models;

namespace QuizAppExtended.Services
{
    internal class MongoDbService
    {
        private readonly IMongoCollection<QuestionPack> _collection;

        public MongoDbService(string connectionString, string databaseName, string collectionName = "Packs")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<QuestionPack>(collectionName);
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            var db = _collection.Database;
            var collectionName = _collection.CollectionNamespace.CollectionName;

            // Create collection if it does not exist
            var existing = await db.ListCollectionNames().ToListAsync();
            if (!existing.Contains(collectionName))
            {
                await db.CreateCollectionAsync(collectionName);
            }

            // Do NOT try to create a unique index on the Id property mapped to _id.
            // Creating a unique index on _id is invalid. Instead create a non-_id index (example: Name).
            var nameIndex = Builders<QuestionPack>.IndexKeys.Ascending(p => p.Name);
            var nameIndexModel = new CreateIndexModel<QuestionPack>(nameIndex);
            try
            {
                await _collection.Indexes.CreateOneAsync(nameIndexModel);
            }
            catch (MongoCommandException)
            {
                // Ignore index creation errors (e.g. already exists) to avoid breaking startup.
                // Optionally log the exception to a logger.
            }
        }

        public async Task<List<QuestionPack>> GetAllPacksAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task UpsertPackAsync(QuestionPack pack)
        {
            if (string.IsNullOrWhiteSpace(pack.Id))
            {
                pack.Id = ObjectId.GenerateNewId().ToString();
            }

            var filter = Builders<QuestionPack>.Filter.Eq(p => p.Id, pack.Id);
            await _collection.ReplaceOneAsync(filter, pack, new ReplaceOptions { IsUpsert = true });
        }

        public async Task DeletePackAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<QuestionPack>.Filter.Eq(p => p.Id, id));
        }

        // Convenience: migrate packs from existing JSON file into Mongo (one-time)
        public async Task MigrateFromJsonAsync(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath)) return;

            var json = await File.ReadAllTextAsync(jsonFilePath);
            var packs = JsonSerializer.Deserialize<QuestionPack[]>(json);
            if (packs == null || packs.Length == 0) return;

            foreach (var pack in packs)
            {
                if (string.IsNullOrWhiteSpace(pack.Id))
                    pack.Id = ObjectId.GenerateNewId().ToString();
            }

            await _collection.InsertManyAsync(packs);
        }
    }
}