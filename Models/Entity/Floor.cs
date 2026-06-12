using FluentValidation;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Entity
{
    public class Floor : BaseModel
    {
        public string FloorNumber { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }

    
}