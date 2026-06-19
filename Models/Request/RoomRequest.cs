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
        public int FloorId { get; set; }
    }

    public class RoomRequestValidation : AbstractValidator<RoomRequest>
    {
        public RoomRequestValidation()
        {
            RuleFor(x => x.RoomNumber).NotEmpty().WithMessage(Message.RoomNumberIsRequired());
            RuleFor(x => x.Price).GreaterThan(0).WithMessage(Message.RoomPriceMustBeGreaterThanZero());
            RuleFor(x => x.Area).GreaterThan(0).WithMessage(Message.RoomAreaMustBeGreaterThanZero());
            RuleFor(x => x.RoomDeposit).GreaterThanOrEqualTo(0).WithMessage(Message.RoomDepositMustBeNonNegative());
            RuleFor(x => x.FloorId).GreaterThan(0).WithMessage(Message.RoomFloorIdIsRequired());
        }
    }
}