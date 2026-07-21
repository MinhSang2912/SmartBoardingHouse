using AutoMapper;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Service
{
    /// <summary>
    /// Dịch vụ tạo thông báo dùng chung: lưu vào DB + đẩy realtime (SignalR/Node).
    /// Dùng cho cả thông báo tạo tay (NotificationController) lẫn thông báo tự động
    /// khi tương tác với Hợp đồng, Hóa đơn, Yêu cầu sửa chữa,...
    /// </summary>
    public interface INotificationService
    {
        Task<NotificationResponse?> CreateAsync(
            string tenantId,
            string title,
            string body,
            NotificationType type,
            string? refId = null,
            string? refModel = null,
            Dictionary<string, object>? meta = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly IMongoCollection<Notification> _notificationCollection;
        private readonly IMapper _mapper;
        //private readonly ChatService _chatService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IMongoDatabase database,
            IMapper mapper,
            //ChatService chatService,
            ILogger<NotificationService> logger)
        {
            _notificationCollection = database.GetCollection<Notification>("notifications");
            _mapper = mapper;
            //_chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo và lưu 1 thông báo, đồng thời đẩy realtime cho tenant liên quan.
        /// Không throw exception ra ngoài để không làm hỏng luồng nghiệp vụ chính
        /// (ví dụ: tạo hóa đơn vẫn phải thành công dù việc bắn thông báo bị lỗi).
        /// </summary>
        public async Task<NotificationResponse?> CreateAsync(
            string tenantId,
            string title,
            string body,
            NotificationType type,
            string? refId = null,
            string? refModel = null,
            Dictionary<string, object>? meta = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogWarning("Bỏ qua tạo thông báo tự động vì thiếu tenantId. Title={Title}", title);
                return null;
            }

            var notification = new Notification
            {
                TenantId = tenantId,
                Title = title,
                Body = body,
                Type = type,
                RefId = refId,
                RefModel = refModel,
                Meta = meta,
                IsRead = false,
                ReadAt = null,
                CreatedAt = DateTime.UtcNow
            };

            NotificationResponse response;
            try
            {
                await _notificationCollection.InsertOneAsync(notification);
                response = _mapper.Map<NotificationResponse>(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Không thể lưu thông báo tự động. TenantId={TenantId}, RefModel={RefModel}, RefId={RefId}",
                    tenantId, refModel, refId);
                return null;
            }

            //try
            //{
            //    await _chatService.PushNotificationAsync(response, tenantId);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning(ex,
            //        "Đã lưu thông báo nhưng không đẩy realtime được. TenantId={TenantId}, NotificationId={Id}",
            //        tenantId, notification.Id);
            //}

            return response;
        }
    }
}
