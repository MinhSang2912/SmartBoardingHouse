using FluentValidation;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Request
{
    public class FloorRequest
    {
        public string FloorNumber { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }
    public class FloorRequestValidation : AbstractValidator<FloorRequest>
    {
        public FloorRequestValidation()
        {
            RuleFor(x => x.FloorNumber).NotEmpty().WithMessage(Message.FloorNumberIsRequired());
            RuleFor(x => x.RoomCount).GreaterThanOrEqualTo(0).WithMessage(Message.FloorRoomCountMustBeNonNegative());
        }
    }
}
