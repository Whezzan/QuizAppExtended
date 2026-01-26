using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizAppExtended.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizAppExtended.Services
{
    internal class MongoCategoryService
    {
        private readonly IMongoCollection<TriviaCategory> _collection;

        public MongoCategoryService(string connectionString, string databaseName, string collectionName = "Categories")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<TriviaCategory>(collectionName);
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
            var nameIndexKeys = Builders<TriviaCategory>.IndexKeys.Ascending(c => c.Name);
            var nameIndexModel = new CreateIndexModel<TriviaCategory>(
                nameIndexKeys,
                new CreateIndexOptions
                {
                    Unique = true,
                    Collation = new Collation("en", strength: CollationStrength.Secondary),
                    Name = "ux_categories_name_ci"
                });
            var openTdbIndexKeys = Builders<TriviaCategory>.IndexKeys.Ascending(c => c.OpenTdbId);
            var openTdbIndexModel = new CreateIndexModel<TriviaCategory>(openTdbIndexKeys);

            try
            {
                await _collection.Indexes.CreateManyAsync(new[] { nameIndexModel, openTdbIndexModel });
            }
            catch (MongoCommandException)
            {

            }
        }

        public Task<List<TriviaCategory>> GetAllAsync()
            => _collection.Find(_ => true).ToListAsync();

        public async Task<TriviaCategory?> FindByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var filter = Builders<TriviaCategory>.Filter.Eq(c => c.Name, name);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<TriviaCategory?> FindByOpenTdbIdAsync(string openTdbId)
        {
            if (string.IsNullOrWhiteSpace(openTdbId)) return null;

            var filter = Builders<TriviaCategory>.Filter.Eq(c => c.OpenTdbId, openTdbId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(TriviaCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.Id))
            {
                category.Id = ObjectId.GenerateNewId().ToString();
            }

            var filter = Builders<TriviaCategory>.Filter.Eq(c => c.Id, category.Id);
            await _collection.ReplaceOneAsync(filter, category, new ReplaceOptions { IsUpsert = true });
        }

        public Task DeleteAsync(string id)
            => _collection.DeleteOneAsync(Builders<TriviaCategory>.Filter.Eq(c => c.Id, id));

        public async Task SeedDefaultsAsync(IEnumerable<TriviaCategory> defaults)
        {
            if (defaults == null)
            {
                return;
            }

            var existing = await GetAllAsync();
            var existingIdsByOpenTdb = existing
                .Where(c => !string.IsNullOrWhiteSpace(c.OpenTdbId))
                .ToDictionary(c => c.OpenTdbId, c => c.Id);

            var existingNames = existing
                .Select(c => (c.Name ?? string.Empty).Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in defaults)
            {
                if (candidate == null)
                {
                    continue;
                }

                var name = (candidate.Name ?? string.Empty).Trim();
                var openTdbId = (candidate.OpenTdbId ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(openTdbId) && existingIdsByOpenTdb.ContainsKey(openTdbId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(name) && existingNames.Contains(name))
                {
                    continue;
                }

                await UpsertAsync(new TriviaCategory(name, openTdbId));
            }
        }
    }
}
