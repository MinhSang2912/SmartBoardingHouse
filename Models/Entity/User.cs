using FluentValidation;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class User: BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Role Role { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCardNumber { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
       
    }

    public class UserValidation : AbstractValidator<User>
    {
        public UserValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Role).NotEmpty().WithMessage("Role is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
            RuleFor(x => x.IDCardNumber).NotEmpty().WithMessage("ID card number is required.");
        }
    }
}
