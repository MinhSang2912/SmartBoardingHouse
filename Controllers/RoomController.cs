using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;
using CommonMessage = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IMongoCollection<Room> _collection;
        private readonly IMongoCollection<Floor> _floorCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IValidator<RoomRequest> _validator;
        private readonly IMapper _mapper;

        public RoomsController(
            MongoDbService mongoService,
            IValidator<RoomRequest> validator,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Room>("rooms");
            _floorCollection = db.GetCollection<Floor>("floors");
            _contractCollection = db.GetCollection<Contract>("contracts");
            _userCollection = db.GetCollection<User>("users");
            _validator = validator;
            _mapper = mapper;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<List<RoomResponse>>> GetAll()
        {
            var rooms = await _collection.Find(_ => true).ToListAsync();
            var result = new List<RoomResponse>();

            foreach (var room in rooms)
            {
                result.Add(await MapToResponseAsync(room));
            }

            return Ok(result);
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomResponse>> GetById(string id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            return Ok(await MapToResponseAsync(room));
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<RoomResponse>> Create(RoomRequest request)
        {
            var errors = await ValidateRequest(request);

            var roomNumberExists = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber)
                .AnyAsync();
            if (roomNumberExists)
                errors.Add(CommonMessage.RoomNumberExists());

            var floor = await _floorCollection
                .Find(x => x.Id == request.FloorId)
                .FirstOrDefaultAsync();
            if (floor is null)
                errors.Add(CommonMessage.NotFound("Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            var room = _mapper.Map<Room>(request);
            room.CreatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(room);

            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                await MapToResponseAsync(room));
        }

        // PUT: api/Rooms/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<RoomResponse>> Update(string id, RoomRequest request)
        {
            var errors = await ValidateRequest(request);

            var existingRoom = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingRoom is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            var roomNumberExists = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber && x.Id != id)
                .AnyAsync();
            if (roomNumberExists)
                errors.Add(CommonMessage.RoomNumberExists());

            var floor = await _floorCollection
                .Find(x => x.Id == request.FloorId)
                .FirstOrDefaultAsync();
            if (floor is null)
                errors.Add(CommonMessage.NotFound("Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            _mapper.Map(request, existingRoom);
            existingRoom.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, existingRoom);

            return Ok(await MapToResponseAsync(existingRoom));
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            // Kiểm tra hợp đồng đang hiệu lực theo RoomId (chính xác hơn RoomNumber)
            var activeContractExists = await _contractCollection
                .Find(c => c.RoomId == id && c.Status == ContractStatus.Active)
                .AnyAsync();

            if (activeContractExists)
                return BadRequest(CommonMessage.RoomHasActiveContract());

            await _collection.DeleteOneAsync(x => x.Id == id);
            return Ok(CommonMessage.Deleted("Phòng"));
        }

        // ==================== HELPERS ====================

        private async Task<List<string>> ValidateRequest(RoomRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            return validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        }

        private async Task<RoomResponse> MapToResponseAsync(Room room)
        {
            var response = _mapper.Map<RoomResponse>(room);

            // Lấy Floor theo Id
            if (!string.IsNullOrEmpty(room.FloorId))
            {
                var floor = await _floorCollection
                    .Find(f => f.Id == room.FloorId)
                    .FirstOrDefaultAsync();
                response.FloorNumber = floor?.FloorNumber ?? 0;
            }

            response.StatusLabel = room.Status switch
            {
                RoomStatus.Available => "Trống",
                RoomStatus.Occupied => "Đã thuê",
                RoomStatus.Maintenance => "Bảo trì",
                _ => room.Status.ToString()
            };

            // Lấy Tenant theo TenantId (ưu tiên) hoặc qua hợp đồng active
            if (!string.IsNullOrEmpty(room.TenantId))
            {
                var tenant = await _userCollection
                    .Find(u => u.Id == room.TenantId)
                    .FirstOrDefaultAsync();
                response.TenantName = tenant?.Name;
            }
            else
            {
                var activeContract = await _contractCollection
                    .Find(c => c.RoomId == room.Id && c.Status == ContractStatus.Active)
                    .FirstOrDefaultAsync();

                if (activeContract != null && !string.IsNullOrEmpty(activeContract.TenantId))
                {
                    var tenant = await _userCollection
                        .Find(u => u.Id == activeContract.TenantId)
                        .FirstOrDefaultAsync();
                    response.TenantName = tenant?.Name ?? activeContract.TenantName;
                }
            }

            return response;
        }
    }
}