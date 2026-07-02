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

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IValidator<MaintenanceRequestRequest> _validator;
        private readonly IMapper _mapper;
        private readonly ActivityLogService _activityLogService;

        public MaintenanceRequestsController(
            MongoDbService mongoService,
            IValidator<MaintenanceRequestRequest> validator,
            IMapper mapper,
            ActivityLogService activityLogService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<MaintenanceRequest>("MaintenanceRequests");
            _roomCollection = db.GetCollection<Room>("Rooms");
            _validator = validator;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        // GET: api/MaintenanceRequests
        [HttpGet]
        public async Task<ActionResult<MaintenanceSummaryResponse>> GetAll()
        {
            var items = await _collection
                .Find(_ => true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();

            var mapped = items.Select(MapToResponse).ToList();

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
        //[HttpGet("{id}")]
        //public async Task<ActionResult<MaintenanceRequestResponse>> GetById(int id)
        //{
        //    var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (item is null)
        //        return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

        //    return Ok(MapToResponse(item));
        //}

        // POST: api/MaintenanceRequests
        //[HttpPost]
        //public async Task<ActionResult<MaintenanceRequestResponse>> Create(MaintenanceRequestRequest request)
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

        //    return CreatedAtAction(nameof(GetById), new { id = item.Id },
        //        MapToResponse(item));
        //}

        // PUT: api/MaintenanceRequests/{id}
        //[HttpPut("{id}")]
        //public async Task<ActionResult<MaintenanceRequestResponse>> Update(int id, MaintenanceRequestRequest request)
        //{
        //    var errors = await ValidateRequest(request);

        //    var existing = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (existing is null)
        //        return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

        //    var numberExists = await _collection
        //        .Find(x => x.RequestNumber == request.RequestNumber && x.Id != id)
        //        .AnyAsync();
        //    if (numberExists)
        //        errors.Add($"Mã yêu cầu '{request.RequestNumber}' đã tồn tại.");

        //    if (errors.Any())
        //        return BadRequest(errors);

        //    var updated = _mapper.Map<MaintenanceRequest>(request);
        //    updated.Id = id;
        //    updated.CreatedAt = existing.CreatedAt;
        //    updated.UpdatedAt = DateTime.UtcNow;

        //    await _collection.ReplaceOneAsync(x => x.Id == id, updated);
        //    return Ok(MapToResponse(updated));
        //}

        // PUT: api/MaintenanceRequests/{id}/start
        [HttpPut("{id}/start")]
        public async Task<ActionResult<MaintenanceRequestResponse>> Start(string id)
        {
            var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item is null)
                return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

            if (item.Status != MaintenanceStatus.Pending)
                return BadRequest(CommonMessage.JustStartThePendingRequest());

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<MaintenanceRequest>.Update
                    .Set(x => x.Status, MaintenanceStatus.InProgress)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            item.Status = MaintenanceStatus.InProgress;
            return Ok(MapToResponse(item));
        }

        // PUT: api/MaintenanceRequests/{id}/complete
        [HttpPut("{id}/complete")]
        public async Task<ActionResult<MaintenanceRequestResponse>> Complete(string id)
        {
            var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item is null)
                return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

            if (item.Status != MaintenanceStatus.InProgress)
                return BadRequest(CommonMessage.JustCompleteTheInProgressRequest());

            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<MaintenanceRequest>.Update
                    .Set(x => x.Status, MaintenanceStatus.Completed)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            await _activityLogService.LogAsync(
                type: ActivityType.Maintenance,
                userName: item.TenantName,
                roomNumber: item.RoomNumber,
                description: item.Title);

            item.Status = MaintenanceStatus.Completed;
            return Ok(MapToResponse(item));
        }

        // DELETE: api/MaintenanceRequests/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    if (item is null)
        //        return NotFound(CommonMessage.NotFound("MaintenanceRequest"));

        //    await _collection.DeleteOneAsync(x => x.Id == id);
        //    return Ok(CommonMessage.Deleted("MaintenanceRequest"));
        //}

        // ==================== HELPERS ====================

        //private async Task<List<string>> ValidateRequest(MaintenanceRequestRequest request)
        //{
        //    var result = await _validator.ValidateAsync(request);
        //    return result.Errors.Select(e => e.ErrorMessage).ToList();
        //}

        private MaintenanceRequestResponse MapToResponse(MaintenanceRequest item)
        {
            var response = _mapper.Map<MaintenanceRequestResponse>(item);

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