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
    public class FloorsController : ControllerBase
    {
        private readonly IMongoCollection<Floor> _floorCollection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IValidator<FloorRequest> _validator;
        private readonly IMapper _mapper;

        public FloorsController(MongoDbService mongoService, IValidator<FloorRequest> validator, IMapper mapper)
        {
            _floorCollection = mongoService.GetDatabase().GetCollection<Floor>("floors");
            _roomCollection = mongoService.GetDatabase().GetCollection<Room>("rooms");
            _validator = validator;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<FloorResponse>> GetAll()
        {
            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var rooms = await _roomCollection.Find(_ => true).ToListAsync();

            var floorItems = floors.Select(floor =>
            {
                var roomsOnFloor = rooms.Where(r => r.FloorId == floor.Id).ToList();
                var activeRoomsOnFloor = roomsOnFloor.Where(r => r.IsActive && r.Status != RoomStatus.InActive).ToList();
                var inactiveRoomsOnFloor = roomsOnFloor.Where(r => !r.IsActive || r.Status == RoomStatus.InActive).ToList();

                var occupiedRooms = activeRoomsOnFloor.Count(r => r.Status == RoomStatus.Occupied);
                var emptyRooms = activeRoomsOnFloor.Count(r => r.Status == RoomStatus.Available);

                var item = _mapper.Map<FloorItemResponse>(floor);
                item.RoomCount = roomsOnFloor.Count;
                item.ActiveRooms = activeRoomsOnFloor.Count;
                item.InactiveRooms = inactiveRoomsOnFloor.Count;
                item.OccupiedRooms = occupiedRooms;
                item.EmptyRooms = emptyRooms;
                item.RevenueOnFloor = activeRoomsOnFloor
                    .Where(r => r.Status == RoomStatus.Occupied)
                    .Sum(r => r.Price);

                return item;
            }).ToList();

            var response = new FloorResponse
            {
                TotalFloors = floors.Count,
                TotalRooms = rooms.Count,
                TotalActiveRooms = rooms.Count(r => r.IsActive && r.Status != RoomStatus.InActive),
                TotalInactiveRooms = rooms.Count(r => !r.IsActive || r.Status == RoomStatus.InActive),
                MonthlyRevenue = floorItems.Sum(f => f.RevenueOnFloor),
                Floors = floorItems
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Floor>> GetById(string id)
        {
            var floor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (floor is null)
                return NotFound(CommonMessage.NotFound("Tầng"));
            return Ok(floor);
        }

        [HttpPost]
        public async Task<ActionResult<Floor>> Create(FloorRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var floorNumberExists = await _floorCollection
                .Find(x => x.FloorNumber == request.FloorNumber)
                .AnyAsync();

            if (floorNumberExists)
                errors.Add(CommonMessage.IsExists("Số Tầng"));

            var floorNameExists = await _floorCollection
                .Find(x => x.Name == request.Name)
                .AnyAsync();

            if (floorNameExists)
                errors.Add(CommonMessage.IsExists("Tên Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            var floor = _mapper.Map<Floor>(request);
            floor.CreatedAt = DateTime.UtcNow;

            await _floorCollection.InsertOneAsync(floor);

            return CreatedAtAction(nameof(GetById), new { id = floor.Id }, floor);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Floor>> Update(string id, FloorRequest updatedFloor)
        {
            var validationResult = await _validator.ValidateAsync(updatedFloor);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var existingFloor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingFloor is null)
                return NotFound(CommonMessage.NotFound("Tầng"));
            var floorNumberExists = await _floorCollection
                .Find(x => x.FloorNumber == updatedFloor.FloorNumber && x.Id != id)
                .AnyAsync();
            if (floorNumberExists)
                errors.Add(CommonMessage.IsExists("Số Tầng"));
            var floorNameExists = await _floorCollection
                .Find(x => x.Name == updatedFloor.Name && x.Id != id)
                .AnyAsync();
            if (floorNameExists)
                errors.Add(CommonMessage.IsExists("Tên Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            var floor = _mapper.Map<Floor>(updatedFloor);
            floor.Id = id;
            floor.UpdatedAt = DateTime.UtcNow;

            await _floorCollection.ReplaceOneAsync(x => x.Id == id, floor);

            return Ok(CommonMessage.Updated("Tầng"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingFloor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingFloor is null)
                return NotFound(CommonMessage.NotFound("Tầng"));

            var roomCount = await _roomCollection.CountDocumentsAsync(x => x.FloorId == id);
            if (roomCount > 0)
                return BadRequest(CommonMessage.FloorHasRooms());

            await _floorCollection.DeleteOneAsync(x => x.Id == id);
            return Ok(CommonMessage.Deleted("Tầng"));
        }
    }
}