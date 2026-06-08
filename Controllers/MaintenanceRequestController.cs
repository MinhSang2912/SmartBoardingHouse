using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _collection;
        private readonly IValidator<MaintenanceRequest> _validator;

        public MaintenanceRequestsController(MongoDbService mongoService, IValidator<MaintenanceRequest> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<MaintenanceRequest>("MaintenanceRequests");
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<List<MaintenanceRequest>>> GetAll()
        {
            var requests = await _collection.Find(_ => true).ToListAsync();
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequest>> GetById(int id)
        {
            var request = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return request is null ? NotFound() : Ok(request);
        }

        [HttpPost]
        public async Task<ActionResult<MaintenanceRequest>> Create(MaintenanceRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            await _collection.InsertOneAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MaintenanceRequest>> Update(int id, MaintenanceRequest updatedRequest)
        {
            var validationResult = await _validator.ValidateAsync(updatedRequest);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedRequest);
            return result.ModifiedCount > 0 ? Ok(updatedRequest) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0 ? NoContent() : NotFound();
        }
    }
}