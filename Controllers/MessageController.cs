using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using System.Security.Claims;
using static SmartBoardingHouse.Common.Enums;

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

        public MessageController(
            IMongoDatabase database,
            IMapper mapper,
            IValidator<SendMessageRequest> validator,
            PhotoService photoService)
        {
            _messageCollection = database.GetCollection<Message>("messages");
            _mapper = mapper;
            _validator = validator;
            _photoService = photoService;
        }

        /// <summary>
        /// Gửi tin nhắn mới (hỗ trợ kèm ảnh) - Tự động tạo ConversationId
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            // Tự động tạo ConversationId nếu chưa có
            string conversationId = GenerateConversationId(request.SenderId, request.ReceiverId);

            string? imageUrl = null;

            // Upload ảnh nếu có
            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    var uploaderId = request.SenderId; // Hoặc lấy từ JWT
                    imageUrl = await _photoService.SaveMaintenancePhotoAsync(request.Image, uploaderId, "Messages");
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = $"Upload ảnh thất bại: {ex.Message}" });
                }
            }

            var message = _mapper.Map<Message>(request);

            message.ConversationId = conversationId;
            message.ImageUrl = imageUrl;
            message.IsRead = false;
            message.ReadAt = null;
            message.CreatedAt = DateTime.UtcNow;

            await _messageCollection.InsertOneAsync(message);

            var response = _mapper.Map<MessageResponse>(message);
            return Ok(response);
        }

        /// <summary>
        /// Tạo ConversationId từ 2 userId (luôn giống nhau dù ai gửi trước)
        /// </summary>
        private string GenerateConversationId(string user1, string user2)
        {
            var ids = new[] { user1, user2 }.OrderBy(id => id).ToArray();
            return string.Join("_", ids);
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

            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);

            var result = await _messageCollection.UpdateOneAsync(m => m.Id == id, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = "Không tìm thấy tin nhắn" });

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

            return Ok(new { message = "Đã đánh dấu tất cả tin nhắn là đã đọc" });
        }
    }
}