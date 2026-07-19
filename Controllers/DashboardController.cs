using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using static SmartBoardingHouse.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<Invoice> _invoiceCollection;
        private readonly IMongoCollection<ActivityLog> _activityLogCollection;

        public DashboardController(MongoDbService mongoService)
        {
            var db = mongoService.GetDatabase();
            _roomCollection = db.GetCollection<Room>("rooms");
            _invoiceCollection = db.GetCollection<Invoice>("invoices");
            _activityLogCollection = db.GetCollection<ActivityLog>("activitylogs");
        }

        // GET: api/Dashboard
        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboard()
        {
            var dto = new DashboardResponse();

            // Thống kê phòng
            var totalRooms = await _roomCollection.CountDocumentsAsync(_ => true);
            var rentedRooms = await _roomCollection.CountDocumentsAsync(r => r.Status == RoomStatus.Occupied);
            dto.TotalRooms = (int)totalRooms;
            dto.RentedRooms = (int)rentedRooms;

            var now = DateTime.Now;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            // Doanh thu tháng hiện tại (chỉ tính hóa đơn đã thanh toán, theo kỳ hóa đơn - BillingMonth/BillingYear
            // chứ không dùng DueDate vì hạn thanh toán có thể lệch sang tháng khác so với kỳ hóa đơn thực tế)
            var monthlyRevenueResult = await _invoiceCollection.Aggregate()
                .Match(i => i.Status == InvoiceStatus.Paid
                         && i.BillingMonth == currentMonth
                         && i.BillingYear == currentYear)
                .Group(i => 1, g => new { Total = g.Sum(i => i.Amount) })
                .FirstOrDefaultAsync();

            dto.MonthlyRevenue = monthlyRevenueResult?.Total ?? 0m;

            // Hóa đơn chưa thanh toán (bao gồm cả hóa đơn đã quá hạn, vì Overdue chỉ là trạng thái
            // hiển thị được tính động, DB vẫn lưu là Unpaid)
            dto.UnpaidInvoices = (int)await _invoiceCollection
                .CountDocumentsAsync(i => i.Status == InvoiceStatus.Unpaid);

            // Doanh thu 6 tháng gần đây (theo kỳ hóa đơn)
            var sixMonthsAgo = new DateTime(currentYear, currentMonth, 1).AddMonths(-5);

            var revenueGroups = await _invoiceCollection.Aggregate()
                .Match(i => i.Status == InvoiceStatus.Paid)
                .Group(i => new { i.BillingYear, i.BillingMonth }, g => new
                {
                    g.Key.BillingYear,
                    g.Key.BillingMonth,
                    Total = g.Sum(i => i.Amount)
                })
                .ToListAsync();

            var revenueDict = revenueGroups.ToDictionary(
                g => (g.BillingYear, g.BillingMonth),
                g => g.Total);

            dto.RevenueLast6Months = new List<RevenueChartDto>();
            for (int i = 5; i >= 0; i--)
            {
                var date = new DateTime(currentYear, currentMonth, 1).AddMonths(-i);
                var key = (date.Year, date.Month);

                dto.RevenueLast6Months.Add(new RevenueChartDto
                {
                    Month = $"T{date.Month}",
                    Revenue = revenueDict.TryGetValue(key, out var total) ? total : 0m
                });
            }

            // Hoạt động gần đây - lấy từ ActivityLog
            var recentLogs = await _activityLogCollection
                .Find(_ => true)
                .SortByDescending(a => a.CreatedAt)
                .Limit(5)
                .ToListAsync();

            dto.RecentActivities = recentLogs.Select(a => new RecentActivityResponse
            {
                Type = a.Type switch
                {
                    ActivityType.Payment => "Thanh toán",
                    ActivityType.CheckOut => "Trả phòng",
                    ActivityType.CheckIn => "Nhận phòng",
                    ActivityType.Maintenance => "Bảo trì",
                    _ => a.Type.ToString()
                },
                UserName = a.UserName,
                RoomNumber = a.RoomNumber,
                Description = a.Description,
                TimeAgo = GetTimeAgo(a.CreatedAt),
                Amount = a.Amount
            }).ToList();

            return Ok(dto);
        }

        // ==================== HELPERS ====================

        private static string GetTimeAgo(DateTime createdAt)
        {
            var span = DateTime.UtcNow - createdAt;

            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} ngày trước";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)} tháng trước";
            return $"{(int)(span.TotalDays / 365)} năm trước";
        }
    }
}