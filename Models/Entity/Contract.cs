using FluentValidation;
using CommonMessage = SmartBoardingHouse.Common.Message;
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
        public decimal Price { get; set; }
        public decimal RoomDeposit { get; set; }
        public int RemainTime => (EndDate > DateTime.Now) ? (EndDate - DateTime.Now).Days : 0;
        public ContractStatus Status { get; set; } 
    }

    public class ContractValidation : AbstractValidator<Contract>
    {
        public ContractValidation()
        {
            RuleFor(x => x.ContractNumber)
                .NotEmpty().WithMessage(CommonMessage.ContractNumberIsRequired());

            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(CommonMessage.ContractRoomNumberIsRequired());

            RuleFor(x => x.TenantName)
                .NotEmpty().WithMessage(CommonMessage.ContractTenantNameIsRequired());

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage(CommonMessage.ContractStartDateIsRequired())
                .LessThan(x => x.EndDate).WithMessage(CommonMessage.ContractStartDateMustBeBeforeEndDate());

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage(CommonMessage.ContractEndDateIsRequired())
                .GreaterThan(x => x.StartDate).WithMessage(CommonMessage.ContractEndDateMustBeAfterStartDate());

            RuleFor(x => x.PaymentDate)
                .InclusiveBetween(1, 31).WithMessage(CommonMessage.ContractPaymentDateIsInvalid());

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(CommonMessage.ContractStatusIsInvalid());
        }
    }
}