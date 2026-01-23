using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuizAppExtended.Models
{
    public class TriviaCategory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // Mongo will set this on insert

        public string Name { get; set; }

        // OpenTDB category id (used as "category" query param)
        public string OpenTdbId { get; set; }

        public TriviaCategory(string name = "", string openTdbId = "")
        {
            Name = name;
            OpenTdbId = openTdbId;
        }
    }
}