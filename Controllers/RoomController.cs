using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IMongoCollection<Room> _collection;
        private readonly IValidator<Room> _validator;

        public RoomsController(MongoDbService mongoService, IValidator<Room> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<Room>("Rooms");
            _validator = validator;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<List<Room>>> GetAll()
        {
            var rooms = await _collection.Find(_ => true).ToListAsync();
            return Ok(rooms);
        }

        // GET: api/Rooms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetById(int id)
        {
            var room = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return room is null ? NotFound(Message.NotFound("Room")) : Ok(room);
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<Room>> Create(Room room)
        {
            var validationResult = await _validator.ValidateAsync(room);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            var roomNumberExists = await _collection
                .Find(x => x.RoomNumber == room.RoomNumber)
                .AnyAsync();

            if (roomNumberExists)
                errors.Add(Message.RoomNumberExists(room.RoomNumber));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(room);
            return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
        }

        // PUT: api/Rooms/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Room>> Update(int id, Room updatedRoom)
        {
            if (updatedRoom.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedRoom);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedRoom);

            return result.ModifiedCount > 0
                ? Ok(updatedRoom)
                : NotFound(Message.NotFound("Room"));
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("Room"))
                : NotFound(Message.NotFound("Room"));
        }
    }
}