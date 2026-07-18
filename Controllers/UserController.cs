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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Message = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _collection;
        private readonly IMongoCollection<Contract> _contractCollection;
        private readonly IValidator<UserRequest> _validator;
        private readonly IOptions<AdminSettings> _adminSettings;
        private readonly IMapper _mapper;

        public UsersController(MongoDbService mongoService, IValidator<UserRequest> validator, IOptions<AdminSettings> adminSettings, IMapper mapper)
        {
            _collection = mongoService.GetDatabase().GetCollection<User>("users");
            _contractCollection = mongoService.GetDatabase().GetCollection<Contract>("contracts");
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
        public async Task<ActionResult<UserResponse>> Update(string id, UserRequest updatedUser)
        {
            // Validate
            var validationResult = await _validator.ValidateAsync(updatedUser);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(errors);
            }

            // Tìm user hiện tại
            var existingUser = await _collection
                .Find(x => x.Id == id && x.Role != "Admin")
                .FirstOrDefaultAsync();

            if (existingUser is null)
                return NotFound(Message.NotFound("User"));

            // Map chỉ các trường cần updat
            _mapper.Map(updatedUser, existingUser);          

            existingUser.UpdatedAt = DateTime.UtcNow;

            // Lưu vào DB
            await _collection.ReplaceOneAsync(x => x.Id == id, existingUser);

            // Trả về response
            var response = _mapper.Map<UserResponse>(existingUser);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _collection.Find(x => x.Id == id && x.Role != "Admin").FirstOrDefaultAsync();
            if (user == null)
                return NotFound(Message.NotFound("Người dùng"));

            var existingContract = await _contractCollection.Find(x => x.TenantId == id 
                        && x.Status == ContractStatus.Active).FirstOrDefaultAsync();
            if (existingContract is not null)
                return BadRequest(Message.UserHasActiveContract());

            user.IsActive = false;
            return Ok(Message.Deleted("Người dùng"));
        }
    }
}