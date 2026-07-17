using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Service;
using SmartBoardingHouse.Services;
using System.Security.Claims;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMongoCollection<Message> _messageCollection;
        private readonly IMapper _mapper;
        private readonly IValidator<SendMessageRequest> _validator;
        private readonly PhotoService _photoService;
        private readonly ChatService _chatService;

        public MessageController(
            IMongoDatabase database,
            IMapper mapper,
            IValidator<SendMessageRequest> validator,
            PhotoService photoService,
            ChatService chatService)
        {
            _messageCollection = database.GetCollection<Message>("messages");
            _mapper = mapper;
            _validator = validator;
            _photoService = photoService;
            _chatService = chatService;
        }

        /// <summary>
        /// Gửi tin nhắn mới (hỗ trợ kèm ảnh)
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentRole = User.FindFirst(ClaimTypes.Role)?.Value; 

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(currentRole))
                return Unauthorized();

            // Xác định TenantId của cuộc hội thoại:
            // - Tenant gửi: luôn là chính họ (không tin request từ client)
            // - Admin gửi: phải chỉ định đang nói chuyện với tenant nào qua request.TenantId
            string tenantId = currentRole == "Admin" ? request.TenantId : currentUserId;

            if (string.IsNullOrEmpty(tenantId))
                return BadRequest(new { message = "TenantId là bắt buộc" });

            string? imageUrl = null;

            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    imageUrl = await _photoService.SaveMaintenancePhotoAsync(request.Image, currentUserId, "Messages");
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = $"Upload ảnh thất bại: {ex.Message}" });
                }
            }

            var message = _mapper.Map<Message>(request);

            message.ConversationId = tenantId;
            message.SenderRole = currentRole;
            message.ImageUrl = imageUrl;
            message.IsRead = false;
            message.ReadAt = null;
            message.CreatedAt = DateTime.UtcNow;

            await _messageCollection.InsertOneAsync(message);

            var response = _mapper.Map<MessageResponse>(message);

            await _chatService.PushNewMessageAsync(response, tenantId);

            return Ok(response);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetMessages(string conversationId, int page = 1, int pageSize = 30)
        {
            if (string.IsNullOrEmpty(conversationId))
                return BadRequest(new { message = "ConversationId là bắt buộc" });

            var messages = await _messageCollection.Find(m => m.ConversationId == conversationId)
                .SortByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var response = _mapper.Map<List<MessageResponse>>(messages);
            return Ok(response);
        }

        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Message Id là bắt buộc" });

            var existing = await _messageCollection.Find(m => m.Id == id).FirstOrDefaultAsync();
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy tin nhắn" });

            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            await _messageCollection.UpdateOneAsync(m => m.Id == id, update);

            await _chatService.PushMessageReadAsync(existing.SenderRole, existing.ConversationId, id);

            return Ok(new { message = "Đã đánh dấu tin nhắn là đã đọc" });
        }

        [HttpPut("mark-all-read/{conversationId}")]
        public async Task<IActionResult> MarkAllAsRead(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
                return BadRequest(new { message = "ConversationId là bắt buộc" });

            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            await _messageCollection.UpdateManyAsync(
                m => m.ConversationId == conversationId && !m.IsRead,
                update);

            await _chatService.PushConversationReadAsync(conversationId);

            return Ok(new { message = "Đã đánh dấu tất cả tin nhắn là đã đọc" });
        }
    }
}