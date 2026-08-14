using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Service;
using System.Security.Claims;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMongoCollection<Notification> _notificationCollection;
        private readonly IMapper _mapper;
        private readonly IValidator<NotificationRequest> _validator;
        private readonly INotificationService _notificationService;

        public NotificationController(
            IMongoDatabase database,
            IMapper mapper,
            IValidator<NotificationRequest> validator,
            INotificationService notificationService)
        {
            _notificationCollection = database.GetCollection<Notification>("notifications");
            _mapper = mapper;
            _validator = validator;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Tạo thông báo mới (thủ công, do Admin gửi)
        /// </summary>
        //[HttpPost]
        //public async Task<IActionResult> CreateNotification([FromBody] NotificationRequest request)
        //{
        //    var validationResult = await _validator.ValidateAsync(request);
        //    if (!validationResult.IsValid)
        //        return BadRequest(validationResult.Errors);

        //    var response = await _notificationService.CreateAsync(
        //        tenantId: request.TenantId,
        //        title: request.Title,
        //        body: request.Body,
        //        type: request.Type,
        //        refId: request.RefId,
        //        refModel: request.RefModel,
        //        meta: request.Meta);

        //    if (response is null)
        //        return StatusCode(500, new { message = "Không thể tạo thông báo, vui lòng thử lại" });

        //    return Ok(response);
        //}

        /// <summary>
        /// Lấy danh sách thông báo của Tenant hiện tại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 20, bool? isRead = null)
        {
            var tenantId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var filter = Builders<Notification>.Filter.Eq(n => n.TenantId, tenantId);

            if (isRead.HasValue)
                filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value);

            var notifications = await _notificationCollection.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response = _mapper.Map<List<NotificationResponse>>(notifications);
            return Ok(response);
        }

        /// <summary>
        /// Lấy tất cả thông báo do Admin đã tạo (toàn hệ thống, không lọc theo tenant)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications(int page = 1, int pageSize = 20, bool? isRead = null)
        {
            var filter = isRead.HasValue
                ? Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value)
                : Builders<Notification>.Filter.Empty;

            var totalCount = await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response = _mapper.Map<List<NotificationResponse>>(notifications);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                data = response
            });
        }

        /// <summary>
        /// Lấy danh sách thông báo theo userId cụ thể
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetNotificationsByUserId(
            string userId,
            int page = 1,
            int pageSize = 20,
            bool? isRead = null)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { message = "UserId là bắt buộc" });

            var filter = Builders<Notification>.Filter.Eq(n => n.TenantId, userId);

            if (isRead.HasValue)
                filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value);

            var totalCount = await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response = _mapper.Map<List<NotificationResponse>>(notifications);

            return Ok(response);
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            var result = await _notificationCollection.UpdateOneAsync(
                n => n.Id == id,
                update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "Không tìm thấy thông báo" });

            return Ok(new { message = "Đã đánh dấu đã đọc" });
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo là đã đọc
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var tenantId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            await _notificationCollection.UpdateManyAsync(
                n => n.TenantId == tenantId && !n.IsRead,
                update);

            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc" });
        }
    }
}