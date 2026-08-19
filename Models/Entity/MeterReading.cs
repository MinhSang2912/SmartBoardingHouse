using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class MeterReading : BaseModel
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public double ElectricityIndex { get; set; }
        public double WaterIndex { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class MeterReadingValidation : AbstractValidator<MeterReading>
    {
        public MeterReadingValidation()
        {
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.MeterReadingRoomNumberIsRequired());

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12).WithMessage(Message.MeterReadingMonthIsInvalid());

            RuleFor(x => x.ElectricityIndex)
                .GreaterThanOrEqualTo(0).WithMessage(Message.MeterReadingElectricityIndexMustBeNonNegative());

            RuleFor(x => x.WaterIndex)
                .GreaterThanOrEqualTo(0).WithMessage(Message.MeterReadingWaterIndexMustBeNonNegative());
        }
    }
}