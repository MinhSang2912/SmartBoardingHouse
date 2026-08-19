using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common.Message;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using SmartBoardingHouse.Service;
using static SmartBoardingHouse.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IMongoCollection<Invoice> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<InvoiceRequest> _validator;
        private readonly IMapper _mapper;
        private readonly ActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public InvoicesController(
            MongoDbService mongoService,
            IValidator<InvoiceRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService,
            INotificationService notificationService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Invoice>("invoices");
            _userCollection = db.GetCollection<User>("users");
            _roomCollection = db.GetCollection<Room>("rooms");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<List<InvoiceResponse>>> GetAll()
        {
            var invoices = await _collection.Find(_ => true).ToListAsync();
            var responses = await Task.WhenAll(invoices.Select(MapToResponse));
            return Ok(responses.ToList());
        }

        // GET: api/Invoices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceResponse>> GetById(string id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            return Ok(await MapToResponse(invoice));
        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<ActionResult<InvoiceResponse>> Create(InvoiceRequest request)
        {
            var errors = await ValidateRequest(request);

            var invoiceExists = await _collection
                .Find(x => x.InvoiceNumber == request.InvoiceNumber)
                .AnyAsync();
            if (invoiceExists)
                errors.Add(CommonMessage.InvoiceNumberExists());

            var roomExists = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .FirstOrDefaultAsync();
            if (roomExists is null)
                errors.Add(CommonMessage.NotFound("Phòng"));

            var userExists = await _userCollection
                .Find(x => x.Name == request.TenantName)
                .FirstOrDefaultAsync();
            if (userExists is null)
                errors.Add(CommonMessage.NotFound("Người thuê"));

            var contractExists = await _contractCollection
                .Find(x => x.ContractNumber == request.ContractNumber)
                .FirstOrDefaultAsync();
            if (contractExists is null)
                errors.Add(CommonMessage.NotFound("Hợp đồng"));

            if (errors.Any())
                return BadRequest(errors);

            if (userExists is null || contractExists is null || roomExists is null)
                return BadRequest(CommonMessage.NotFound("Các dữ liệu liên quan"));

            if (roomExists.Price != request.RoomPrice)
                return BadRequest(CommonMessage.RoomPriceMismatch());

            var invoice = _mapper.Map<Invoice>(request);
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.ContractId = contractExists.Id;
            invoice.TenantId = userExists.Id;
            invoice.RoomId = roomExists.Id;
            invoice.RoomDeposit = request.RoomDeposit > 0 ? request.RoomDeposit : roomExists.RoomDeposit;

            var amont = invoice.RoomPrice + (decimal)invoice.ElectricUsage * invoice.ElectricPrice + (decimal)invoice.WaterUsage * invoice.WaterPrice + invoice.ServiceFee;
            if (request.Items != null && request.Items.Length > 0)
            {
                foreach (var item in request.Items)
                {
                    var total = item.Quantity * item.UnitPrice;
                    invoice.Items.Add(new InvoiceItem
                    {
                        Name = item.Name,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        Total = total
                    });
                    amont += total;
                }
            }
            invoice.Amount = amont;

            await _collection.InsertOneAsync(invoice);

            await _notificationService.CreateAsync(
                tenantId: invoice.TenantId,
                title: "Hóa đơn mới",
                body: $"Hóa đơn {invoice.InvoiceNumber} tháng {invoice.BillingMonth}/{invoice.BillingYear} với số tiền {invoice.Amount:N0}đ đã được tạo. Hạn thanh toán: {invoice.DueDate:dd/MM/yyyy}.",
                type: NotificationType.Invoice,
                refId: invoice.Id,
                refModel: "Invoice");

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id },
                await MapToResponse(invoice));
        }

        // PUT: api/Invoices/{id}
        //[HttpPut("{id}")]
        //public async Task<ActionResult<InvoiceResponse>> Update(int id, InvoiceRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var existingInvoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (existingInvoice is null)
        //        return NotFound(CommonMessage.NotFound("Invoice"));

        //    var invoiceExists = await _collection
        //        .Find(x => x.InvoiceNumber == request.InvoiceNumber && x.Id != id)
        //        .AnyAsync();
        //    if (invoiceExists)
        //        errors.Add($"Mã hóa đơn '{request.InvoiceNumber}' đã tồn tại.");

        //    var roomExists = await _roomCollection
        //        .Find(x => x.RoomNumber == request.RoomNumber)
        //        .AnyAsync();
        //    if (!roomExists)
        //        errors.Add(CommonMessage.NotFound("Room"));

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var updatedInvoice = _mapper.Map<Invoice>(request);
        //    updatedInvoice.Id = id;
        //    updatedInvoice.CreatedAt = existingInvoice.CreatedAt;
        //    updatedInvoice.UpdatedAt = DateTime.UtcNow;

        //    await _collection.ReplaceOneAsync(x => x.Id == id, updatedInvoice);
        //    return Ok(await MapToResponse(updatedInvoice));
        //}

        // PUT: api/Invoices/{id}/pay
        [HttpPut("{id}/pay")]
        public async Task<ActionResult<InvoiceResponse>> MarkAsPaid(string id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            if (invoice.Status == InvoiceStatus.Paid)
                return BadRequest(CommonMessage.InvoiceAlreadyPaid());

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Invoice>.Update
                    .Set(x => x.Status, InvoiceStatus.Paid)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            //await _activityLogService.LogAsync(
            //    type: ActivityType.Payment,
            //    userName: invoice.TenantName,
            //    roomNumber: invoice.RoomNumber,
            //    description: $"Đã thanh toán {invoice.Amount / 1_000_000:0.#} triệu đồng",
            //    amount: invoice.Amount);

            invoice.Status = InvoiceStatus.Paid;

            await _notificationService.CreateAsync(
                tenantId: invoice.TenantId,
                title: "Thanh toán thành công",
                body: $"Hóa đơn {invoice.InvoiceNumber} đã được xác nhận thanh toán {invoice.Amount:N0}đ.",
                type: NotificationType.Invoice,
                refId: invoice.Id,
                refModel: "Invoice");

            return Ok(await MapToResponse(invoice));
        }

        // DELETE: api/Invoices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            await _collection.DeleteOneAsync(x => x.Id == id);
            return Ok(CommonMessage.Deleted("Hóa đơn"));
        }

        // ==================== STATUS TRANSITIONS ====================

        // PUT: api/Invoices/{id}/confirm-payment 
        [HttpPut("{id}/confirm-payment")]
        public Task<ActionResult<InvoiceResponse>> ConfirmPayment(string id) =>
            ChangeStatusAsync(
                id,
                newStatus: InvoiceStatus.Paid,
                allowedCurrentStatuses: new[] { InvoiceStatus.Pending, InvoiceStatus.Unpaid, InvoiceStatus.Overdue },
                notificationTitle: "Thanh toán thành công",
                notificationBody: invoice => $"Hóa đơn {invoice.InvoiceNumber} đã được xác nhận thanh toán {invoice.Amount:N0}đ.");

        // PUT: api/Invoices/{id}/cancel  (Pending -> Cancelled)
        [HttpPut("{id}/cancel")]
        public Task<ActionResult<InvoiceResponse>> Cancel(string id) =>
            ChangeStatusAsync(
                id,
                newStatus: InvoiceStatus.Cancelled,
                allowedCurrentStatuses: new[] { InvoiceStatus.Pending, InvoiceStatus.Unpaid },
                notificationTitle: "Hóa đơn đã bị hủy",
                notificationBody: invoice => $"Hóa đơn {invoice.InvoiceNumber} đã bị hủy.");

        // PUT: api/Invoices/{id}/reactivate  (Cancelled -> Unpaid)
        [HttpPut("{id}/reactivate")]
        public Task<ActionResult<InvoiceResponse>> Reactivate(string id) =>
            ChangeStatusAsync(
                id,
                newStatus: InvoiceStatus.Unpaid,
                allowedCurrentStatuses: new[] { InvoiceStatus.Cancelled },
                notificationTitle: null,
                notificationBody: null);

        // ==================== STATUS HELPER ====================

        private async Task<ActionResult<InvoiceResponse>> ChangeStatusAsync(
            string id,
            InvoiceStatus newStatus,
            InvoiceStatus[] allowedCurrentStatuses,
            string? notificationTitle,
            Func<Invoice, string>? notificationBody)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            if (!allowedCurrentStatuses.Contains(invoice.Status))
                return BadRequest($"Không thể chuyển hóa đơn từ trạng thái '{invoice.Status}' sang '{newStatus}'.");

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Invoice>.Update
                    .Set(x => x.Status, newStatus)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            invoice.Status = newStatus;

            if (notificationTitle != null && notificationBody != null)
            {
                await _notificationService.CreateAsync(
                    tenantId: invoice.TenantId,
                    title: notificationTitle,
                    body: notificationBody(invoice),
                    type: NotificationType.Invoice,
                    refId: invoice.Id,
                    refModel: "Invoice");
            }

            return Ok(await MapToResponse(invoice));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(InvoiceRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private async Task<InvoiceResponse> MapToResponse(Invoice invoice)
        {
            var response = _mapper.Map<InvoiceResponse>(invoice);
            response.ElectricTotal = (decimal)response.ElectricUsage * response.ElectricPrice;
            response.WaterTotal = (decimal)response.WaterUsage * response.WaterPrice;

            response.Status = invoice.Status;
            response.StatusLabel = invoice.Status switch
            {
                InvoiceStatus.Paid => "Đã thanh toán",
                InvoiceStatus.Unpaid => "Chờ thanh toán",
                InvoiceStatus.Pending => "Đang xử lý",
                InvoiceStatus.Cancelled => "Đã hủy",
                _ => invoice.Status.ToString()
            };
            response.BillingPeriod = "Tháng " + invoice.BillingMonth + "/" + invoice.BillingYear;

            var room = await _roomCollection
                .Find(x => x.Id == invoice.RoomId)
                .FirstOrDefaultAsync();
            response.RoomNumber = room?.RoomNumber ?? "Không tìm thấy";
            response.RoomDeposit = invoice.RoomDeposit > 0 ? invoice.RoomDeposit : (room?.RoomDeposit ?? 0);
            response.Type = invoice.Type;

            var tenant = await _userCollection
                .Find(x => x.Id == invoice.TenantId)
                .FirstOrDefaultAsync();
            response.TenantName = tenant?.Name ?? "Không tìm thấy";

            return response;
        }
    }
}