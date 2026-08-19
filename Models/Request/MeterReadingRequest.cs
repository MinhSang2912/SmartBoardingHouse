using FluentValidation;
using Microsoft.AspNetCore.Http;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Request
{
    public class MeterReadingRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public double ElectricityIndex { get; set; }
        public double WaterIndex { get; set; }
        public IFormFile? Photo { get; set; }   
    }

    public class MeterReadingRequestValidation : AbstractValidator<MeterReadingRequest>
    {
        public MeterReadingRequestValidation()
        {
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.MeterReadingRoomNumberIsRequired());
            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12).WithMessage(Message.MeterReadingMonthIsInvalid());
            RuleFor(x => x.Year)
                .GreaterThan(2000).WithMessage("Năm không hợp lệ.");
            RuleFor(x => x.ElectricityIndex)
                .GreaterThanOrEqualTo(0).WithMessage(Message.MeterReadingElectricityIndexMustBeNonNegative());
            RuleFor(x => x.WaterIndex)
                .GreaterThanOrEqualTo(0).WithMessage(Message.MeterReadingWaterIndexMustBeNonNegative());

            // Validate ảnh nếu có
            RuleFor(x => x.Photo)
                .Must(photo => photo == null || photo.Length <= 5 * 1024 * 1024)
                .WithMessage("Ảnh không được vượt quá 5MB.")
                .Must(photo => photo == null || new[] { "image/jpeg", "image/png", "image/jpg" }
                    .Contains(photo.ContentType))
                .WithMessage("Chỉ chấp nhận ảnh JPG hoặc PNG.");
        }
    }
}