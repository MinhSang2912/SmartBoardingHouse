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
        public bool IsActive { get; set; }
        public string? Role { get; set; }

        // === Thông tin phòng đang thuê ===
        public int ActiveRoomCount { get; set; }                      // Số phòng đang thuê
        public List<string> ActiveRoomNumbers { get; set; } = new();  // Danh sách số phòng (P101, P205,...)
        public List<string> ActiveRoomIds { get; set; } = new();      // Danh sách RoomId (nếu cần)

        // Giữ lại để tương thích frontend cũ (hiển thị dạng chuỗi)
        public string RoomNumber { get; set; } = "Chưa có phòng";
    }
}