using FluentValidation;

namespace SmartBoardingHouse.Models.Entity
{
    public class MeterReading : BaseModel
    {
        public string RoomName { get; set; } = string.Empty;
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
            RuleFor(x => x.RoomName).NotEmpty().WithMessage("Room name is required.");
            RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
            RuleFor(x => x.Year).GreaterThan(2000).WithMessage("Year must be greater than 2000.");
            RuleFor(x => x.ElectricityIndex).GreaterThanOrEqualTo(0).WithMessage("Electricity index must be greater than or equal to 0.");
            RuleFor(x => x.WaterIndex).GreaterThanOrEqualTo(0).WithMessage("Water index must be greater than or equal to 0.");
        }
    }
}
