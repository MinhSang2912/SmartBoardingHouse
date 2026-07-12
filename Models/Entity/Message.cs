using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng Message lưu trữ thông tin về các tin nhắn trong cuộc trò chuyện giữa người thuê phòng và quản trị viên.
    /// </summary>
    public class Message : BaseModel
    {
        [BsonElement("conversationId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ConversationId { get; set; } = string.Empty;

        [BsonElement("senderRole")]
        public string SenderRole { get; set; } = "Tenant";  // "Tenant" hoặc "Admin"

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "text";  // "text" hoặc "image"

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }
    }
}