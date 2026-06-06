using FluentValidation;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Room: BaseModel
    {
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Area { get; set; }
        public decimal RoomDeposit { get; set; }
        public int FloorId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
      
    }

    public class RoomValidation : AbstractValidator<Room>
    {
        public RoomValidation()
        {
            RuleFor(x => x.RoomNumber).NotEmpty().WithMessage("Room number is required.");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
            RuleFor(x => x.Area).GreaterThan(0).WithMessage("Area must be greater than 0.");
            RuleFor(x => x.RoomDeposit).GreaterThanOrEqualTo(0).WithMessage("Room deposit must be greater than or equal to 0.");
        }
    }
}
