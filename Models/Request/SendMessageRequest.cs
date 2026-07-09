using FluentValidation;

namespace SmartBoardingHouse.Models.Request
{
    public class SendMessageRequest
    {
        public string ConversationId { get; set; } = string.Empty;
        public string SenderRole { get; set; } = "Tenant"; // Tenant hoặc Admin
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text hoặc image
        public string? ImageUrl { get; set; }
    }

    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("ConversationId là bắt buộc");

            RuleFor(x => x.SenderRole)
                .NotEmpty().WithMessage("SenderRole là bắt buộc")
                .Must(role => role == "Tenant" || role == "Admin")
                .WithMessage("SenderRole chỉ được là 'Tenant' hoặc 'Admin'");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống")
                .MaximumLength(2000).WithMessage("Nội dung tin nhắn tối đa 2000 ký tự");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Type là bắt buộc")
                .Must(type => type == "text" || type == "image")
                .WithMessage("Type chỉ được là 'text' hoặc 'image'");

            // Validation bổ sung khi gửi hình ảnh
            When(x => x.Type == "image", () =>
            {
                RuleFor(x => x.ImageUrl)
                    .NotEmpty().WithMessage("Phải có ImageUrl khi type là 'image'");
            });
        }
    }
}
