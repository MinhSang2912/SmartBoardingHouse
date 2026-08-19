using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class RoomRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Area { get; set; }
        public decimal RoomDeposit { get; set; }
        public int maxOccupants { get; set; } = 2;
        public string FloorId { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Amenities { get; set; } = new();
    }

    public class RoomRequestValidation : AbstractValidator<RoomRequest>
    {
        public RoomRequestValidation()
        {
            RuleFor(x => x.RoomNumber).NotEmpty().WithMessage(Message.RoomNumberIsRequired());
            RuleFor(x => x.Price).GreaterThan(0).WithMessage(Message.RoomPriceMustBeGreaterThanZero());
            RuleFor(x => x.Area).GreaterThan(0).WithMessage(Message.RoomAreaMustBeGreaterThanZero());
            RuleFor(x => x.RoomDeposit).GreaterThanOrEqualTo(0).WithMessage(Message.RoomDepositMustBeNonNegative());
            RuleFor(x => x.FloorId).NotEmpty().WithMessage(Message.RoomFloorIdIsRequired());
            RuleFor(x => x.maxOccupants).GreaterThan(0).WithMessage(Message.MaxOccupantsMustBeGreateThanZero());
            RuleFor(x => x.Description).MaximumLength(200).WithMessage(Message.RoomDescriptionTooLong());
        }
    }
}