using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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
            return room is null ? NotFound() : Ok(room);
        }

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<Room>> Create(Room room)
        {
            var validationResult = await _validator.ValidateAsync(room);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            await _collection.InsertOneAsync(room);
            return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
        }

        // PUT: api/Rooms/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Room>> Update(int id, Room updatedRoom)
        {
            var validationResult = await _validator.ValidateAsync(updatedRoom);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedRoom);
            return result.ModifiedCount > 0 ? Ok(updatedRoom) : NotFound();
        }

        // DELETE: api/Rooms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0 ? NoContent() : NotFound();
        }
    }
}