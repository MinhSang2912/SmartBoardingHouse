using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class TenantRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class TenantRequestValidation : AbstractValidator<TenantRequest>
    {
        public TenantRequestValidation()
        {
            RuleFor(x => x.FullName)
               .NotEmpty().WithMessage(Message.UserNameIsRequired());
            RuleFor(x => x.IdCard)
                .NotEmpty().WithMessage(Message.UserIDCardNumberIsRequired())
                .MinimumLength(10).WithMessage(Message.UserIDCardNumberIsTooShort());
        }
    }
}
