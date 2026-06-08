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
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IValidator<User> _validator;

        public UsersController(MongoDbService mongoService, IValidator<User> validator)
        {
            _collection = mongoService.GetDatabase().GetCollection<User>("Users");
            _validator = validator;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _collection.Find(_ => true).ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var user = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
            return user is null ? NotFound(Message.NotFound("User")) : Ok(user);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> Create(User user)
        {
            var validationResult = await _validator.ValidateAsync(user);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            // Kiểm tra Id đã tồn tại
            var idExists = await _collection.Find(x => x.Id == user.Id).AnyAsync();
            if (idExists)
                errors.Add(Message.IdExists("User", user.Id));

            if (errors.Any())
                return BadRequest(errors);

            await _collection.InsertOneAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> Update(int id, User updatedUser)
        {
            
            var validationResult = await _validator.ValidateAsync(updatedUser);
            var errors = validationResult.Errors
                                .Select(e => e.ErrorMessage)
                                .ToList();

            if (errors.Any())
                return BadRequest(errors);

            var result = await _collection.ReplaceOneAsync(x => x.Id == id, updatedUser);

            return result.ModifiedCount > 0
                ? Ok(updatedUser)
                : NotFound(Message.NotFound("User"));
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("User"))
                : NotFound(Message.NotFound("User"));
        }
    }
}