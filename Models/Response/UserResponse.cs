using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Role Role { get; set; }
        public string RoleLabel { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCardNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        // Thông tin bổ sung từ Room
        public decimal RoomDeposit { get; set; }
        public decimal Price { get; set; }
        // Ngày nhận phòng từ Contract
        public DateTime? StartDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
    }
}