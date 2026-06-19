using FluentValidation;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Entity
{
    public class Floor : BaseModel
    {
        public int FloorNumber { get; set; }
        public int RoomCount { get; set; }
    }

}