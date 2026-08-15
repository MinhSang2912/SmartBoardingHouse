using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Thông báo gửi cho người thuê phòng (Tenant)
    /// </summary>
    public class Notification : BaseModel
    {
        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        [BsonElement("type")]
        public NotificationType Type { get; set; } = NotificationType.General;

        [BsonElement("refId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? RefId { get; set; }   // Có thể là InvoiceId, MaintenanceId, MessageId,...

        [BsonElement("refModel")]
        public string? RefModel { get; set; }  // Ví dụ: "Invoice", "MaintenanceRequest", "Message"

        [BsonElement("meta")]
        public Dictionary<string, object>? Meta { get; set; }  // Dữ liệu linh hoạt

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }
        [BsonElement("isReadAdmin")]
        public bool IsReadAdmin { get; set; } = false;

    }
}
