namespace SmartBoardingHouse.Models.Response
{
    public class FloorResponse
    {
        public int TotalFloors { get; set; }
        public int TotalRooms { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<FloorItemResponse> Floors { get; set; } = new();
    }

    public class FloorItemResponse
    {
        public int Id { get; set; }
        public int FloorNumber { get; set; } 
        public int RoomCount { get; set; }
        public int OccupiedRooms { get; set; }
        public int EmptyRooms { get; set; }
        public decimal RevenueOnFloor { get; set; }
    }
}