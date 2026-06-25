using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class User: BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = "";
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCardNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = "Chưa có phòng";
        public string AvatarUrl { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? FcmToken { get; set; }
    }
}
