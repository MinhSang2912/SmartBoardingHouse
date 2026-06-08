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
    public class FloorsController : ControllerBase
    {
        private readonly IMongoCollection<Floor> _collection;
        private readonly IValidator<Floor> _validator;

        public FloorsController(MongoDbService mongoService, IValidator<Floor> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<Floor>("Floors");
            _validator = validator;
        }

        // GET: api/Floors
        [HttpGet]
        public async Task<ActionResult<List<Floor>>> GetAll()
        {
            var floors = await _collection.Find(_ => true).ToListAsync();
            return Ok(floors);
        }

        // GET: api/Floors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Floor>> GetById(int id)
        {
            var floor = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return floor is null ? NotFound(Message.NotFound("Floor")) : Ok(floor);
        }

        // POST: api/Floors
        [HttpPost]
        public async Task<ActionResult<Floor>> Create(Floor floor)
        {
            var validationResult = await _validator.ValidateAsync(floor);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra FloorNumber đã tồn tại chưa
            var floorNumberExists = await _collection
                .Find(x => x.FloorNumber == floor.FloorNumber)
                .AnyAsync();

            if (floorNumberExists)
                errors.Add(Message.FloorNumberExists(floor.FloorNumber));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(floor);
            return CreatedAtAction(nameof(GetById), new { id = floor.Id }, floor);
        }

        // PUT: api/Floors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Floor>> Update(int id, Floor updatedFloor)
        {
            if (updatedFloor.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedFloor);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedFloor);

            return result.ModifiedCount > 0
                ? Ok(updatedFloor)
                : NotFound(Message.NotFound("Floor"));
        }

        // DELETE: api/Floors/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("Floor"))
                : NotFound(Message.NotFound("Floor"));
        }
    }
}