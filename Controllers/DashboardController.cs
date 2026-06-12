using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<Invoice> _invoiceCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<ActivityLog> _activityLogCollection;
        private readonly IMongoCollection<MaintenanceRequest> _maintenanceCollection;

        public DashboardController(MongoDbService mongoService)
        {
            var database = mongoService.GetDatabase();
            _roomCollection = database.GetCollection<Room>("Rooms");
            _invoiceCollection = database.GetCollection<Invoice>("Invoices");
            _contractCollection = database.GetCollection<Contract>("Contracts");
            _activityLogCollection = database.GetCollection<ActivityLog>("ActivityLogs");
            _maintenanceCollection = database.GetCollection<MaintenanceRequest>("MaintenanceRequests");
        }

        // GET: api/Dashboard
        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboard()
        {
            var response = new DashboardResponse();

            // Thống kê phòng
            var totalRooms = await _roomCollection.CountDocumentsAsync(_ => true);
            var rentedRooms = await _roomCollection.CountDocumentsAsync(r => r.Status == RoomStatus.Occupied);
            response.TotalRooms = (int)totalRooms;
            response.RentedRooms = (int)rentedRooms;

            var now = DateTime.Now;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            // Doanh thu tháng hiện tại
            var monthlyRevenue = await _invoiceCollection.Aggregate()
                .Match(i => i.DueDate.Month == currentMonth
                         && i.DueDate.Year == currentYear
                         && i.Status == InvoiceStatus.Paid)
                .Group(new BsonDocument
                {
            { "_id", BsonNull.Value },
            { "total", new BsonDocument("$sum", "$Amount") }
                })
                .FirstOrDefaultAsync();

            response.MonthlyRevenue = monthlyRevenue?["total"]?.AsDecimal128 is Decimal128 d
                ? (decimal)d
                : 0m;

            // Hóa đơn chưa thanh toán
            response.UnpaidInvoices = (int)await _invoiceCollection
                .CountDocumentsAsync(i => i.Status == InvoiceStatus.Unpaid);

            // Doanh thu 6 tháng gần đây
            var sixMonthsAgo = new DateTime(currentYear, currentMonth, 1).AddMonths(-5);

            var revenueGroups = await _invoiceCollection.Aggregate()
                .Match(i => i.Status == InvoiceStatus.Paid && i.DueDate >= sixMonthsAgo)
                .Group(new BsonDocument
                {
            { "_id", new BsonDocument
                {
                    { "year", new BsonDocument("$year", "$DueDate") },
                    { "month", new BsonDocument("$month", "$DueDate") }
                }
            },
            { "total", new BsonDocument("$sum", "$Amount") }
                })
                .ToListAsync();

            var revenueDict = revenueGroups.ToDictionary(
                g => (g["_id"]["year"].AsInt32, g["_id"]["month"].AsInt32),
                g => g["total"].AsDecimal128
            );

            response.RevenueLast6Months = new List<RevenueChartDto>();
            for (int i = 5; i >= 0; i--)
            {
                var date = new DateTime(currentYear, currentMonth, 1).AddMonths(-i);
                var key = (date.Year, date.Month);

                response.RevenueLast6Months.Add(new RevenueChartDto
                {
                    Month = $"T{date.Month}", 
                    Revenue = revenueDict.TryGetValue(key, out var total) ? (decimal)total : 0m
                });
            }

            // Hoạt động gần đây - lấy từ ActivityLog
            var recentLogs = await _activityLogCollection
                .Find(_ => true)
                .SortByDescending(a => a.CreatedAt)
                .Limit(4)
                .ToListAsync();

            response.RecentActivities = recentLogs.Select(a => new RecentActivityResponse
            {
                Type = GetActivityTypeLabel(a.Type),
                UserName = a.UserName,
                Description = a.Description,
                RoomNumber = a.RoomNumber,
                TimeAgo = GetTimeAgo(a.CreatedAt),
                Amount = a.Amount
            }).ToList();

            return Ok(response);
        }

        // Helper: chuyển enum thành label tiếng Việt hiển thị
        private static string GetActivityTypeLabel(ActivityType type) => type switch
        {
            ActivityType.Payment => "Thanh toán",
            ActivityType.CheckOut => "Trả phòng",
            ActivityType.CheckIn => "Nhận phòng",
            ActivityType.Maintenance => "Bảo trì",
            _ => "Khác"
        };

        // Helper: tính thời gian tương đối (vd: "10 phút trước")
        private static string GetTimeAgo(DateTime createdAt)
        {
            var span = DateTime.Now - createdAt;

            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} ngày trước";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)} tháng trước";
            return $"{(int)(span.TotalDays / 365)} năm trước";
        }
    }
}