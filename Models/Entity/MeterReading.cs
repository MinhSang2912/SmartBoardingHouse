namespace SmartBoardingHouse.Models.Entity
{
    public class MeterReading : BaseModel
    {
        public int RoomId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public double ElectricityIndex { get; set; }
        public double WaterIndex { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime ReadingDate { get; set; } = DateTime.UtcNow;
    }
}
