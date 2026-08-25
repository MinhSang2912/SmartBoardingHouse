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

        // Thêm ảnh
        public IFormFile? FrontImage { get; set; }
        public IFormFile? BackImage { get; set; }
    }

    public class UserRequestValidation : AbstractValidator<UserRequest>
    {
        public UserRequestValidation()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage(Message.UserNameIsRequired());
            RuleFor(x => x.IDCard)
                .NotEmpty().WithMessage(Message.UserIDCardNumberIsRequired())
                .Matches(@"^[0-9]{12}$").WithMessage("Số CCCD không hợp lệ (phải là 12 chữ số từ 0-9)!")
                .MinimumLength(10).WithMessage(Message.UserIDCardNumberIsTooShort());
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống!")
                .Matches(@"^[0-9]{10,11}$").WithMessage("Số điện thoại không hợp lệ (phải là 10 - 11 số)!");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(Message.LoginEmailIsRequired())
                .EmailAddress().WithMessage(Message.LoginEmailIsInvalid());
            RuleFor(x => x.FrontImage).Must(f => f == null || f.Length < 5 * 1024 * 1024)
                 .WithMessage(Message.ImageTooLong());
            RuleFor(x => x.BackImage).Must(f => f == null || f.Length < 5 * 1024 * 1024)
                 .WithMessage(Message.ImageTooLong());
                
        }
    }
}