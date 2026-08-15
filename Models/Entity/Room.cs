using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng Room lưu trữ thông tin về các phòng trong nhà trọ. 
    /// Mỗi phòng có thể thuộc về một tầng và có thể được thuê bởi một người thuê.
    /// </summary>
    public class Room : BaseModel
    {
        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = string.Empty;

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("roomDeposit")]
        public decimal RoomDeposit { get; set; }

        [BsonElement("area")]
        public double Area { get; set; }

        [BsonElement("maxOccupants")]
        public int MaxOccupants { get; set; } = 2;

        [BsonElement("floor")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FloorId { get; set; } = string.Empty;

        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TenantId { get; set; }

        [BsonElement("amenities")]
        public List<string> Amenities { get; set; } = new();

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("status")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
