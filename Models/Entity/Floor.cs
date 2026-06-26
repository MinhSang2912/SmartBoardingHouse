using FluentValidation;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Entity
{
    public class Floor : BaseModel
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("floorNumber")]
        public int FloorNumber { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        // KHÔNG lưu trong Mongo (không có field tương ứng ở Client) — tính runtime
        // bằng cách query Room.CountDocuments(r => r.FloorId == this.Id) để 2 bên
        // luôn khớp, tránh trường hợp field bị cache lệch.
        [BsonIgnore]
        public int RoomCount { get; set; }
    }
}
