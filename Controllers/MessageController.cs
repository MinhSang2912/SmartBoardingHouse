using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using System.Security.Claims;
using Message = SmartBoardingHouse.Models.Entity.Message;

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

        public MessageController(IMongoDatabase database, IMapper mapper, IValidator<SendMessageRequest> validator)
        {
            _messageCollection = database.GetCollection<Message>("Messages");
            _mapper = mapper;
            _validator = validator;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            // Sử dụng AutoMapper để mapping
            var message = _mapper.Map<Message>(request);

            // Set thêm các giá trị mặc định không có trong Request
            message.IsRead = false;
            message.ReadAt = null;

            await _messageCollection.InsertOneAsync(message);

            var response = _mapper.Map<MessageResponse>(message);

            return Ok(response);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetMessages(string conversationId, int page = 1, int pageSize = 30)
        {
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
            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow);

            var result = await _messageCollection.UpdateOneAsync(
                m => m.Id == id,
                update);

            if (result.ModifiedCount == 0)
                return NotFound("Message not found");

            return Ok(new { message = "Đã đánh dấu đã đọc" });
        }

        [HttpPut("mark-all-read/{conversationId}")]
        public async Task<IActionResult> MarkAllAsRead(string conversationId)
        {
            var update = Builders<Message>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow);

            await _messageCollection.UpdateManyAsync(
                m => m.ConversationId == conversationId && !m.IsRead,
                update);

            return Ok(new { message = "Đã đánh dấu tất cả tin nhắn là đã đọc" });
        }
    }
}