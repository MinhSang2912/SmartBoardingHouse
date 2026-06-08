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
    public class MaintenanceRequestsController : ControllerBase
    {
        private readonly IMongoCollection<MaintenanceRequest> _collection;
        private readonly IValidator<MaintenanceRequest> _validator;

        public MaintenanceRequestsController(MongoDbService mongoService, IValidator<MaintenanceRequest> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<MaintenanceRequest>("MaintenanceRequests");
            _validator = validator;
        }

        // GET: api/MaintenanceRequests
        [HttpGet]
        public async Task<ActionResult<List<MaintenanceRequest>>> GetAll()
        {
            var requests = await _collection.Find(_ => true).ToListAsync();
            return Ok(requests);
        }

        // GET: api/MaintenanceRequests/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequest>> GetById(int id)
        {
            var request = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return request is null ? NotFound(Message.NotFound("MaintenanceRequest")) : Ok(request);
        }

        // POST: api/MaintenanceRequests
        [HttpPost]
        public async Task<ActionResult<MaintenanceRequest>> Create(MaintenanceRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra yêu cầu bảo trì trùng (RoomNumber + Title)
            var exists = await _collection
                .Find(x => x.RoomNumber == request.RoomNumber && x.Title == request.Title)
                .AnyAsync();

            if (exists)
                errors.Add(Message.MaintenanceRequestExists(request.RoomNumber, request.Title));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }

        // PUT: api/MaintenanceRequests/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<MaintenanceRequest>> Update(int id, MaintenanceRequest updatedRequest)
        {
            if (updatedRequest.Id != id)
            {
                return BadRequest(new List<string> { "Id in URL and body must match." });
            }

            var validationResult = await _validator.ValidateAsync(updatedRequest);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedRequest);

            return result.ModifiedCount > 0
                ? Ok(updatedRequest)
                : NotFound(Message.NotFound("MaintenanceRequest"));
        }

        // DELETE: api/MaintenanceRequests/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("MaintenanceRequest"))
                : NotFound(Message.NotFound("MaintenanceRequest"));
        }
    }
}