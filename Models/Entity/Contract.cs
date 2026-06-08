using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Contract : BaseModel
    {
        public string ContractNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PaymentDate { get; set; }
        public int RemainTime => (EndDate > DateTime.Now) ? (EndDate - DateTime.Now).Days : 0;
        public ContractStatus Status { get; set; } = ContractStatus.Active;
    }

    public class ContractValidation : AbstractValidator<Contract>
    {
        public ContractValidation()
        {
            RuleFor(x => x.ContractNumber)
                .NotEmpty().WithMessage(Message.ContractNumberIsRequired());

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

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(Message.ContractStatusIsInvalid());
        }
    }
}