using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class ContractRequest
    {
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PaymentDate { get; set; }
        public decimal Price { get; set; }
    }

    public class ContractRequestValidation : AbstractValidator<ContractRequest>
    {
        public ContractRequestValidation()
        {
           RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.ContractRoomNumberIsRequired());
           RuleFor(x => x.TenantName)
                .NotEmpty().WithMessage(Message.ContractTenantNameIsRequired());
           RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage(Message.ContractStartDateIsRequired())
                .LessThan(x => x.EndDate).WithMessage(Message.ContractStartDateMustBeBeforeEndDate());
           RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage(Message.ContractEndDateIsRequired())
                .GreaterThan(x => x.StartDate).WithMessage(Message.ContractEndDateMustBeAfterStartDate());
           RuleFor(x => x.PaymentDate)
                .InclusiveBetween(1, 31).WithMessage(Message.ContractPaymentDateIsInvalid());
           RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage(Message.ContractMonthlyRentMustBeGreaterThanZero());
        }
    }
}