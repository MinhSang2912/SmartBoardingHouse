using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    public class Message : BaseModel
    {
        [BsonElement("sender")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SenderId { get; set; } = string.Empty;

        [BsonElement("receiver")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReceiverId { get; set; } = string.Empty;

        [BsonElement("senderModel")]
        public string SenderModel { get; set; } = "Tenant";

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "text";

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; }

        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }
    }
}
