using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : AuthorizedControllerBase
    {
        private readonly IMongoCollection<Invoice> _invoiceCollection;
        private readonly IMongoCollection<Payment> _paymentCollection;
        private readonly IMongoCollection<MeterReading> _meterReadingCollection;

        public ReportsController(MongoDbService mongoService)
            : base(mongoService)
        {
            var db = mongoService.GetDatabase();
            _invoiceCollection = db.GetCollection<Invoice>("Invoices");
            _paymentCollection = db.GetCollection<Payment>("Payments");
            _meterReadingCollection = db.GetCollection<MeterReading>("MeterReadings");
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyReport(int? year, int? month)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null)
                return Unauthorized();

            var y = year ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            if (m < 1 || m > 12)
                return BadRequest("Tháng phải từ 1 đến 12.");

            var invoice = await _invoiceCollection.Find(i => i.BillingYear == y && i.BillingMonth == m && i.RoomNumber == currentUser.RoomNumber && i.TenantName == currentUser.Name).FirstOrDefaultAsync();
            var payments = await _paymentCollection.Find(p => p.TenantId == currentUser.Id && p.CreatedAt >= new DateTime(y, m, 1) && p.CreatedAt <= new DateTime(y, m, DateTime.DaysInMonth(y, m), 23, 59, 59)).ToListAsync();
            var meterReadings = await _meterReadingCollection.Find(r => r.Year == y && r.Month == m && r.RoomNumber == currentUser.RoomNumber).ToListAsync();

            var report = new
            {
                generatedAt = DateTime.UtcNow,
                period = new { month = m, year = y, label = $"Tháng {m}/{y}" },
                tenant = new { name = currentUser.Name, phone = currentUser.PhoneNumber, email = currentUser.Email },
                room = invoice is not null ? new { invoice.RoomNumber, monthlyRent = invoice.RoomPrice } : null,
                invoice = invoice is not null ? new
                {
                    totalAmount = invoice.Amount,
                    paidAmount = invoice.PaidAmount,
                    remainingAmount = invoice.Amount - invoice.PaidAmount,
                    status = invoice.Status.ToString().ToLower(),
                    dueDate = invoice.DueDate,
                } : null,
                payments = payments.Select(p => new { p.Amount, p.Method, p.TransactionId, p.PaidAt }),
                meterReadings = new
                {
                    electric = meterReadings.FirstOrDefault(r => r.Type == Enums.MeterType.Electric),
                    water = meterReadings.FirstOrDefault(r => r.Type == Enums.MeterType.Water)
                }
            };

            return Ok(report);
        }

    }
}
