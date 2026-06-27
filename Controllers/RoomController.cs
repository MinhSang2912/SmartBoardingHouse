using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using AutoMapper;
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
    public class RoomsController : ControllerBase
    {
        private readonly IMongoCollection<Room> _collection;
        private readonly IMongoCollection<Floor> _floorCollection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<RoomRequest> _validator;
        private readonly IMapper _mapper;

        public RoomsController(
            MongoDbService mongoService,
            IValidator<RoomRequest> validator,
            IMapper mapper)
        {
            var db = mongoService.GetDatabase();
            _collection = db.GetCollection<Room>("Rooms");
            _floorCollection = db.GetCollection<Floor>("Floors");
            _contractCollection = db.GetCollection<Contract>("Contracts");
            _validator = validator;
            _mapper = mapper;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<List<RoomResponse>>> GetAll()
        {
            var rooms = await _collection.Find(_ => true).ToListAsync();
            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var activeContracts = await _contractCollection
                .Find(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            var result = rooms.Select(r => MapToResponse(r, floors, activeContracts)).ToList();
            return Ok(result);
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomResponse>> GetById(string id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));

            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var activeContracts = await _contractCollection
                .Find(c => c.Status == ContractStatus.Active)
                .ToListAsync();
            try
            {
                var response = MapToResponse(room, floors, activeContracts);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xử lý dữ liệu: {ex.Message}");
            }
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

            var floors = await _floorCollection.Find(_ => true).ToListAsync();

            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                MapToResponse(room, floors, new List<Contract>()));
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

            var updatedRoom = _mapper.Map<Room>(request);
            updatedRoom.Id = id;
            updatedRoom.CreatedAt = existingRoom.CreatedAt;
            updatedRoom.UpdatedAt = DateTime.UtcNow;

            await _collection.ReplaceOneAsync(x => x.Id == id, updatedRoom);

            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var activeContracts = await _contractCollection
                .Find(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            return Ok(MapToResponse(updatedRoom, floors, activeContracts));
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (room is null)
                return NotFound(CommonMessage.NotFound("Phòng"));
            var activeContractExists = await _contractCollection
                .Find(c => c.RoomNumber == room.RoomNumber && c.Status == ContractStatus.Active)
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

        private RoomResponse MapToResponse(Room room, List<Floor> floors, List<Contract> activeContracts)
        {
            var response = _mapper.Map<RoomResponse>(room);

            var floor = floors.FirstOrDefault(f => f.Id == room.FloorId);
            response.FloorNumber = floor is not null ? floor.FloorNumber : 0;

            response.StatusLabel = room.Status switch
            {
                RoomStatus.Available => "Trống",
                RoomStatus.Occupied => "Đã thuê",
                RoomStatus.Maintenance => "Bảo trì",
                _ => room.Status.ToString()
            };

            // Contract liên kết với Room qua RoomNumber
            var contract = activeContracts.FirstOrDefault(c => c.RoomNumber == room.RoomNumber);
            response.TenantName = contract?.TenantName;

            return response;
        }
    }
}