using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
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
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMapper _mapper;
        private readonly IValidator<NotificationRequest> _validator;

        public NotificationController(
            IMongoDatabase database,
            IMapper mapper,
            IValidator<NotificationRequest> validator)
        {
            _notificationCollection = database.GetCollection<Notification>("notifications");
            _userCollection = database.GetCollection<User>("users");
            _contractCollection = database.GetCollection<Contract>("contracts");
            _roomCollection = database.GetCollection<Room>("rooms");
            _mapper = mapper;
            _validator = validator;
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
        public async Task<IActionResult> GetAllNotifications()
        {
            var notifications = await _notificationCollection
                .Find(Builders<Notification>.Filter.Empty)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Group notifications in memory to merge duplicates created at the same time
            var groupedList = new List<Notification>();
            foreach (var notif in notifications)
            {
                var match = groupedList.FirstOrDefault(g =>
                    g.Title == notif.Title &&
                    g.Body == notif.Body &&
                    g.Type == notif.Type &&
                    Math.Abs((g.CreatedAt - notif.CreatedAt).TotalSeconds) < 15);

                if (match == null)
                {
                    groupedList.Add(notif);
                }
                else
                {
                    // If at least one in the group is unread, mark the representative as unread
                    if (!notif.IsReadAdmin)
                    {
                        match.IsReadAdmin = false;
                    }
                }
            }

            var mappedList = _mapper.Map<List<NotificationResponse>>(groupedList);

            var response = new NotificationListResponse
            {
                TotalCount = mappedList.Count,
                UnreadCount = mappedList.Count(n => !n.IsReadAdmin),
                ReadCount = mappedList.Count(n => n.IsReadAdmin),
                Page = 1,
                PageSize = mappedList.Count,
                Data = mappedList
            };

            return Ok(response);
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
            var target = await _notificationCollection.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (target == null)
            {
                return NotFound(new { message = "Không tìm thấy thông báo" });
            }

            var update = Builders<Notification>.Update
                .Set(n => n.IsReadAdmin, true) 
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            // Mark all notifications in the same broadcast group as read
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.Title, target.Title),
                Builders<Notification>.Filter.Eq(n => n.Body, target.Body),
                Builders<Notification>.Filter.Eq(n => n.Type, target.Type),
                Builders<Notification>.Filter.Gte(n => n.CreatedAt, target.CreatedAt.AddSeconds(-15)),
                Builders<Notification>.Filter.Lte(n => n.CreatedAt, target.CreatedAt.AddSeconds(15))
            );

            var result = await _notificationCollection.UpdateManyAsync(filter, update);

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
                Builders<Notification>.Filter.Ne(n => n.IsReadAdmin, true), update);

            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc" });
        }

        // GET: api/Notification/group-detail/{id}
        [HttpGet("group-detail/{id}")]
        public async Task<IActionResult> GetGroupDetail(string id)
        {
            var target = await _notificationCollection.Find(n => n.Id == id).FirstOrDefaultAsync();
            if (target == null)
            {
                return NotFound(new { message = "Không tìm thấy thông báo" });
            }

            // Find all notifications in the same group
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.Title, target.Title),
                Builders<Notification>.Filter.Eq(n => n.Body, target.Body),
                Builders<Notification>.Filter.Eq(n => n.Type, target.Type),
                Builders<Notification>.Filter.Gte(n => n.CreatedAt, target.CreatedAt.AddSeconds(-15)),
                Builders<Notification>.Filter.Lte(n => n.CreatedAt, target.CreatedAt.AddSeconds(15))
            );

            var notifications = await _notificationCollection.Find(filter).ToListAsync();

            // Retrieve all matching users
            var tenantIds = notifications.Select(n => n.TenantId).Distinct().ToList();
            var users = await _userCollection.Find(u => tenantIds.Contains(u.Id)).ToListAsync();
            var userMap = users.ToDictionary(u => u.Id);

            // Fetch active contracts for these users to get ALL rooms
            var activeContracts = await _contractCollection
                .Find(c => tenantIds.Contains(c.TenantId) && c.Status == ContractStatus.Active)
                .ToListAsync();

            var roomIds = activeContracts.Select(c => c.RoomId).Where(rid => !string.IsNullOrEmpty(rid)).Distinct().ToList();
            var rooms = await _roomCollection.Find(r => roomIds.Contains(r.Id)).ToListAsync();
            var roomMap = rooms.ToDictionary(r => r.Id);

            // Group active rooms by TenantId
            var tenantRoomsMap = activeContracts
                .GroupBy(c => c.TenantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => roomMap.TryGetValue(c.RoomId ?? "", out var r) ? r.RoomNumber : null)
                          .Where(num => !string.IsNullOrEmpty(num))
                          .Distinct()
                          .ToList()
                );

            var details = notifications.Select(n => {
                userMap.TryGetValue(n.TenantId, out var user);
                tenantRoomsMap.TryGetValue(n.TenantId, out var roomList);

                string roomsString = roomList != null && roomList.Any()
                    ? string.Join(", ", roomList)
                    : "Chưa có phòng";

                return new {
                    TenantId = n.TenantId,
                    TenantName = user?.Name ?? "N/A",
                    RoomNumber = roomsString,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt
                };
            }).OrderBy(d => d.RoomNumber).ToList();

            return Ok(details);
        }

        // POST: api/Notification
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] NotificationRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
            }

            // Check if tenantId is specified (and not empty or "all")
            if (!string.IsNullOrWhiteSpace(request.TenantId) && request.TenantId.ToLower() != "all")
            {
                // Verify the tenant/user exists
                var userExists = await _userCollection.Find(u => u.Id == request.TenantId).AnyAsync();
                if (!userExists)
                {
                    return NotFound(new { message = "Không tìm thấy người thuê nhận thông báo" });
                }

                var notification = _mapper.Map<Notification>(request);
                notification.CreatedAt = DateTime.UtcNow;
                notification.IsRead = false;
                notification.IsReadAdmin = false;
                notification.Meta = request.Meta;

                await _notificationCollection.InsertOneAsync(notification);
                var response = _mapper.Map<NotificationResponse>(notification);
                return CreatedAtAction(nameof(GetNotifications), new { id = response.Id }, response);
            }
            else
            {
                // Send to all tenants (role != "Admin")
                var tenants = await _userCollection.Find(u => u.Role != "Admin").ToListAsync();
                if (tenants.Count == 0)
                {
                    return BadRequest(new { message = "Không tìm thấy người thuê nào để gửi thông báo" });
                }

                var notifications = tenants.Select(tenant =>
                {
                    var notification = _mapper.Map<Notification>(request);
                    notification.TenantId = tenant.Id;
                    notification.CreatedAt = DateTime.UtcNow;
                    notification.IsRead = false;
                    notification.IsReadAdmin = false;
                    notification.Meta = request.Meta;
                    return notification;
                }).ToList();

                await _notificationCollection.InsertManyAsync(notifications);
                return Ok(new { message = $"Đã gửi thông báo thành công tới {notifications.Count} người thuê" });
            }
        }
    }
}