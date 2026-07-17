using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCard { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? DateOfBirth { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public bool isActive { get; set; }
        public string? Role { get; set; }
    }
}
