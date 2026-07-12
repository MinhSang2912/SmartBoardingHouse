using MongoDB.Bson.Serialization.Attributes;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng ActivityLog lưu trữ thông tin về các hoạt động của người dùng trong hệ thống.
    /// </summary>
    public class ActivityLog : BaseModel
    {
        [BsonElement("type")]
        public ActivityType Type { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; } = string.Empty;

        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal? Amount { get; set; }
    }
}
