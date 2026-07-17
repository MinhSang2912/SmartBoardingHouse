using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class UserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCard { get; set; } = string.Empty;
        public string? Address {get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public class UserRequestValidation : AbstractValidator<UserRequest>
    {
        public UserRequestValidation()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage(Message.UserNameIsRequired());
            RuleFor(x => x.IDCard)
                .NotEmpty().WithMessage(Message.UserIDCardNumberIsRequired())
                .MinimumLength(10).WithMessage(Message.UserIDCardNumberIsTooShort());
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Message.LoginEmailIsRequired())
                .EmailAddress().WithMessage(Message.LoginEmailIsInvalid());
        }
    }
}