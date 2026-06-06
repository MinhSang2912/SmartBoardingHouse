using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Room: BaseModel
    {
        public string RoomName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Area { get; set; }
        public decimal RoomDeposit { get; set; }
        public int FloorId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
    }
}
