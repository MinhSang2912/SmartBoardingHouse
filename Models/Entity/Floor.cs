using FluentValidation;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng Floor lưu trữ thông tin về các tầng trong nhà trọ. Mỗi tầng có thể chứa nhiều phòng.
    /// </summary>
    public class Floor : BaseModel
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("floorNumber")]
        public int FloorNumber { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }
    }
}
