using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models
{
    public class Floor : BaseModel
    {
        public string FloorNumber { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }
}