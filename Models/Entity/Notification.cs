using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng Notification lưu trữ thông tin về các thông báo gửi đến người dùng trong hệ thống.
    /// </summary>
    public class Notification : BaseModel
    {
        [BsonElement("user")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "general";

        [BsonElement("refId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? RefId { get; set; }

        [BsonElement("refModel")]
        public string? RefModel { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; }

        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }
    }
}
