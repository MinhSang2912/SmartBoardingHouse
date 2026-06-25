using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common.Message;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<UserRequest> _validator;
        private readonly IMapper _mapper;

        public UsersController(
            MongoDbService mongoService,
            IValidator<UserRequest> validator,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<User>("Users");
            _roomCollection = db.GetCollection<Room>("Rooms");
            _contractCollection = db.GetCollection<Contract>("Contracts");
            _validator = validator;
            _mapper = mapper;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<List<UserResponse>>> GetAll()
        {
            var users = await _collection.Find(_ => true).ToListAsync();
            var rooms = await _roomCollection.Find(_ => true).ToListAsync();
            var contracts = await _contractCollection.Find(_ => true).ToListAsync();

            var result = users.Select(u => MapToResponse(u, rooms, contracts)).ToList();
            return Ok(result);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetById(int id)
        {
            var user = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            var rooms = await _roomCollection.Find(_ => true).ToListAsync();
            var contracts = await _contractCollection.Find(_ => true).ToListAsync();

            return Ok(MapToResponse(user, rooms, contracts));
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create(UserRequest request)
        {
            var errors = await ValidateRequest(request);

            var idCardExists = await _collection
                .Find(x => x.IDCardNumber == request.IDCardNumber)
                .AnyAsync();
            if (idCardExists)
                errors.Add(CommonMessage.UserIDCardNumberExists(request.IDCardNumber));

            if (errors.Any())
                return BadRequest(errors);

            var user = _mapper.Map<User>(request);
            user.Id = await MongoIdHelper.GetNextIdAsync(_collection);
            user.CreatedAt = DateTime.UtcNow;

            // Password mặc định = IDCardNumber, được hash trước khi lưu
            user.Password = PasswordHelper.Hash(request.IDCardNumber);

            await _collection.InsertOneAsync(user);

            var rooms = await _roomCollection.Find(_ => true).ToListAsync();
            var contracts = await _contractCollection.Find(_ => true).ToListAsync();

            return CreatedAtAction(nameof(GetById), new { id = user.Id },
                MapToResponse(user, rooms, contracts));
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponse>> Update(int id, UserRequest request)
        {
            var errors = await ValidateRequest(request);

            var existingUser = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingUser is null)
                return NotFound(CommonMessage.NotFound("Người thuê"));

            var idCardExists = await _collection
                .Find(x => x.IDCardNumber == request.IDCardNumber && x.Id != id)
                .AnyAsync();
            if (idCardExists)
                errors.Add(CommonMessage.UserIDCardNumberExists(request.IDCardNumber));


            if (errors.Any())
                return BadRequest(errors);

            var updatedUser = _mapper.Map<User>(request);
            updatedUser.Id = id;
            updatedUser.CreatedAt = existingUser.CreatedAt;
            updatedUser.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, updatedUser);

            var rooms = await _roomCollection.Find(_ => true).ToListAsync();
            var contracts = await _contractCollection.Find(_ => true).ToListAsync();

            return Ok(MapToResponse(updatedUser, rooms, contracts));
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người thuê"));
            if (user.RoomNumber != "Chưa có phòng")
                return BadRequest(CommonMessage.UserHasActiveContract());

            await _collection.DeleteOneAsync(x => x.Id == id);
            return Ok(CommonMessage.Deleted("Người thuê"));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(UserRequest request)
        {
            var result = await _validator.ValidateAsync(request);
            return result.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private UserResponse MapToResponse(User user, List<Room> rooms, List<Contract> contracts)
        {
            var response = _mapper.Map<UserResponse>(user);

            // Lấy thông tin phòng
            var room = rooms.FirstOrDefault(r => r.RoomNumber == user.RoomNumber);
            response.RoomDeposit = room?.RoomDeposit ?? 0;
            response.Price = room?.Price ?? 0;

            // Lấy hợp đồng active của user (theo RoomNumber)
            var contract = contracts.FirstOrDefault(c =>
                c.RoomNumber == user.RoomNumber &&
                c.TenantName == user.Name &&
                c.Status == ContractStatus.Active);

            response.StartDate = contract?.StartDate;

            return response;
        }
    }
}