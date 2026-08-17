using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng User lưu trữ thông tin về người dùng trong hệ thống, 
    /// bao gồm tên, email, số điện thoại, mật khẩu và các thông tin liên quan khác.
    /// </summary>
    public class User : BaseModel
    {
        [BsonElement("fullName")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("idCard")]
        public string IDCard { get; set; } = string.Empty;

        [BsonElement("avatar")]
        public string AvatarUrl { get; set; } = string.Empty;

        [BsonElement("address")]
        public string? Address { get; set; }

        [BsonElement("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; } = null;

        // ObjectId ref tới Room hiện tại (khớp field "room" trong Tenant.js)
        [BsonElement("room")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? RoomId { get; set; }

        // Cache hiển thị, đồng bộ khi Tenant được gán/đổi phòng
        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = "Chưa có phòng";

        [BsonElement("refreshToken")]
        public string? RefreshToken { get; set; }

        [BsonElement("refreshTokenExpiry")]
        public DateTime? RefreshTokenExpiry { get; set; }

        [BsonElement("fcmToken")]
        public string? FcmToken { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("resetOtp")]
        public string? ResetOtp { get; set; }

        [BsonElement("resetOtpExpiry")]
        public DateTime? ResetOtpExpiry { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } = "Tenant";

        [BsonElement("frontImage")]
        public string? FrontImageUrl { get; set; }

        [BsonElement("backImage")]
        public string? BackImageUrl { get; set; }
    }
}
