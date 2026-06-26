using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using CommonMessage = SmartBoardingHouse.Common.Message;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : AuthorizedControllerBase
    {
        private readonly IMongoCollection<Payment> _paymentCollection;
        private readonly IMongoCollection<Invoice> _invoiceCollection;

        public PaymentsController(MongoDbService mongoService)
            : base(mongoService)
        {
            var db = mongoService.GetDatabase();
            _paymentCollection = db.GetCollection<Payment>("Payments");
            _invoiceCollection = db.GetCollection<Invoice>("Invoices");
        }

        [HttpPost]
        public async Task<ActionResult<PaymentResponse>> Create(PaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.InvoiceId) || request.Amount <= 0)
                return BadRequest(CommonMessage.PaymentRequestInvalid());

            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var invoice = await _invoiceCollection
                .Find(i => i.Id == request.InvoiceId && i.TenantName == user.Name && i.RoomNumber == user.RoomNumber)
                .FirstOrDefaultAsync();

            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            if (invoice.Status == InvoiceStatus.Paid)
                return BadRequest(CommonMessage.InvoiceAlreadyPaid());

            var remaining = invoice.Amount - invoice.PaidAmount;
            if (request.Amount > remaining)
                return BadRequest($"Số tiền thanh toán vượt quá số còn lại: {remaining}");

            var payment = new Payment
            {
                TenantId = user.Id,
                InvoiceId = invoice.Id,
                Amount = request.Amount,
                Method = request.Method,
                Status = "success",
                TransactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                QrData = $"Phong {invoice.RoomNumber} - Thang {invoice.BillingMonth}/{invoice.BillingYear} - {user.Name}",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

            await _paymentCollection.InsertOneAsync(payment);

            invoice.PaidAmount += request.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.Amount
                ? InvoiceStatus.Paid
                : InvoiceStatus.Partial;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _invoiceCollection.ReplaceOneAsync(i => i.Id == invoice.Id, invoice);

            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, MapToResponse(payment));
        }

        [HttpGet("history")]
        public async Task<ActionResult<object>> GetHistory(int page = 1, int limit = 10)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;

            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var filter = Builders<Payment>.Filter.Eq(p => p.TenantId, user.Id);
            var total = await _paymentCollection.CountDocumentsAsync(filter);
            var payments = await _paymentCollection.Find(filter)
                .SortByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            return Ok(new
            {
                payments = payments.Select(MapToResponse),
                pagination = new { page, limit, total }
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentResponse>> GetById(string id)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var payment = await _paymentCollection
                .Find(p => p.Id == id && p.TenantId == user.Id)
                .FirstOrDefaultAsync();

            if (payment is null)
                return NotFound("Thanh toán không tìm thấy.");

            return Ok(MapToResponse(payment));
        }

        private PaymentResponse MapToResponse(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                Method = payment.Method,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                QrData = payment.QrData,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt,
            };
        }

    }
}
