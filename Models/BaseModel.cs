using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models
{
    public class BaseModel
    {
        [BsonId]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
