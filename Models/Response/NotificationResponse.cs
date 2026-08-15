using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class NotificationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string? RefId { get; set; }
        public string? RefModel { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
        public bool IsRead { get; set; }
        public bool IsReadAdmin { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}