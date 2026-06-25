using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CommonMessage = SmartBoardingHouse.Common.Message;
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
    [Authorize]
    public class ProfileController : AuthorizedControllerBase
    {
        private readonly PhotoService _photoService;

        public ProfileController(MongoDbService mongoService, PhotoService photoService)
            : base(mongoService)
        {
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<UserResponse>> GetProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            return Ok(MapToResponse(user));
        }

        [HttpPut]
        public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            user.Name = request.Name;
            user.PhoneNumber = request.PhoneNumber;
            user.IDCardNumber = request.IDCardNumber;
            user.Address = request.Address;
            user.DateOfBirth = request.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);
            return Ok(MapToResponse(user));
        }

        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar is null)
                return BadRequest("Vui lòng chọn ảnh.");

            var user = await GetCurrentUserAsync();
            if (user is null)
                return NotFound(CommonMessage.NotFound("Người dùng"));

            try
            {
                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                    await _photoService.DeletePhotoAsync(user.AvatarUrl);

                // Upload new avatar
                var photoUrl = await _photoService.SaveAvatarAsync(avatar, user.Id.ToString());
                user.AvatarUrl = photoUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _userCollection.ReplaceOneAsync(x => x.Id == user.Id, user);
                return Ok(new { avatarUrl = photoUrl, message = "Cập nhật avatar thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi tải ảnh: {ex.Message}");
            }
        }

        private UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IDCardNumber = user.IDCardNumber,
                RoomNumber = user.RoomNumber,
                AvatarUrl = user.AvatarUrl,
                Address = user.Address,
            };
        }
    }
}
