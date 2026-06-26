using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models
{
    // Id và timestamps đổi để khớp đúng kiểu/tên field mà Mongoose (phía Client)
    // tạo ra trên cùng MongoDB. Mongoose luôn sinh _id dạng ObjectId, không phải int,
    // và field timestamps là "createdAt" / "updatedAt" (camelCase).
    public class BaseModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
