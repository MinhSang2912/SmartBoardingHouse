using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using MessageEntity = SmartBoardingHouse.Models.Entity.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : AuthorizedControllerBase
    {
        private readonly IMongoCollection<MessageEntity> _collection;

        public MessagesController(MongoDbService mongoService)
            : base(mongoService)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<MessageEntity>("Messages");
        }

        [HttpGet]
        public async Task<ActionResult<List<MessageResponse>>> GetConversations()
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var messages = await _collection.Find(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .SortByDescending(m => m.CreatedAt)
                .Limit(50)
                .ToListAsync();

            return Ok(messages.Select(MapToResponse).ToList());
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessagesWith(int userId, int page = 1, int limit = 30)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null)
                return Unauthorized();

            var filter = Builders<MessageEntity>.Filter.Or(
                Builders<MessageEntity>.Filter.And(
                    Builders<MessageEntity>.Filter.Eq(m => m.SenderId, currentUser.Id),
                    Builders<MessageEntity>.Filter.Eq(m => m.ReceiverId, userId)),
                Builders<MessageEntity>.Filter.And(
                    Builders<MessageEntity>.Filter.Eq(m => m.SenderId, userId),
                    Builders<MessageEntity>.Filter.Eq(m => m.ReceiverId, currentUser.Id)));

            var messages = await _collection.Find(filter)
                .SortByDescending(m => m.CreatedAt)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            await _collection.UpdateManyAsync(
                Builders<MessageEntity>.Filter.And(
                    Builders<MessageEntity>.Filter.Eq(m => m.SenderId, userId),
                    Builders<MessageEntity>.Filter.Eq(m => m.ReceiverId, currentUser.Id),
                    Builders<MessageEntity>.Filter.Eq(m => m.IsRead, false)),
                Builders<MessageEntity>.Update.Set(m => m.IsRead, true).Set(m => m.ReadAt, DateTime.UtcNow));

            return Ok(messages.Select(MapToResponse).Reverse().ToList());
        }

        [HttpPost]
        public async Task<ActionResult<MessageResponse>> Create(MessageRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Thông tin tin nhắn không hợp lệ.");

            var receiverExists = await _userCollection.Find(x => x.Id == request.ReceiverId).AnyAsync();
            if (!receiverExists)
                return NotFound("Người nhận không tồn tại.");

            var message = new MessageEntity
            {
                Id = await MongoIdHelper.GetNextIdAsync(_collection),
                SenderId = user.Id,
                ReceiverId = request.ReceiverId,
                SenderModel = "Tenant",
                Content = request.Content,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
            };

            await _collection.InsertOneAsync(message);
            return CreatedAtAction(nameof(GetById), new { id = message.Id }, MapToResponse(message));
        }

        [HttpGet("detail/{id}")]
        public async Task<ActionResult<MessageResponse>> GetById(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var message = await _collection.Find(m => m.Id == id && (m.SenderId == user.Id || m.ReceiverId == user.Id)).FirstOrDefaultAsync();
            if (message is null)
                return NotFound("Tin nhắn không tìm thấy.");

            return Ok(MapToResponse(message));
        }

        private MessageResponse MapToResponse(MessageEntity message)
        {
            return new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                SenderModel = message.SenderModel,
                Content = message.Content,
                Type = message.Type,
                ImageUrl = message.ImageUrl,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt,
                CreatedAt = message.CreatedAt,
            };
        }

    }
}
