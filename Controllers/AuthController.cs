using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartBoardingHouse.Common;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly JwtService _jwtService;

        public AuthController(
            MongoDbService mongoService,
            IValidator<LoginRequest> loginValidator,
            IValidator<RegisterRequest> registerValidator,
            JwtService jwtService)
        {
            _userCollection = mongoService.GetDatabase().GetCollection<User>("Users");
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
            _jwtService = jwtService;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

            // Tìm user theo email + role
            var user = await _userCollection
                .Find(x => x.Email == request.Email && x.Role == request.Role)
                .FirstOrDefaultAsync();

            if (user is null)
                return BadRequest(Message.LoginEmailOrPasswordIsWrong());

            if (!PasswordHelper.Verify(request.Password, user.Password))
                return BadRequest(Message.LoginEmailOrPasswordIsWrong());

            return Ok(MapToAuthResponse(user));
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var validationResult = await _registerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

            // Kiểm tra email đã tồn tại chưa
            var emailExists = await _userCollection
                .Find(x => x.Email == request.Email)
                .AnyAsync();
            if (emailExists)
                return BadRequest(Message.LoginEmailExists());

            var user = new User
            {
                Id = await MongoIdHelper.GetNextIdAsync(_userCollection),
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = PasswordHelper.Hash(request.Password),
                Role = request.Role,
                RoomNumber = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _userCollection.InsertOneAsync(user);

            return Ok(MapToAuthResponse(user));
        }

        // ==================== HELPERS ====================

        private AuthResponse MapToAuthResponse(User user)
        {
            return new AuthResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                RoleLabel = user.Role switch
                {
                    Role.Owner => "Chủ nhà",
                    Role.Tenant => "Người thuê",
                    _ => user.Role.ToString()
                },
                RoomNumber = user.RoomNumber,
                Token = _jwtService.GenerateToken(user)
            };
        }
    }
}