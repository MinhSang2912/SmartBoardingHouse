using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common.Message;
using SmartBoardingHouse.Data;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;
using SmartBoardingHouse.Services;
using SmartBoardingHouse.Common;

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
                .Find(x => x.Email == request.Email)
                .FirstOrDefaultAsync();

            if (user is null)
                return BadRequest(CommonMessage.LoginEmailOrPasswordIsWrong());

            if (!PasswordHelper.Verify(request.Password, user.Password))
                return BadRequest(CommonMessage.LoginEmailOrPasswordIsWrong());

            user.RefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

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
                return BadRequest(CommonMessage.LoginEmailExists());

            var user = new User
            {
                Id = await MongoIdHelper.GetNextIdAsync(_userCollection),
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = PasswordHelper.Hash(request.Password),
                RoomNumber = string.Empty,
                CreatedAt = DateTime.UtcNow,
                RefreshToken = _jwtService.GenerateRefreshToken(),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };

            await _userCollection.InsertOneAsync(user);

            return Ok(MapToAuthResponse(user));
        }

        // POST: api/Auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Refresh token is required.");

            var user = await _userCollection
                .Find(x => x.RefreshToken == request.RefreshToken)
                .FirstOrDefaultAsync();

            if (user is null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized("Refresh token is invalid or expired.");

            user.RefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            var response = new
            {
                Token = _jwtService.GenerateToken(user),
                RefreshToken = user.RefreshToken
            };

            return Ok(response);
        }

        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("Email and new password are required.");

            var user = await _userCollection.Find(x => x.Email == request.Email).FirstOrDefaultAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            user.Password = PasswordHelper.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            return Ok(CommonMessage.Updated("Mật khẩu"));
        }

        // PUT: api/Auth/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("Current password and new password are required.");

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            if (!PasswordHelper.Verify(request.CurrentPassword, user.Password))
                return BadRequest("Mật khẩu hiện tại không đúng.");

            user.Password = PasswordHelper.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            return Ok(CommonMessage.Updated("Mật khẩu"));
        }

        // POST: api/Auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.FcmToken = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            return Ok(CommonMessage.Updated("Đăng xuất"));
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
                
                RoomNumber = user.RoomNumber,
                Token = _jwtService.GenerateToken(user)
            };
        }
    }
}