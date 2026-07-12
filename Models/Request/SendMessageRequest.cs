using FluentValidation;
using ValidationMessages = SmartBoardingHouse.Common.Message;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class SendMessageRequest
    {
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string? Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public IFormFile? Image { get; set; }
    }

    // Validator
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage(ValidationMessages.MessageSenderIdRequired());

            RuleFor(x => x.ReceiverId)
                .NotEmpty().WithMessage(ValidationMessages.MessageReceiverIdRequired());

            RuleFor(x => x.Content)
                .NotEmpty().When(x => x.Type == MessageType.Text)
                .WithMessage(ValidationMessages.MessageContentRequired())
                .MaximumLength(2000).WithMessage(ValidationMessages.MessageContentTooLong());

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage(ValidationMessages.MessageTypeInvalid());
        }
    }
}