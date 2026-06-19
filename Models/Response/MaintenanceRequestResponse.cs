using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class MaintenanceRequestResponse
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;   // MT-2026-001
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PriotyRequest Priority { get; set; }
        public string PriorityLabel { get; set; } = string.Empty; 
        public MaintenanceStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;    
        public DateTime CreatedAt { get; set; }
    }

    // Dùng cho thống kê 
    public class MaintenanceSummaryResponse
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public List<MaintenanceRequestResponse> Items { get; set; } = new();
    }
}