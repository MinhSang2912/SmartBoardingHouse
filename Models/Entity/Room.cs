using FluentValidation;
using SmartBoardingHouse.Common;
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
            RuleFor(x => x.RoomNumber).NotEmpty().WithMessage(Message.RoomNumberIsRequired());
            RuleFor(x => x.Price).GreaterThan(0).WithMessage(Message.RoomPriceMustBeGreaterThanZero());
            RuleFor(x => x.Area).GreaterThan(0).WithMessage(Message.RoomAreaMustBeGreaterThanZero());
            RuleFor(x => x.RoomDeposit).GreaterThanOrEqualTo(0).WithMessage(Message.RoomDepositMustBeNonNegative());
            RuleFor(x => x.FloorId).GreaterThan(0).WithMessage(Message.RoomFloorIdIsRequired());
            RuleFor(x => x.Status).IsInEnum().WithMessage(Message.RoomStatusIsInvalid());
        }
    }
}
