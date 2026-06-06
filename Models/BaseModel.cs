using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models
{
    public class BaseModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
