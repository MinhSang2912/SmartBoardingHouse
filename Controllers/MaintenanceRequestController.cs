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

            if (items.Count == 0)
            {
                return Ok(new MaintenanceSummaryResponse
                {
                    Total = 0,
                    Pending = 0,
                    InProgress = 0,
                    Completed = 0,
                    Items = new List<MaintenanceRequestResponse>()
                });
            }

            var roomIds = items
                .Where(x => !string.IsNullOrEmpty(x.RoomId))
                .Select(x => x.RoomId)
                .Distinct()
                .ToList();

            var tenantIds = items
                .Where(x => !string.IsNullOrEmpty(x.TenantId))
                .Select(x => x.TenantId)
                .Distinct()
                .ToList();

            var roomsTask = roomIds.Count > 0
                ? _roomCollection
                    .Find(x => roomIds.Contains(x.Id))
                    .ToListAsync()
                : Task.FromResult(new List<Room>());

            var tenantsTask = tenantIds.Count > 0
                ? _userCollection
                    .Find(x => tenantIds.Contains(x.Id))
                    .ToListAsync()
                : Task.FromResult(new List<User>());

            await Task.WhenAll(roomsTask, tenantsTask);

            var rooms = roomsTask.Result;
            var tenants = tenantsTask.Result;

            var roomDict = rooms
                .Where(x => !string.IsNullOrEmpty(x.Id))
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());

            var tenantDict = tenants
                .Where(x => !string.IsNullOrEmpty(x.Id))
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());

            var mapped = items
                .Select(item =>
                {
                    roomDict.TryGetValue(item.RoomId ?? string.Empty, out var room);
                    tenantDict.TryGetValue(item.TenantId ?? string.Empty, out var tenant);

                    return MapToResponse(item, room, tenant);
                })
                .ToList();

            var summary = new MaintenanceSummaryResponse
            {
                Total = mapped.Count,
                Pending = mapped.Count(x => x.Status == MaintenanceStatus.Pending),
                InProgress = mapped.Count(x => x.Status == MaintenanceStatus.Processing),
                Completed = mapped.Count(x => x.Status == MaintenanceStatus.Completed),
                Items = mapped
            };

            return Ok(summary);
        }

        // GET: api/MaintenanceRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequestResponse>> GetById(string id)
        {
            var item = await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (item is null)
                return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

            Room? room = null;
            User? tenant = null;

            var tasks = new List<Task>();

            if (!string.IsNullOrEmpty(item.RoomId))
            {
                tasks.Add(
                    _roomCollection
                        .Find(x => x.Id == item.RoomId)
                        .FirstOrDefaultAsync()
                        .ContinueWith(t => room = t.Result));
            }

            if (!string.IsNullOrEmpty(item.TenantId))
            {
                tasks.Add(
                    _userCollection
                        .Find(x => x.Id == item.TenantId)
                        .FirstOrDefaultAsync()
                        .ContinueWith(t => tenant = t.Result));
            }

            await Task.WhenAll(tasks);

            return Ok(MapToResponse(item, room, tenant));
        }

        // POST: api/MaintenanceRequests
        //[HttpPost]
        //public async Task<ActionResult<MaintenanceRequestResponse>> Create(
        //    MaintenanceRequestRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var numberExists = await _collection
        //        .Find(x => x.RequestNumber == request.RequestNumber)
        //        .AnyAsync();

        //    if (numberExists)
        //        errors.Add($"Mã yêu cầu '{request.RequestNumber}' đã tồn tại.");

        //    var roomExists = await _roomCollection
        //        .Find(x => x.RoomNumber == request.RoomNumber)
        //        .AnyAsync();

        //    if (!roomExists)
        //        errors.Add(CommonMessage.NotFound("Room"));

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var item = _mapper.Map<MaintenanceRequest>(request);
        //    item.CreatedAt = DateTime.UtcNow;

        //    await _collection.InsertOneAsync(item);

        //    return CreatedAtAction(
        //        nameof(GetById),
        //        new { id = item.Id },
        //        MapToResponse(item, null, null));
        //}

        // PUT: api/MaintenanceRequests/{id}
        //[HttpPut("{id}")]
        //public async Task<ActionResult<MaintenanceRequestResponse>> Update(
        //    string id,
        //    MaintenanceRequestRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var existing = await _collection
        //        .Find(x => x.Id == id)
        //        .FirstOrDefaultAsync();

        //    if (existing is null)
        //        return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

        //    var numberExists = await _collection
        //        .Find(x =>
        //            x.RequestNumber == request.RequestNumber &&
        //            x.Id != id)
        //        .AnyAsync();

        //    if (numberExists)
        //        errors.Add($"Mã yêu cầu '{request.RequestNumber}' đã tồn tại.");

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var updated = _mapper.Map<MaintenanceRequest>(request);

        //    updated.Id = id;
        //    updated.CreatedAt = existing.CreatedAt;
        //    updated.UpdatedAt = DateTime.UtcNow;

        //    await _collection.ReplaceOneAsync(
        //        x => x.Id == id,
        //        updated);

        //    return Ok(MapToResponse(updated, null, null));
        //}

        // PUT: api/MaintenanceRequests/{id}/start
        [HttpPut("{id}/start")]
        public async Task<ActionResult<MaintenanceRequestResponse>> Start(string id)
        {
            var item = await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (item is null)
                return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

            if (item.Status != MaintenanceStatus.Pending)
                return BadRequest(CommonMessage.JustStartThePendingRequest());

            var now = DateTime.UtcNow;

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<MaintenanceRequest>.Update
                    .Set(x => x.Status, MaintenanceStatus.Processing)
                    .Set(x => x.UpdatedAt, now));

            item.Status = MaintenanceStatus.Processing;
            item.UpdatedAt = now;

            await _notificationService.CreateAsync(
                tenantId: item.TenantId,
                title: "Yêu cầu sửa chữa đang được xử lý",
                body: $"Yêu cầu \"{item.Title}\" (phòng {item.RoomNumber}) của bạn đang được xử lý.",
                type: NotificationType.Maintenance,
                refId: item.Id,
                refModel: "MaintenanceRequest");

            return Ok(MapToResponse(item, null, null));
        }

        // PUT: api/MaintenanceRequests/{id}/complete
        [HttpPut("{id}/complete")]
        public async Task<ActionResult<MaintenanceRequestResponse>> Complete(string id)
        {
            var item = await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (item is null)
                return NotFound(CommonMessage.NotFound("Yêu cầu sửa chữa"));

            if (item.Status != MaintenanceStatus.Processing)
                return BadRequest(
                    CommonMessage.JustCompleteTheInProgressRequest());

            var now = DateTime.UtcNow;

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<MaintenanceRequest>.Update
                    .Set(x => x.Status, MaintenanceStatus.Completed)
                    .Set(x => x.UpdatedAt, now));

            await _activityLogService.LogAsync(
                type: ActivityType.Maintenance,
                userName: item.TenantName,
                roomNumber: item.RoomNumber,
                description: item.Title);

            item.Status = MaintenanceStatus.Completed;
            item.UpdatedAt = now;

            await _notificationService.CreateAsync(
                tenantId: item.TenantId,
                title: "Yêu cầu sửa chữa đã hoàn thành",
                body: $"Yêu cầu \"{item.Title}\" (phòng {item.RoomNumber}) của bạn đã được xử lý xong.",
                type: NotificationType.Maintenance,
                refId: item.Id,
                refModel: "MaintenanceRequest");

            return Ok(MapToResponse(item, null, null));
        }

        // DELETE: api/MaintenanceRequests/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(string id)
        //{
        //    var item = await _collection
        //        .Find(x => x.Id == id)
        //        .FirstOrDefaultAsync();

        //    if (item is null)
        //        return NotFound(
        //            CommonMessage.NotFound("MaintenanceRequest"));

        //    await _collection.DeleteOneAsync(x => x.Id == id);

        //    return Ok(
        //        CommonMessage.Deleted("MaintenanceRequest"));
        //}

        // ==================== HELPERS ====================

        //private async Task<List<string>> ValidateRequest(
        //    MaintenanceRequestRequest request)
        //{
        //    var result = await _validator.ValidateAsync(request);

        //    return result.Errors
        //        .Select(e => e.ErrorMessage)
        //        .ToList();
        //}

        private MaintenanceRequestResponse MapToResponse(
            MaintenanceRequest item,
            Room? room,
            User? tenant)
        {
            var response =
                _mapper.Map<MaintenanceRequestResponse>(item);

            if (room != null)
                response.RoomNumber = room.RoomNumber;
            else if (!string.IsNullOrEmpty(item.RoomNumber))
                response.RoomNumber = item.RoomNumber;

            if (tenant != null)
                response.TenantName = tenant.Name;
            else if (!string.IsNullOrEmpty(item.TenantName))
                response.TenantName = item.TenantName;

            response.Images = item.Images ?? new List<string>();

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
                MaintenanceStatus.Processing => "Đang xử lý",
                MaintenanceStatus.Completed => "Hoàn thành",
                _ => item.Status.ToString()
            };

            return response;
        }
    }
}