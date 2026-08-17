using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Models.Settings;
using static SmartBoardingHouse.Common.Enums;
using Message = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<UserRequest> _validator;
        private readonly IOptions<AdminSettings> _adminSettings;
        private readonly IMapper _mapper;

        public UsersController(
            MongoDbService mongoService,
            IValidator<UserRequest> validator,
            IOptions<AdminSettings> adminSettings,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<User>("users");
            _roomCollection = db.GetCollection<Room>("rooms");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _validator = validator;
            _adminSettings = adminSettings;
            _mapper = mapper;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<List<UserResponse>>> GetAll()
        {
            var users = await _collection.Find(x => x.Role != "Admin").ToListAsync();
            var result = new List<UserResponse>();

            foreach (var user in users)
            {
                result.Add(await MapToResponseAsync(user));
            }

            return Ok(result);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetById(string id)
        {
            var user = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound(Message.NotFound("Người dùng"));

            return Ok(await MapToResponseAsync(user));
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create(UserRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var emailExists = await _collection.Find(x => x.Email == request.Email).AnyAsync();
            if (emailExists)
                errors.Add("Email đã tồn tại");

            if (errors.Any())
                return BadRequest(errors);

            var user = _mapper.Map<User>(request);
            user.Password = PasswordHelper.Hash(request.Password);
            user.Role = "Tenant";
            user.CreatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(user);

            return CreatedAtAction(nameof(GetById), new { id = user.Id },
                await MapToResponseAsync(user));
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> Update(string id, UserRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(errors);
            }

            var existingUser = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (existingUser is null)
                return NotFound(Message.NotFound("Người dùng"));

            _mapper.Map(request, existingUser);
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, existingUser);

            return Ok(await MapToResponseAsync(existingUser));
        }

        // DELETE: api/Users/{id} 
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (user is null)
                return NotFound(Message.NotFound("Người dùng"));

            // Kiểm tra còn hợp đồng đang hiệu lực không
            var hasActiveContract = await _contractCollection
                .Find(x => x.TenantId == id && x.Status == ContractStatus.Active)
                .AnyAsync();

            if (hasActiveContract)
                return BadRequest(Message.UserHasActiveContract());

            // Soft delete
            await _collection.UpdateOneAsync(
                x => x.Id == id,
                Builders<User>.Update
                    .Set(x => x.IsActive, false)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            return Ok(Message.Deleted("Người dùng"));
        }

        // ==================== HELPERS ====================
        private async Task<UserResponse> MapToResponseAsync(User user)
        {
            var response = _mapper.Map<UserResponse>(user);

            // Lấy tất cả hợp đồng đang Active của người này
            var activeContracts = await _contractCollection
                .Find(c => c.TenantId == user.Id && c.Status == ContractStatus.Active)
                .ToListAsync();

            if (activeContracts.Any())
            {
                var roomIds = activeContracts
                    .Select(c => c.RoomId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                // Lấy thông tin phòng
                var rooms = await _roomCollection
                    .Find(r => roomIds.Contains(r.Id))
                    .ToListAsync();

                response.ActiveRoomIds = rooms.Select(r => r.Id).ToList();
                response.ActiveRoomNumbers = rooms
                    .Select(r => r.RoomNumber)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                response.ActiveRoomCount = response.ActiveRoomNumbers.Count;

                // Giữ tương thích với field cũ (nếu frontend vẫn dùng)
                response.RoomNumber = response.ActiveRoomCount > 0
                    ? string.Join(", ", response.ActiveRoomNumbers)
                    : "Chưa có phòng";
            }
            else
            {
                response.ActiveRoomCount = 0;
                response.ActiveRoomNumbers = new List<string>();
                response.ActiveRoomIds = new List<string>();
                response.RoomNumber = "Chưa có phòng";
            }

            return response;
        }
    }
}