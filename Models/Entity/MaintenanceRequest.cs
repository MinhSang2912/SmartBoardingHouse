using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class MaintenanceRequest : BaseModel
    {
        public int RoomId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
        public Room Room { get; set; } = null!;
    }
}
