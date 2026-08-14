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
using static SmartBoardingHouse.Common.Enums;

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
            _notificationCollection =
                database.GetCollection<Notification>("notifications");

            _mapper = mapper;
            _validator = validator;
            _notificationService = notificationService;
        }

        // POST: api/Notification
        //[HttpPost]
        //public async Task<IActionResult> CreateNotification(
        //    [FromBody] NotificationRequest request)
        //{
        //    var validationResult =
        //        await _validator.ValidateAsync(request);

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
        //        return StatusCode(
        //            500,
        //            new
        //            {
        //                message =
        //                    "Không thể tạo thông báo, vui lòng thử lại"
        //            });

        //    return Ok(response);
        //}

        // GET: api/Notification
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            NotificationType? type = null)
        {
            var tenantId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter =
                Builders<Notification>.Filter.Eq(
                    n => n.TenantId,
                    tenantId);

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.IsRead,
                    isRead.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.Type,
                    type.Value);
            }

            var totalCount =
                await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response =
                _mapper.Map<List<NotificationResponse>>(notifications);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages =
                    (int)Math.Ceiling(
                        (double)totalCount / pageSize),
                data = response
            });
        }

        // GET: api/Notification/all
        // Admin: lấy tất cả thông báo toàn hệ thống
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications(
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            NotificationType? type = null)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter =
                Builders<Notification>.Filter.Empty;

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.IsRead,
                    isRead.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.Type,
                    type.Value);
            }

            var totalCount =
                await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response =
                _mapper.Map<List<NotificationResponse>>(notifications);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages =
                    (int)Math.Ceiling(
                        (double)totalCount / pageSize),
                data = response
            });
        }

        // GET: api/Notification/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetNotificationsByUserId(
            string userId,
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            NotificationType? type = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(
                    new
                    {
                        message = "UserId là bắt buộc"
                    });
            }

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter =
                Builders<Notification>.Filter.Eq(
                    n => n.TenantId,
                    userId);

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.IsRead,
                    isRead.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(
                    n => n.Type,
                    type.Value);
            }

            var totalCount =
                await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response =
                _mapper.Map<List<NotificationResponse>>(notifications);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages =
                    (int)Math.Ceiling(
                        (double)totalCount / pageSize),
                data = response
            });
        }

        // PUT: api/Notification/mark-read/{id}
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var update =
                Builders<Notification>.Update
                    .Set(n => n.IsRead, true)
                    .Set(n => n.ReadAt, DateTime.UtcNow)
                    .Set(n => n.UpdatedAt, DateTime.UtcNow);

            var result =
                await _notificationCollection.UpdateOneAsync(
                    n => n.Id == id,
                    update);

            if (result.MatchedCount == 0)
            {
                return NotFound(
                    new
                    {
                        message = "Không tìm thấy thông báo"
                    });
            }

            return Ok(
                new
                {
                    message = "Đã đánh dấu đã đọc"
                });
        }

        // PUT: api/Notification/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var tenantId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            var update =
                Builders<Notification>.Update
                    .Set(n => n.IsRead, true)
                    .Set(n => n.ReadAt, DateTime.UtcNow)
                    .Set(n => n.UpdatedAt, DateTime.UtcNow);

            await _notificationCollection.UpdateManyAsync(
                n =>
                    n.TenantId == tenantId &&
                    !n.IsRead,
                update);

            return Ok(
                new
                {
                    message =
                        "Đã đánh dấu tất cả thông báo là đã đọc"
                });
        }
    }
}