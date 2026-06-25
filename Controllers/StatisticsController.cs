using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatisticsController : AuthorizedControllerBase
    {
        private readonly IMongoCollection<Invoice> _invoiceCollection;
        private readonly IMongoCollection<MeterReading> _meterReadingCollection;

        public StatisticsController(MongoDbService mongoService)
            : base(mongoService)
        {
            var db = mongoService.GetDatabase();
            _invoiceCollection = db.GetCollection<Invoice>("Invoices");
            _meterReadingCollection = db.GetCollection<MeterReading>("MeterReadings");
        }

        // GET: api/Statistics/monthly?year=2024&month=6
        [HttpGet("monthly")]
        public async Task<ActionResult<MonthlyStatisticsResponse>> GetMonthly(int? year, int? month)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null)
                return Unauthorized();

            var y = year ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            if (m < 1 || m > 12)
                return BadRequest("Tháng phải từ 1 đến 12.");

            var invoice = await _invoiceCollection
                .Find(i => i.BillingYear == y && i.BillingMonth == m && i.RoomNumber == currentUser.RoomNumber && i.TenantName == currentUser.Name)
                .FirstOrDefaultAsync();

            var meterReadings = await _meterReadingCollection
                .Find(r => r.Year == y && r.Month == m && r.RoomNumber == currentUser.RoomNumber)
                .ToListAsync();

            var electricReading = meterReadings.FirstOrDefault(r => r.Type == Enums.MeterType.Electric);
            var waterReading = meterReadings.FirstOrDefault(r => r.Type == Enums.MeterType.Water);

            var response = new MonthlyStatisticsResponse
            {
                Year = y,
                Month = m,
                MonthName = $"Tháng {m}",
                TotalAmount = invoice?.Amount ?? 0m,
                PaidAmount = invoice?.PaidAmount ?? 0m,
                DebtAmount = (invoice?.Amount ?? 0m) - (invoice?.PaidAmount ?? 0m),
                InvoiceStatus = invoice?.Status.ToString().ToLower() ?? "no_invoice",
                DueDate = invoice?.DueDate,
                Room = invoice is not null ? new RoomStatistic { RoomNumber = invoice.RoomNumber, MonthlyRent = invoice.RoomPrice } : null,
                Utilities = new UtilitiesStatistic
                {
                    Electric = electricReading is not null ? new UtilityStatistic
                    {
                        Usage = electricReading.Usage,
                        UnitPrice = electricReading.Type == Enums.MeterType.Electric ? 3000m : 0m,
                        Cost = (decimal)electricReading.Usage * 3000m,
                        CurrentReading = electricReading.CurrentIndex,
                        PreviousReading = electricReading.PreviousIndex,
                        Verified = false,
                    } : null,
                    Water = waterReading is not null ? new UtilityStatistic
                    {
                        Usage = waterReading.Usage,
                        UnitPrice = waterReading.Type == Enums.MeterType.Water ? 10000m : 0m,
                        Cost = (decimal)waterReading.Usage * 10000m,
                        CurrentReading = waterReading.CurrentIndex,
                        PreviousReading = waterReading.PreviousIndex,
                        Verified = false,
                    } : null
                },
                Breakdown = new List<BreakdownItem>()
            };

            return Ok(response);
        }

        // GET: api/Statistics/yearly?year=2024
        [HttpGet("yearly")]
        public async Task<ActionResult<YearlyStatisticsResponse>> GetYearly(int? year)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null)
                return Unauthorized();

            var y = year ?? DateTime.Now.Year;
            var invoices = await _invoiceCollection
                .Find(i => i.BillingYear == y && i.RoomNumber == currentUser.RoomNumber && i.TenantName == currentUser.Name)
                .ToListAsync();

            var monthlyData = new List<MonthlyStatistic>();
            for (var i = 1; i <= 12; i++)
            {
                var invoice = invoices.FirstOrDefault(inv => inv.BillingMonth == i);
                monthlyData.Add(new MonthlyStatistic
                {
                    Month = i,
                    MonthName = $"Tháng {i}",
                    TotalAmount = invoice?.Amount ?? 0m,
                    PaidAmount = invoice?.PaidAmount ?? 0m,
                    DebtAmount = (invoice?.Amount ?? 0m) - (invoice?.PaidAmount ?? 0m),
                    Status = invoice?.Status.ToString().ToLower() ?? "no_invoice"
                });
            }

            var totalYear = invoices.Sum(inv => inv.Amount);
            var paidYear = invoices.Sum(inv => inv.PaidAmount);
            var meterReadings = await _meterReadingCollection
                .Find(r => r.Year == y && r.RoomNumber == currentUser.RoomNumber)
                .ToListAsync();

            var electricReadings = meterReadings.Where(r => r.Type == Enums.MeterType.Electric).ToList();
            var waterReadings = meterReadings.Where(r => r.Type == Enums.MeterType.Water).ToList();

            var response = new YearlyStatisticsResponse
            {
                Year = y,
                Summary = new SummaryStatistic
                {
                    TotalYear = totalYear,
                    PaidYear = paidYear,
                    DebtYear = totalYear - paidYear,
                    MonthsWithInvoice = invoices.Count,
                    AverageMonthly = invoices.Any() ? Math.Round(totalYear / invoices.Count, 2) : 0m
                },
                MonthlyData = monthlyData,
                Utilities = new UsageStatistic
                {
                    Electric = new UtilitySummary
                    {
                        TotalUsage = electricReadings.Sum(r => r.Usage),
                        TotalCost = electricReadings.Sum(r => (decimal)r.Usage * 3000m),
                        AverageUsage = electricReadings.Any() ? electricReadings.Average(r => r.Usage) : 0,
                        AverageCost = electricReadings.Any() ? Math.Round(electricReadings.Sum(r => (decimal)r.Usage * 3000m) / electricReadings.Count, 2) : 0m,
                        MonthsRecorded = electricReadings.Count
                    },
                    Water = new UtilitySummary
                    {
                        TotalUsage = waterReadings.Sum(r => r.Usage),
                        TotalCost = waterReadings.Sum(r => (decimal)r.Usage * 10000m),
                        AverageUsage = waterReadings.Any() ? waterReadings.Average(r => r.Usage) : 0,
                        AverageCost = waterReadings.Any() ? Math.Round(waterReadings.Sum(r => (decimal)r.Usage * 10000m) / waterReadings.Count, 2) : 0m,
                        MonthsRecorded = waterReadings.Count
                    }
                },
                PaymentStatus = new PaymentStatusStatistic
                {
                    Paid = invoices.Count(inv => inv.Status == InvoiceStatus.Paid),
                    Unpaid = invoices.Count(inv => inv.Status == InvoiceStatus.Unpaid),
                    Partial = invoices.Count(inv => inv.Status == InvoiceStatus.Partial),
                    Overdue = invoices.Count(inv => inv.Status == InvoiceStatus.Overdue)
                }
            };

            return Ok(response);
        }

    }
}
