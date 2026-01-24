using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizAppExtended.Models;

namespace QuizAppExtended.Services
{
    internal class MongoQuestionBankService
    {
        private readonly IMongoCollection<Question> _collection;

        public MongoQuestionBankService(string connectionString, string databaseName, string collectionName = "QuestionBank")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Question>(collectionName);
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

            var categoryIndex = Builders<Question>.IndexKeys.Ascending(q => q.CategoryId);
            var categoryIndexModel = new CreateIndexModel<Question>(categoryIndex);

            try
            {
                await _collection.Indexes.CreateOneAsync(categoryIndexModel);
            }
            catch (MongoCommandException)
            {
            }
        }

        public async Task InsertAsync(Question question)
        {
            if (string.IsNullOrWhiteSpace(question.Id))
            {
                question.Id = ObjectId.GenerateNewId().ToString();
            }

            await _collection.InsertOneAsync(question);
        }

        public Task<List<Question>> GetAllAsync()
            => _collection.Find(_ => true).ToListAsync();

        public Task<List<Question>> GetByCategoryIdAsync(string categoryId)
            => _collection.Find(q => q.CategoryId == categoryId).ToListAsync();
    }
}
