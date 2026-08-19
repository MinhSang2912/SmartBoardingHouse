using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    public class ItemFee : BaseModel
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("unit")]
        public string Unit { get; set; } = "tháng";

        [BsonElement("type")]
        public string Type { get; set; } = "mandatory"; // mandatory, wifi, parking

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
