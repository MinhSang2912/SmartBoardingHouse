using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : AuthorizedControllerBase
    {
        private readonly IMongoCollection<Notification> _collection;

        public NotificationsController(MongoDbService mongoService)
            : base(mongoService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Notification>("Notifications");
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(bool? isRead = null, int page = 1, int limit = 20)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var filter = Builders<Notification>.Filter.Eq(n => n.UserId, user.Id);
            if (isRead != null)
                filter = Builders<Notification>.Filter.And(filter, Builders<Notification>.Filter.Eq(n => n.IsRead, isRead.Value));

            var total = await _collection.CountDocumentsAsync(filter);
            var notifications = await _collection.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            return Ok(new
            {
                notifications = notifications.Select(MapToResponse),
                unreadCount = await _collection.CountDocumentsAsync(Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.Eq(n => n.UserId, user.Id),
                    Builders<Notification>.Filter.Eq(n => n.IsRead, false))),
                pagination = new { page, limit, total }
            });
        }

        [HttpPut("read")]
        public async Task<IActionResult> MarkAsRead(NotificationReadRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            if (request.All)
            {
                await _collection.UpdateManyAsync(
                    Builders<Notification>.Filter.And(
                        Builders<Notification>.Filter.Eq(n => n.UserId, user.Id),
                        Builders<Notification>.Filter.Eq(n => n.IsRead, false)),
                    Builders<Notification>.Update.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow));
                return Ok(new { message = "Đã đọc tất cả thông báo." });
            }

            if (request.NotificationIds == null || !request.NotificationIds.Any())
                return BadRequest("Vui lòng chọn thông báo.");

            await _collection.UpdateManyAsync(
                Builders<Notification>.Filter.And(
                    Builders<Notification>.Filter.In(n => n.Id, request.NotificationIds),
                    Builders<Notification>.Filter.Eq(n => n.UserId, user.Id)),
                Builders<Notification>.Update.Set(n => n.IsRead, true).Set(n => n.ReadAt, DateTime.UtcNow));

            return Ok(new { message = "Đã đọc thông báo." });
        }

        [HttpPut("fcm")]
        public async Task<IActionResult> UpdateFcmToken(UpdateFcmTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.FcmToken))
                return BadRequest("FCM token is required.");

            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            user.FcmToken = request.FcmToken;
            user.UpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            return Ok(new { message = "Cập nhật FCM token thành công." });
        }

        private NotificationResponse MapToResponse(Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                RefId = notification.RefId,
                RefModel = notification.RefModel,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt,
            };
        }

    }
}
