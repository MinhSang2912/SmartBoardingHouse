using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FloorsController : ControllerBase
    {
        private readonly IMongoCollection<Floor> _floorCollection;
        private readonly IMongoCollection<Room> _roomCollection;
        private readonly IValidator<FloorRequest> _validator;
        private readonly IMapper _mapper;

        public FloorsController(MongoDbService mongoService, IValidator<FloorRequest> validator, IMapper mapper)
        {
            _floorCollection = mongoService.GetDatabase().GetCollection<Floor>("Floors");
            _roomCollection = mongoService.GetDatabase().GetCollection<Room>("Rooms");
            _validator = validator;
            _mapper = mapper;
        }

        // GET: api/Floors
        [HttpGet]
        public async Task<ActionResult<FloorResponse>> GetAll()
        {
            var floors = await _floorCollection.Find(_ => true).ToListAsync();
            var rooms = await _roomCollection.Find(_ => true).ToListAsync();

            var floorItems = floors.Select(floor =>
            {
                // So sánh Id của Floor với FloorId của Room
                var roomsOnFloor = rooms.Where(r => r.FloorId == floor.Id).ToList();

                var occupiedRooms = roomsOnFloor.Count(r => r.Status == RoomStatus.Occupied);
                var emptyRooms = roomsOnFloor.Count(r => r.Status == RoomStatus.Available);

                return new FloorItemResponse
                {
                    Id = floor.Id,
                    FloorNumber = floor.FloorNumber,
                    RoomCount = roomsOnFloor.Count,
                    OccupiedRooms = occupiedRooms,
                    EmptyRooms = emptyRooms,
                    RevenueOnFloor = roomsOnFloor
                        .Where(r => r.Status == RoomStatus.Occupied)
                        .Sum(r => r.Price)
                };
            }).ToList();

            var response = new FloorResponse
            {
                TotalFloors = floors.Count,
                TotalRooms = rooms.Count,
                MonthlyRevenue = floorItems.Sum(f => f.RevenueOnFloor),
                Floors = floorItems
            };

            return Ok(response);
        }

        // GET: api/Floors/{id}
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Floor>> GetById(int id)
        //{
        //    var floor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //    return floor is null ? NotFound(Message.NotFound("Floor")) : Ok(floor);
        //}

        // POST: api/Floors
        [HttpPost]
        public async Task<ActionResult<Floor>> Create(FloorRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            var floorNumberExists = await _floorCollection
                .Find(x => x.FloorNumber == request.FloorNumber)
                .AnyAsync();

            if (floorNumberExists)
                errors.Add(Message.FloorNumberExists(request.FloorNumber));

            if (errors.Any())
                return BadRequest(errors);

            var floor = _mapper.Map<Floor>(request);
            floor.Id = await MongoIdHelper.GetNextIdAsync(_floorCollection);
            floor.CreatedAt = DateTime.UtcNow;

            await _floorCollection.InsertOneAsync(floor);

            return CreatedAtAction(nameof(GetAll), new { id = floor.Id }, floor);
        }

        // PUT: api/Floors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Floor>> Update(int id, FloorRequest updatedFloor)
        {
            var validationResult = await _validator.ValidateAsync(updatedFloor);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();
            var existingFloor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingFloor is null)
                return NotFound(Message.NotFound("Tầng"));

            if (errors.Any())
                return BadRequest(errors);

            var floor = _mapper.Map<Floor>(updatedFloor);
            floor.Id = id;
            var result = await _floorCollection.ReplaceOneAsync(x => x.Id == id, floor);

            return Ok(Message.Updated("Tầng"));
        }

        // DELETE: api/Floors/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existingFloor = await _floorCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existingFloor is null)
                return NotFound(Message.NotFound("Tầng"));

            if(existingFloor.RoomCount != 0)
                return BadRequest(Message.FloorHasRooms());
            var result = await _floorCollection.DeleteOneAsync(x => x.Id == id);
            return Ok(Message.Deleted("Tầng"));
        }
    }
}