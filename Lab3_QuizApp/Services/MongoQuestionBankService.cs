using System.Security.Cryptography;
using System.Text;
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

            await BackfillFingerprintsAsync();

            var categoryIndex = Builders<Question>.IndexKeys.Ascending(q => q.CategoryId);
            var categoryIndexModel = new CreateIndexModel<Question>(categoryIndex);

            var fingerprintIndex = Builders<Question>.IndexKeys.Ascending(q => q.BankFingerprint);
            var fingerprintIndexModel = new CreateIndexModel<Question>(
                fingerprintIndex,
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_questionbank_fingerprint"
                });

            // IMPORTANT: do not swallow. If this fails you MUST know why.
            await _collection.Indexes.CreateManyAsync(new[] { categoryIndexModel, fingerprintIndexModel });
        }

        public async Task InsertAsync(Question question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            question.BankFingerprint = ComputeFingerprint(question);

            // Friendly pre-check (optional). Real protection is the unique index.
            var exists = await _collection.Find(q => q.BankFingerprint == question.BankFingerprint).AnyAsync();
            if (exists)
            {
                throw new InvalidOperationException("Duplicate question in Question Bank (same question + all answers).");
            }

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

        private async Task BackfillFingerprintsAsync()
        {
            // Load docs missing fingerprint and update them.
            var missing = await _collection
                .Find(q => q.BankFingerprint == null || q.BankFingerprint == string.Empty)
                .ToListAsync();

            if (missing.Count == 0)
            {
                return;
            }

            foreach (var q in missing)
            {
                q.BankFingerprint = ComputeFingerprint(q);

                var filter = Builders<Question>.Filter.Eq(x => x.Id, q.Id);
                var update = Builders<Question>.Update.Set(x => x.BankFingerprint, q.BankFingerprint);

                await _collection.UpdateOneAsync(filter, update);
            }
        }

        private static string ComputeFingerprint(Question q)
        {
            static string Norm(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

            var incorrect = (q.IncorrectAnswers ?? Array.Empty<string>())
                .Select(Norm)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            var payload = string.Join("|", new[]
            {
                Norm(q.Query),
                Norm(q.CorrectAnswer),
                string.Join(",", incorrect)
            });

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes);
        }
    }
}
