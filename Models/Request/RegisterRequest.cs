using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class RegisterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Tenant;
    }

    public class RegisterRequestValidation : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(Message.RegisterNameIsRequired());
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Message.LoginEmailIsRequired())
                .EmailAddress().WithMessage(Message.LoginEmailIsInvalid());
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(Message.RegisterPhoneIsRequired())
                .Matches(@"^(0|\+84)[0-9]{9}$").WithMessage(Message.RegisterPhoneIsInvalid());
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(Message.LoginPasswordIsRequired())
                .MinimumLength(6).WithMessage(Message.RegisterPasswordTooShort());
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(Message.RegisterConfirmPasswordIsRequired())
                .Equal(x => x.Password).WithMessage(Message.RegisterPasswordNotMatch());
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage(Message.LoginRoleIsInvalid());
        }
    }
}