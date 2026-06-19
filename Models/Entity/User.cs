using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class User: BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = "";
        public Role Role { get; set; } = Role.Tenant;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCardNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
       
    }
}
