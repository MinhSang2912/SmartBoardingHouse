using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;
using ValidationMessages = SmartBoardingHouse.Common.Message;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class NotificationRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.General;
        public string? RefId { get; set; }
        public string? RefModel { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
    }

    // Validator
    public class NotificationRequestValidator : AbstractValidator<NotificationRequest>
    {
        public NotificationRequestValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage(ValidationMessages.NotificationTenantIdRequired());

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(ValidationMessages.NotificationTitleRequired())
                .MaximumLength(200).WithMessage(ValidationMessages.NotificationTitleTooLong());

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage(ValidationMessages.NotificationBodyRequired())
                .MaximumLength(1000).WithMessage(ValidationMessages.NotificationBodyTooLong());
        }
    }
}