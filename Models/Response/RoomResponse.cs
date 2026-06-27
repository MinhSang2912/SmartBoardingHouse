using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class RoomResponse
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } 
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Area { get; set; }
        public int MaxOccupants { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal RoomDeposit { get; set; }
        public int FloorNumber { get; set; }
        public RoomStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? TenantName { get; set; }
    }
}
