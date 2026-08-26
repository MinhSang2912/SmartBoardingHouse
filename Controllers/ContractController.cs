using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Service;
using SmartBoardingHouse.Services;
using static SmartBoardingHouse.Common.Enums;
using CommonMessage = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Invoice> _invoiceCollection;
        private readonly IValidator<ContractRequest> _validator;
        private readonly IMapper _mapper;
        private readonly ActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;
        private readonly EmailService _emailService;

        public ContractsController(
            MongoDbService mongoService,
            IValidator<ContractRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService,
            INotificationService notificationService,
            EmailService emailService)
        {
            var db = mongoService.GetDatabase();
            _contractCollection = db.GetCollection<Contract>("contracts");
            _roomCollection = db.GetCollection<Room>("rooms");
            _userCollection = db.GetCollection<User>("users");
            _invoiceCollection = db.GetCollection<Invoice>("invoices");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // GET: api/Contracts
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? page = null, [FromQuery] int? limit = null)
        {
            // 1. Fetch contracts
            List<Contract> contracts;
            int total = 0;
            if (page.HasValue && limit.HasValue)
            {
                int p = page.Value < 1 ? 1 : page.Value;
                int l = limit.Value < 1 ? 10 : limit.Value;
                total = (int)await _contractCollection.CountDocumentsAsync(_ => true);
                contracts = await _contractCollection.Find(_ => true)
                    .Skip((p - 1) * l)
                    .Limit(l)
                    .ToListAsync();
            }
            else
            {
                contracts = await _contractCollection.Find(_ => true).ToListAsync();
                total = contracts.Count;
            }

            // 2. Bulk load related rooms and users to solve N+1 queries
            var roomIds = contracts.Select(c => c.RoomId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var tenantIds = contracts.Select(c => c.TenantId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var rooms = await _roomCollection.Find(r => roomIds.Contains(r.Id)).ToListAsync();
            var users = await _userCollection.Find(u => tenantIds.Contains(u.Id)).ToListAsync();

            var roomDict = rooms.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
            var userDict = users.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());

            var responses = contracts.Select(contract => MapToResponse(contract, roomDict, userDict)).ToList();

            if (page.HasValue && limit.HasValue)
            {
                return Ok(new PagedResult<ContractResponse>
                {
                    Total = total,
                    Page = page.Value,
                    Limit = limit.Value,
                    Items = responses
                });
            }
            else
            {
                return Ok(responses);
            }
        }

        // GET: api/Contracts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractResponse>> GetById(string id)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            return Ok(await MapToResponseAsync(contract));
        }

        // POST: api/Contracts
        [HttpPost]
        public async Task<ActionResult<ContractResponse>> Create(ContractRequest request)
        {
            var errors = await ValidateRequest(request);

            var contractNumber = "HD-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            // Kiểm tra số hợp đồng trùng
            var contractExists = await _contractCollection
                .Find(x => x.ContractNumber == contractNumber)
                .AnyAsync();
            if (contractExists)
                return BadRequest(CommonMessage.ContractNumberExists(contractNumber));

            // Tìm phòng theo RoomNumber
            var room = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .FirstOrDefaultAsync();
            if (room is null)
                return BadRequest(CommonMessage.NotFound("Phòng"));

            // Phòng đã có hợp đồng đang hiệu lực
            var activeContractForRoom = await _contractCollection
                .Find(x => x.RoomId == room.Id && x.Status == ContractStatus.Active)
                .AnyAsync();
            if (activeContractForRoom)
                errors.Add(CommonMessage.ContractRoomIsExists());

            //// Tìm người thuê theo tên
            var tenant = await _userCollection
                .Find(x => x.Name == request.TenantName)
                .FirstOrDefaultAsync();
            if (tenant is null)
                return BadRequest(CommonMessage.NotFound("Người thuê"));

            if (errors.Any())
                return BadRequest(errors);

            var contract = _mapper.Map<Contract>(request);
            contract.ContractNumber = contractNumber;
            contract.CreatedAt = DateTime.UtcNow;
            contract.Status = ContractStatus.Active;
            contract.SignedDate = DateTime.UtcNow;
            contract.RoomId = room.Id;
            contract.TenantId = tenant.Id;
            contract.RoomNumber = room.RoomNumber;
            contract.TenantName = tenant.Name;
            contract.RoomDeposit = room.RoomDeposit;

            await _contractCollection.InsertOneAsync(contract);

            // Cập nhật Room
            await _roomCollection.UpdateOneAsync(
                x => x.Id == room.Id,
                Builders<Room>.Update
                    .Set(x => x.Status, RoomStatus.Occupied)
                    .Set(x => x.TenantId, tenant.Id)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            // Cập nhật User
            await _userCollection.UpdateOneAsync(
                x => x.Id == tenant.Id,
                Builders<User>.Update
                    .Set(x => x.RoomId, room.Id)
                    .Set(x => x.RoomNumber, room.RoomNumber)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _activityLogService.LogAsync(
                type: ActivityType.CheckIn,
                userName: tenant.Name,
                roomNumber: room.RoomNumber,
                description: $"Phòng {room.RoomNumber} được {tenant.Name} thuê vào {DateTime.Now:dd/MM/yyyy}");

            await _notificationService.CreateAsync(
                tenantId: tenant.Id,
                title: "Hợp đồng mới đã được tạo",
                body: $"Hợp đồng số {contract.ContractNumber} cho phòng {room.RoomNumber} đã được tạo thành công.",
                type: NotificationType.Contract,
                refId: contract.Id,
                refModel: "Contract");

            // Gửi email hợp đồng cho người thuê (fire-and-forget)
            var tenantEmail = tenant.Email;
            var tenantName = tenant.Name;
            var roomNumber = room.RoomNumber;
            var startDate = contract.StartDate.ToString("dd/MM/yyyy");
            var endDate = contract.EndDate.ToString("dd/MM/yyyy");
            var price = contract.Price.ToString("N0");
            var deposit = contract.RoomDeposit.ToString("N0");
            var contractNum = contract.ContractNumber;
            var paymentDate = contract.PaymentDate;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendContractEmailAsync(
                        tenantEmail, tenantName, contractNum, roomNumber,
                        startDate, endDate, price, deposit, paymentDate);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Email Error] Lỗi gửi email hợp đồng tới {tenantEmail}: {ex.Message}");
                }
            });

            return CreatedAtAction(nameof(GetById), new { id = contract.Id },
                await MapToResponseAsync(contract));
        }

        // PUT: api/Contracts/{id}/terminate
        [HttpPut("{id}/terminate")]
        public async Task<ActionResult<ContractResponse>> Terminate(string id)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            if (contract.Status != ContractStatus.Active)
                return BadRequest(CommonMessage.ContractStatusNotActive());

            // Kiểm tra các hóa đơn chưa thanh toán
            var unpaidInvoices = await _invoiceCollection
                .Find(x => x.ContractId == id && x.Status != InvoiceStatus.Paid)
                .AnyAsync();

            if (unpaidInvoices)
                return BadRequest("Không thể thanh lý hợp đồng vì còn hóa đơn chưa thanh toán.");
            
            await _contractCollection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Contract>.Update
                    .Set(x => x.Status, ContractStatus.Terminated)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            // Cập nhật Room theo RoomId
            if (!string.IsNullOrEmpty(contract.RoomId))
            {
                await _roomCollection.UpdateOneAsync(
                    x => x.Id == contract.RoomId,
                    Builders<Room>.Update
                        .Set(x => x.Status, RoomStatus.Available)
                        .Set(x => x.TenantId, null)
                        .Set(x => x.UpdatedAt, DateTime.UtcNow));
            }

            // Cập nhật User theo TenantId
            if (!string.IsNullOrEmpty(contract.TenantId))
            {
                await _userCollection.UpdateOneAsync(
                    x => x.Id == contract.TenantId,
                    Builders<User>.Update
                        .Set(x => x.RoomId, null)
                        .Set(x => x.RoomNumber, "Chưa có phòng")
                        .Set(x => x.UpdatedAt, DateTime.UtcNow));
            }

            await _activityLogService.LogAsync(
                type: ActivityType.CheckOut,
                userName: contract.TenantName,
                roomNumber: contract.RoomNumber,
                description: $"Phòng {contract.RoomNumber} được {contract.TenantName} trả phòng vào {DateTime.Now:dd/MM/yyyy}");

            await _notificationService.CreateAsync(
                tenantId: contract.TenantId,
                title: "Hợp đồng đã chấm dứt",
                body: $"Hợp đồng số {contract.ContractNumber} cho phòng {contract.RoomNumber} đã được chấm dứt.",
                type: NotificationType.Contract,
                refId: contract.Id,
                refModel: "Contract");

            contract.Status = ContractStatus.Terminated;
            return Ok(await MapToResponseAsync(contract));
        }

        // PUT: api/Contracts/{id}/extend
        [HttpPut("{id}/extend")]
        public async Task<ActionResult<ContractResponse>> Extend(string id, [FromBody] DateTime newEndDate)
        {
            var contract = await _contractCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (contract is null)
                return NotFound(CommonMessage.NotFound("Hợp đồng"));

            if (newEndDate <= contract.EndDate)
                return BadRequest("Ngày kết thúc mới phải lớn hơn ngày kết thúc hiện tại");

            await _contractCollection.UpdateOneAsync(
                x => x.Id == id,
                Builders<Contract>.Update
                    .Set(x => x.EndDate, newEndDate)
                    .Set(x => x.Status, ContractStatus.Active)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            contract.EndDate = newEndDate;
            contract.Status = ContractStatus.Active;

            await _notificationService.CreateAsync(
                tenantId: contract.TenantId,
                title: "Hợp đồng đã được gia hạn",
                body: $"Hợp đồng số {contract.ContractNumber} đã được gia hạn đến ngày {newEndDate:dd/MM/yyyy}.",
                type: NotificationType.Contract,
                refId: contract.Id,
                refModel: "Contract");

            return Ok(await MapToResponseAsync(contract));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(ContractRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private ContractResponse MapToResponse(Contract contract, Dictionary<string, Room> roomDict, Dictionary<string, User> userDict)
        {
            var response = _mapper.Map<ContractResponse>(contract);

            if (!string.IsNullOrEmpty(contract.RoomId) && roomDict.TryGetValue(contract.RoomId, out var room))
            {
                response.RoomNumber = room.RoomNumber;
            }

            if (!string.IsNullOrEmpty(contract.TenantId) && userDict.TryGetValue(contract.TenantId, out var tenant))
            {
                response.TenantName = tenant.Name;
            }

            response.StatusLabel = contract.Status switch
            {
                ContractStatus.Active => "Đang hiệu lực",
                ContractStatus.Expired => "Hết hạn",
                ContractStatus.Terminated => "Đã chấm dứt",
                _ => contract.Status.ToString()
            };

            response.PaymentDateLabel = $"Ngày {contract.PaymentDate} hàng tháng";
            response.RemainTime = contract.RemainTime;

            return response;
        }

        private async Task<ContractResponse> MapToResponseAsync(Contract contract)
        {
            var response = _mapper.Map<ContractResponse>(contract);

            // Lấy Room theo RoomId
            if (!string.IsNullOrEmpty(contract.RoomId))
            {
                var room = await _roomCollection
                    .Find(r => r.Id == contract.RoomId)
                    .FirstOrDefaultAsync();

                if (room != null)
                    response.RoomNumber = room.RoomNumber;
            }

            // Lấy Tenant theo TenantId
            if (!string.IsNullOrEmpty(contract.TenantId))
            {
                var tenant = await _userCollection
                    .Find(u => u.Id == contract.TenantId)
                    .FirstOrDefaultAsync();

                if (tenant != null)
                    response.TenantName = tenant.Name;
            }

            response.StatusLabel = contract.Status switch
            {
                ContractStatus.Active => "Đang hiệu lực",
                ContractStatus.Expired => "Hết hạn",
                ContractStatus.Terminated => "Đã chấm dứt",
                _ => contract.Status.ToString()
            };

            response.PaymentDateLabel = $"Ngày {contract.PaymentDate} hàng tháng";
            response.RemainTime = contract.RemainTime;

            return response;
        }
    }
}