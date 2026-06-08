using FluentValidation;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Entity
{
    public class Floor : BaseModel
    {
        public string FloorNumber { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }

    public class FloorValidation : AbstractValidator<Floor>
    {
        public FloorValidation()
        {
            RuleFor(x => x.FloorNumber).NotEmpty().WithMessage(Message.FloorNumberIsRequired());
            RuleFor(x => x.RoomCount).GreaterThanOrEqualTo(0).WithMessage(Message.FloorRoomCountMustBeNonNegative());
        }
    }
}