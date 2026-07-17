using Microsoft.AspNetCore.SignalR;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Service
{
    public class ChatService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Đẩy tin nhắn mới tới tenant liên quan + tới nhóm Admin
        /// </summary>
        public async Task PushNewMessageAsync(MessageResponse message, string tenantId)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("ReceiveMessage", message);
            await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("ReceiveMessage", message);
        }

        /// <summary>
        /// Báo tin nhắn đã đọc cho đúng phía còn lại của cuộc hội thoại
        /// </summary>
        public async Task PushMessageReadAsync(string senderRole, string tenantId, string messageId)
        {
            // Nếu người gửi ban đầu là Admin -> báo cho nhóm Admin biết tenant đã đọc
            // Nếu người gửi ban đầu là Tenant -> báo cho đúng tenant đó biết Admin đã đọc
            if (senderRole == "Admin")
            {
                await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("MessageRead", messageId);
            }
            else
            {
                await _hubContext.Clients.Group(tenantId).SendAsync("MessageRead", messageId);
            }
        }

        public async Task PushConversationReadAsync(string tenantId)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("ConversationRead", tenantId);
            await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("ConversationRead", tenantId);
        }

        public async Task PushNotificationAsync(NotificationResponse notification, string tenantId)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("ReceiveNotification", notification);
        }
    }
}