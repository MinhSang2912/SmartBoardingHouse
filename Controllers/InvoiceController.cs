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

        public InvoicesController(
            MongoDbService mongoService,
            IValidator<InvoiceRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Invoice>("invoices");
            _userCollection = db.GetCollection<User>("users");
            _roomCollection = db.GetCollection<Room>("rooms");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<List<InvoiceResponse>>> GetAll()
        {
            var invoices = await _collection.Find(_ => true).ToListAsync();
            return Ok(invoices.Select(MapToResponse).ToList());
        }

        // GET: api/Invoices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceResponse>> GetById(string id)
        {
            var invoice = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (invoice is null)
                return NotFound(CommonMessage.NotFound("Hóa đơn"));

            return Ok(MapToResponse(invoice));
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

            var amont = invoice.RoomPrice + (decimal)invoice.ElectricUsage * invoice.ElectricPrice + (decimal)invoice.WaterUsage * invoice.WaterPrice + invoice.ServiceFee;
            if(request.Items != null && request.Items.Length > 0)
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

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id },
                MapToResponse(invoice));
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
        //    return Ok(MapToResponse(updatedInvoice));
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

            await _activityLogService.LogAsync(
                type: ActivityType.Payment,
                userName: invoice.TenantName,
                roomNumber: invoice.RoomNumber,
                description: $"Đã thanh toán {invoice.Amount / 1_000_000:0.#} triệu đồng",
                amount: invoice.Amount);

            invoice.Status = InvoiceStatus.Paid;
            return Ok(MapToResponse(invoice));
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

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(InvoiceRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private InvoiceResponse MapToResponse(Invoice invoice)
        {
            var response = _mapper.Map<InvoiceResponse>(invoice);
            response.ElectricTotal = (decimal)response.ElectricUsage * response.ElectricPrice;
            response.WaterTotal = (decimal)response.WaterUsage * response.WaterPrice;

            var effectiveStatus = invoice.Status == InvoiceStatus.Unpaid && invoice.DueDate < DateTime.Now
                ? InvoiceStatus.Overdue
                : invoice.Status;

            response.Status = effectiveStatus;
            response.StatusLabel = effectiveStatus switch
            {
                InvoiceStatus.Paid    => "Đã thanh toán",
                InvoiceStatus.Unpaid  => "Chờ thanh toán",
                InvoiceStatus.Overdue => "Quá hạn",
                _                     => effectiveStatus.ToString()
            };
            response.BillingPeriod = "Tháng " + invoice.BillingMonth + "/" + invoice.BillingYear;

            return response;
        }
    }
}