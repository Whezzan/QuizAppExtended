using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuizAppExtended.Models
{
    public class TriviaCategory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;
        public string Name { get; set; }
        public string OpenTdbId { get; set; }

        public TriviaCategory(string name = "", string openTdbId = "")
        {
            Name = name;
            OpenTdbId = openTdbId;
        }
    }
}