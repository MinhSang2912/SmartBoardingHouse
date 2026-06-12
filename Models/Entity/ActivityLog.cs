using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class ActivityLog
    {
        public ActivityType Type { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
