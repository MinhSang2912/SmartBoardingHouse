using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Models.Settings;
using static SmartBoardingHouse.Common.Enums;
using Message = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IValidator<UserRequest> _validator;
        private readonly IOptions<AdminSettings> _adminSettings;
        private readonly IMapper _mapper;

        public UsersController(MongoDbService mongoService, IValidator<UserRequest> validator, IOptions<AdminSettings> adminSettings, IMapper mapper)
        {
            _collection = mongoService.GetDatabase().GetCollection<User>("users");
            _validator = validator;
            _adminSettings = adminSettings;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserResponse>>> GetAll()
        {
            var users = await _collection.Find(x => x.Role != "Admin").ToListAsync();
            var response = _mapper.Map<List<UserResponse>>(users);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(string id)
        {
            var user = await _collection.Find(x => x.Id == id && x.Role != "Admin").FirstOrDefaultAsync();
            if (user == null)
                return NotFound(Message.NotFound("Người dùng"));
            var response = _mapper.Map<UserResponse>(user);
            return Ok(response);
         }

        [HttpPost]
        public async Task<ActionResult<User>> Create(UserRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            // Kiểm tra email đã tồn tại
            var emailExists = await _collection.Find(x => x.Email == request.Email).AnyAsync();
            if (emailExists)
                errors.Add("Email đã tồn tại");

            if (errors.Any())
                return BadRequest(errors);

            var user = _mapper.Map<User>(request);
            user.Password = PasswordHelper.Hash(request.Password);
            user.Role = "Tenant";
            user.CreatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(user);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<User>> Update(string id, UserRequest updatedUser)
        {
            var validationResult = await _validator.ValidateAsync(updatedUser);
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            var existingUser = await _collection.Find(x => x.Id == id && x.Role !="Admin").FirstOrDefaultAsync();
            if (existingUser is null)
                return NotFound(Message.NotFound("User"));

            if (errors.Any())
                return BadRequest(errors);

            var user = _mapper.Map<User>(updatedUser);
            user.Id = id;
            user.Role = "Tenant";
            user.UpdatedAt = DateTime.UtcNow;

            // Không cho phép thay đổi password qua API
            user.Password = existingUser.Password;

            await _collection.ReplaceOneAsync(x => x.Id == id, user);

            var response = _mapper.Map<UserResponse>(user);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _collection.DeleteOneAsync(x => x.Id == id && x.Role != "Admin");
            return result.DeletedCount > 0
                ? Ok(Message.Deleted("User"))
                : NotFound(Message.NotFound("User"));
        }
    }
}