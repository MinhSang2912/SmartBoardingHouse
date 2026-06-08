using FluentValidation;
using SmartBoardingHouse.Common;
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
        public string RoomNumber { get; set; } = string.Empty;
       
    }

    public class UserValidation : AbstractValidator<User>
    {
        public UserValidation()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage(Message.UserIdIsRequired());
            RuleFor(x => x.Name).NotEmpty().WithMessage(Message.UserNameIsRequired());
            RuleFor(x => x.Password).NotEmpty().WithMessage(Message.UserPasswordIsRequired());
            RuleFor(x => x.Role).NotEmpty().WithMessage(Message.UserRoleIsRequired());
            RuleFor(x => x.Role).IsInEnum().WithMessage(Message.UserRoleIsInvalid());
            RuleFor(x => x.IDCardNumber).NotEmpty().WithMessage(Message.UserIDCardNumberIsRequired());
            RuleFor(x => x.IDCardNumber).MinimumLength(10).WithMessage(Message.UserIDCardNumberIsTooShort());
        }
    }
}
