//using Microsoft.AspNetCore.SignalR;
//using SmartBoardingHouse.Common;
//using SmartBoardingHouse.Models.Response;
//using System.Net.Http.Json;

//namespace SmartBoardingHouse.Service
//{
//    public class ChatService
//    {
//        private readonly IHubContext<ChatHub> _hubContext;
//        private readonly HttpClient _httpClient;
//        private readonly string _internalKey;
//        private readonly ILogger<ChatService> _logger;

//        public ChatService(
//            IHubContext<ChatHub> hubContext,
//            HttpClient httpClient,
//            IConfiguration config,
//            ILogger<ChatService> logger)
//        {
//            _hubContext = hubContext;
//            _httpClient = httpClient;
//            _httpClient.BaseAddress = new Uri(config["NodeJS:NodeBaseUrl"]!);
//            _internalKey = config["NodeJS:InternalApiKey"]!;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Đẩy tin nhắn mới tới tenant liên quan + tới nhóm Admin (SignalR, giữ nguyên)
//        /// + forward sang Node để Socket.IO phát cho tenant app (mobile) đang nối bên đó
//        /// </summary>
//        public async Task PushNewMessageAsync(MessageResponse message, string tenantId)
//        {
//            await _hubContext.Clients.Group(tenantId).SendAsync("ReceiveMessage", message);
//            await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("ReceiveMessage", message);

//            await ForwardToNodeAsync("internal/messages/push", new { message, tenantId });
//        }

//        public async Task PushMessageReadAsync(string senderRole, string tenantId, string messageId)
//        {
//            if (senderRole == "Admin")
//                await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("MessageRead", messageId);
//            else
//                await _hubContext.Clients.Group(tenantId).SendAsync("MessageRead", messageId);

//            await ForwardToNodeAsync("internal/messages/push-read",
//                new { conversationId = tenantId, messageId, senderRole });
//        }

//        public async Task PushConversationReadAsync(string tenantId)
//        {
//            await _hubContext.Clients.Group(tenantId).SendAsync("ConversationRead", tenantId);
//            await _hubContext.Clients.Group(ChatHub.AdminGroup).SendAsync("ConversationRead", tenantId);

//            await ForwardToNodeAsync("internal/messages/push-conversation-read",
//                new { conversationId = tenantId, readBy = "Admin" });
//        }

//        public async Task PushNotificationAsync(NotificationResponse notification, string tenantId)
//        {
//            await _hubContext.Clients.Group(tenantId).SendAsync("ReceiveNotification", notification);
//            await ForwardToNodeAsync("internal/notifications/push", new { notification, tenantId });
//        }

//        private async Task ForwardToNodeAsync(string path, object payload)
//        {
//            try
//            {
//                var request = new HttpRequestMessage(HttpMethod.Post, path)
//                {
//                    Content = JsonContent.Create(payload)
//                };
//                request.Headers.Add("X-Internal-Key", _internalKey);

//                var res = await _httpClient.SendAsync(request);
//                if (!res.IsSuccessStatusCode)
//                    _logger.LogWarning("Forward sang Node thất bại: {Path} - {Status}", path, res.StatusCode);
//            }
//            catch (Exception ex)
//            {
          
//                _logger.LogError(ex, "Không thể forward sang Node: {Path}", path);
//            }
//        }
//    }
//}