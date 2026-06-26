using MongoDB.Bson.Serialization.Attributes;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    // Log nội bộ riêng cho Admin — không có entity tương ứng bên Client,
    // không cần đồng bộ field name/type với Mongoose.
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
