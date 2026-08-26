
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class DashboardResponse
    {
        public int TotalRooms { get; set; }
        public int RentedRooms { get; set; }
        public int InactiveRooms { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int UnpaidInvoices { get; set; }
        public List<RevenueChartDto> RevenueLast6Months { get; set; } = new();
        public List<RecentActivityResponse> RecentActivities { get; set; } = new();
    }
    public class RevenueChartDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
    public class RecentActivityResponse
    {
        public string Type { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
    }
}
