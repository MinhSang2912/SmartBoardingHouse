using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    // Đại diện cho cùng collection "Tenant" mà Client (Mongoose model Tenant.js) dùng.
    // Tên class vẫn giữ "User" theo convention cũ của Admin (Request/Response đã dùng tên này),
    // nhưng field đã khớp 100% với Tenant.js.
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
        public DateTime? DateOfBirth { get; set; }

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
    }
}
