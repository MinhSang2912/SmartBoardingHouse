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
            _userCollection = mongoService.GetDatabase().GetCollection<User>("users");
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

            // Tìm user theo email
            var user = await _userCollection
                .Find(x => x.Email == request.Email)
                .FirstOrDefaultAsync();

            if (user is null)
                return BadRequest(CommonMessage.LoginEmailOrPasswordIsWrong());

            if (!PasswordHelper.Verify(request.Password, user.Password))
                return BadRequest(CommonMessage.LoginEmailOrPasswordIsWrong());

            // Web quản lý này chỉ dành cho chủ nhà (Admin). Tenant đăng nhập qua
            // app di động (backend Node.js riêng), không được vào đây.
            if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Chỉ tài khoản quản trị (chủ nhà) mới được đăng nhập vào hệ thống này.");

            user.RefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);

            return Ok(MapToAuthResponse(user));
        }

        // POST: api/Auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = userIdClaim;

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