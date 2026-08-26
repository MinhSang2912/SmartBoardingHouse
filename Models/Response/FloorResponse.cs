namespace SmartBoardingHouse.Models.Response
{
    public class FloorResponse
    {
        public int TotalFloors { get; set; }
        public int TotalRooms { get; set; }
        public int TotalActiveRooms { get; set; }
        public int TotalInactiveRooms { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<FloorItemResponse> Floors { get; set; } = new();
    }

    public class FloorItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int RoomCount { get; set; }
        public int ActiveRooms { get; set; }
        public int InactiveRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int EmptyRooms { get; set; }
        public decimal RevenueOnFloor { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
