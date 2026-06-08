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
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMongoCollection<MeterReading> _collection;
        private readonly IValidator<MeterReading> _validator;

        public MeterReadingsController(MongoDbService mongoService, IValidator<MeterReading> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<MeterReading>("MeterReadings");
            _validator = validator;
        }

        // GET: api/MeterReadings
        [HttpGet]
        public async Task<ActionResult<List<MeterReading>>> GetAll()
        {
            var readings = await _collection.Find(_ => true).ToListAsync();
            return Ok(readings);
        }

        // GET: api/MeterReadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReading>> GetById(int id)
        {
            var reading = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return reading is null ? NotFound(Message.NotFound("MeterReading")) : Ok(reading);
        }

        // POST: api/MeterReadings
        [HttpPost]
        public async Task<ActionResult<MeterReading>> Create(MeterReading meterReading)
        {
            var validationResult = await _validator.ValidateAsync(meterReading);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra chỉ số điện/nước đã tồn tại cho Room + Tháng + Năm
            var exists = await _collection
                .Find(x => x.RoomNumber == meterReading.RoomNumber
                        && x.Month == meterReading.Month
                        && x.Year == meterReading.Year)
                .AnyAsync();

            if (exists)
                errors.Add(Message.MeterReadingAlreadyExists(meterReading.RoomNumber, meterReading.Month, meterReading.Year));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(meterReading);
            return CreatedAtAction(nameof(GetById), new { id = meterReading.Id }, meterReading);
        }

        // PUT: api/MeterReadings/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<MeterReading>> Update(int id, MeterReading updatedReading)
        {
            if (updatedReading.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedReading);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedReading);

            return result.ModifiedCount > 0
                ? Ok(updatedReading)
                : NotFound(Message.NotFound("MeterReading"));
        }

        // DELETE: api/MeterReadings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("MeterReading"))
                : NotFound(Message.NotFound("MeterReading"));
        }
    }
}