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
        public string RoomNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string? Address { get; set; }
        // Thông tin bổ sung từ Room
        public decimal RoomDeposit { get; set; }
        public decimal Price { get; set; }
        // Ngày nhận phòng từ Contract
        public DateTime? StartDate { get; set; }
    }
}
