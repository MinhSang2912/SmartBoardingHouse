using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Response;
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

        public NotificationController(
            IMongoDatabase database,
            IMapper mapper)
        {
            _notificationCollection = database.GetCollection<Notification>("notifications");
            _mapper = mapper;
        }

        // GET: api/Notification
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            bool? isReadAdmin = null,
            NotificationType? type = null)
        {
            var tenantId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = Builders<Notification>.Filter.Eq(n => n.TenantId, tenantId);

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value);
            }

            if (isReadAdmin.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsReadAdmin, isReadAdmin.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.Type, type.Value);
            }

            var totalCount = await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
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

        // GET: api/Notification/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications(
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            bool? isReadAdmin = null,
            NotificationType? type = null)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = Builders<Notification>.Filter.Empty;

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value);
            }

            if (isReadAdmin.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsReadAdmin, isReadAdmin.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.Type, type.Value);
            }

            var totalCount = await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
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

        // GET: api/Notification/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetNotificationsByUserId(
            string userId,
            int page = 1,
            int pageSize = 20,
            bool? isRead = null,
            bool? isReadAdmin = null,
            NotificationType? type = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "UserId là bắt buộc" });
            }

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = Builders<Notification>.Filter.Eq(n => n.TenantId, userId);

            if (isRead.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value);
            }

            if (isReadAdmin.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.IsReadAdmin, isReadAdmin.Value);
            }

            if (type.HasValue)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.Type, type.Value);
            }

            var totalCount = await _notificationCollection.CountDocumentsAsync(filter);

            var notifications = await _notificationCollection
                .Find(filter)
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

        //PUT: api/Notification/mark-read/{id}
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var update = Builders<Notification>.Update
                .Set(n => n.IsReadAdmin, true) 
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            var result = await _notificationCollection.UpdateOneAsync(n => n.Id == id && !n.IsReadAdmin, update);

            if (result.MatchedCount == 0)
            {
                return NotFound(new { message = "Không tìm thấy thông báo" });
            }

            return Ok(new { message = "Đã đánh dấu đã đọc bởi admin" });
        }

        //PUT: api/Notification/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {

            var update = Builders<Notification>.Update
                .Set(n => n.IsReadAdmin, true)
                .Set(n => n.ReadAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            await _notificationCollection.UpdateManyAsync(
                n => !n.IsReadAdmin, update);

            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc" });
        }
    }
}