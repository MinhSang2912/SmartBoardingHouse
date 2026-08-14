using AutoMapper;
using FluentValidation;
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
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IValidator<MaintenanceRequestRequest> _validator;
        private readonly IMapper _mapper;
        private readonly ActivityLogService _activityLogService;
        private readonly INotificationService _notificationService;

        public MaintenanceRequestsController(
            MongoDbService mongoService,
            IValidator<MaintenanceRequestRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService,
            INotificationService notificationService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<MaintenanceRequest>("maintenancerequests");
            _roomCollection = db.GetCollection<Room>("rooms");
            _userCollection = db.GetCollection<User>("users");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
            _notificationService = notificationService;
        }

        // GET: api/MaintenanceRequests
        [HttpGet]
        public async Task<ActionResult<MaintenanceSummaryResponse>> GetAll()
        {
            var items = await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();

            var mapped = new List<MaintenanceRequestResponse>();
            foreach (var item in items)
            {
                mapped.Add(await MapToResponseAsync(item));
            }

            var summary = new MaintenanceSummaryResponse
            {
                Total = mapped.Count,
                Pending = mapped.Count(x => x.Status == MaintenanceStatus.Pending),
                InProgress = mapped.Count(x => x.Status == MaintenanceStatus.InProgress),
                Completed = mapped.Count(x => x.Status == MaintenanceStatus.Completed),
                Items = mapped
            };

            return Ok(summary);
        }

        // GET: api/MaintenanceRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequestResponse>> GetById(string id)
        {
            var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item is null)
                return NotFound(CommonMessage.NotFound("Yêu cầu bảo trì"));

            return Ok(await MapToResponseAsync(item));
        }

        // POST: api/MaintenanceRequests
        [HttpPost]
        public async Task<ActionResult<MaintenanceRequestResponse>> Create(MaintenanceRequestRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage).ToList());

            // Tìm phòng theo RoomNumber
            var room = await _roomCollection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .FirstOrDefaultAsync();

            if (room is null)
                return BadRequest(CommonMessage.NotFound("Phòng"));

            // Tìm tenant theo tên (hoặc lấy từ room.TenantId nếu có)
            User? tenant = null;
            if (!string.IsNullOrEmpty(room.TenantId))
            {
                tenant = await _userCollection
                    .Find(x => x.Id == room.TenantId)
                    .FirstOrDefaultAsync();
            }

            if (tenant is null)
            {
                tenant = await _userCollection
                    .Find(x => x.Name == request.TenantName)
                    .FirstOrDefaultAsync();
            }

            if (tenant is null)
                return BadRequest(CommonMessage.NotFound("Người thuê"));

            // Kiểm tra RequestNumber trùng
            var exists = await _collection
                .Find(x => x.RequestNumber == request.RequestNumber)
                .AnyAsync();
            if (exists)
                return BadRequest("Mã yêu cầu đã tồn tại");

            var item = _mapper.Map<MaintenanceRequest>(request);
            item.RoomId = room.Id;
            item.TenantId = tenant.Id;
            item.RoomNumber = room.RoomNumber;
            item.TenantName = tenant.Name;
            item.Status = MaintenanceStatus.Pending;
            item.CreatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(item);

            await _activityLogService.LogAsync(
                type: ActivityType.Maintenance,
                userName: tenant.Name,
                roomNumber: room.RoomNumber,
                description: $"Tạo yêu cầu bảo trì: {item.Title}");

            await _notificationService.CreateAsync(
                tenantId: tenant.Id,
                title: "Yêu cầu bảo trì đã được ghi nhận",
                body: $"Yêu cầu \"{item.Title}\" của phòng {room.RoomNumber} đã được tiếp nhận.",
                type: NotificationType.Maintenance,
                refId: item.Id,
                refModel: "MaintenanceRequest");

            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                await MapToResponseAsync(item));
        }

        // PUT: api/MaintenanceRequests/{id}/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<MaintenanceRequestResponse>> UpdateStatus(
            string id, [FromBody] MaintenanceStatus newStatus)
        {
            var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item is null)
                return NotFound(CommonMessage.NotFound("Yêu cầu bảo trì"));

            var update = Builders<MaintenanceRequest>.Update
                .Set(x => x.Status, newStatus)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (newStatus == MaintenanceStatus.Completed)
                update = update.Set(x => x.ResolvedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(x => x.Id == id, update);

            item.Status = newStatus;
            if (newStatus == MaintenanceStatus.Completed)
                item.ResolvedAt = DateTime.UtcNow;

            string title = newStatus switch
            {
                MaintenanceStatus.InProgress => "Yêu cầu đang được xử lý",
                MaintenanceStatus.Completed => "Yêu cầu sửa chữa đã hoàn thành",
                _ => "Cập nhật trạng thái yêu cầu bảo trì"
            };

            string body = newStatus switch
            {
                MaintenanceStatus.InProgress => $"Yêu cầu \"{item.Title}\" (phòng {item.RoomNumber}) đang được xử lý.",
                MaintenanceStatus.Completed => $"Yêu cầu \"{item.Title}\" (phòng {item.RoomNumber}) đã được xử lý xong.",
                _ => $"Yêu cầu \"{item.Title}\" đã được cập nhật trạng thái."
            };

            await _notificationService.CreateAsync(
                tenantId: item.TenantId,
                title: title,
                body: body,
                type: NotificationType.Maintenance,
                refId: item.Id,
                refModel: "MaintenanceRequest");

            if (newStatus == MaintenanceStatus.Completed)
            {
                await _activityLogService.LogAsync(
                    type: ActivityType.Maintenance,
                    userName: item.TenantName,
                    roomNumber: item.RoomNumber,
                    description: item.Title);
            }

            return Ok(await MapToResponseAsync(item));
        }

        // ==================== HELPERS ====================

        private async Task<MaintenanceRequestResponse> MapToResponseAsync(MaintenanceRequest item)
        {
            var response = _mapper.Map<MaintenanceRequestResponse>(item);

            // Lấy RoomNumber từ Room theo RoomId
            if (!string.IsNullOrEmpty(item.RoomId))
            {
                var room = await _roomCollection
                    .Find(r => r.Id == item.RoomId)
                    .FirstOrDefaultAsync();

                if (room != null)
                    response.RoomNumber = room.RoomNumber;
            }

            // Lấy TenantName từ User theo TenantId
            if (!string.IsNullOrEmpty(item.TenantId))
            {
                var tenant = await _userCollection
                    .Find(u => u.Id == item.TenantId)
                    .FirstOrDefaultAsync();

                if (tenant != null)
                    response.TenantName = tenant.Name;
            }

            response.PriorityLabel = item.Priority switch
            {
                PriotyRequest.High => "Cao",
                PriotyRequest.Medium => "Trung bình",
                PriotyRequest.Low => "Thấp",
                _ => item.Priority.ToString()
            };

            response.StatusLabel = item.Status switch
            {
                MaintenanceStatus.Pending => "Chờ xử lý",
                MaintenanceStatus.InProgress => "Đang xử lý",
                MaintenanceStatus.Completed => "Hoàn thành",
                _ => item.Status.ToString()
            };

            return response;
        }
    }
}