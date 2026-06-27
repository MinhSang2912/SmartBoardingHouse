using FluentValidation;
using SmartBoardingHouse.Common;

namespace SmartBoardingHouse.Models.Request
{
    public class FloorRequest
    {
        public string Name { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string? Description { get; set; }
    }
    public class FloorRequestValidation : AbstractValidator<FloorRequest>
    {
        public FloorRequestValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(Message.FloorNameIsRequired());
            RuleFor(x => x.FloorNumber).NotEmpty().WithMessage(Message.FloorNumberIsRequired());
            RuleFor(x => x.FloorNumber).GreaterThanOrEqualTo(0).WithMessage(Message.FloorRoomCountMustBeNonNegative());
            RuleFor(x => x.Description).MaximumLength(200).WithMessage(Message.FloorDescriptionIsTooLong());
        }
    }
}
