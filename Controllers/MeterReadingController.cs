using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMongoCollection<MeterReading> _collection;
        private readonly IValidator<MeterReading> _validator;

        public MeterReadingsController(MongoDbService mongoService, IValidator<MeterReading> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<MeterReading>("MeterReadings");
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<List<MeterReading>>> GetAll()
        {
            var readings = await _collection.Find(_ => true).ToListAsync();
            return Ok(readings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReading>> GetById(int id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return reading is null ? NotFound() : Ok(reading);
        }

        [HttpPost]
        public async Task<ActionResult<MeterReading>> Create(MeterReading meterReading)
        {
            var validationResult = await _validator.ValidateAsync(meterReading);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            await _collection.InsertOneAsync(meterReading);
            return CreatedAtAction(nameof(GetById), new { id = meterReading.Id }, meterReading);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MeterReading>> Update(int id, MeterReading updatedReading)
        {
            var validationResult = await _validator.ValidateAsync(updatedReading);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedReading);
            return result.ModifiedCount > 0 ? Ok(updatedReading) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0 ? NoContent() : NotFound();
        }
    }
}