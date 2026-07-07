using FluentValidation;
using Microsoft.AspNetCore.Http;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class MeterReadingRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
        public double CurrentIndex { get; set; }     
        public MeterType Type { get; set; }         
        public IFormFile? Photo { get; set; }
        public string? OcrRawText { get; set; }
    }

    public class MeterReadingRequestValidation : AbstractValidator<MeterReadingRequest>
    {
        public MeterReadingRequestValidation()
        {
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.MeterReadingRoomNumberIsRequired());
            RuleFor(x => x.CurrentIndex)
                .GreaterThanOrEqualTo(0).WithMessage(Message.MeterReadingElectricityIndexMustBeNonNegative());
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage(Message.MeterReadingTypeInValid());
            RuleFor(x => x.Photo)
                .Must(p => p == null || p.Length <= 5 * 1024 * 1024)
                .WithMessage("Ảnh không được vượt quá 5MB.")
                .Must(p => p == null || new[] { "image/jpeg", "image/png", "image/jpg" }.Contains(p.ContentType))
                .WithMessage("Chỉ chấp nhận ảnh JPG hoặc PNG.");
            RuleFor(x => x.OcrRawText)
                .MaximumLength(200).WithMessage(Message.DescriptionTooLong());
        }
    }
}