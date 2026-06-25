using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestValidation : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Message.LoginEmailIsRequired())
                .EmailAddress().WithMessage(Message.LoginEmailIsInvalid());
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(Message.LoginPasswordIsRequired());
        }
    }
}