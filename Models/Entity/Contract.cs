using FluentValidation;
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
            RuleFor(x => x.ContractNumber).NotEmpty().WithMessage("Contract number is required.");
            RuleFor(x => x.RoomNumber).NotEmpty().WithMessage("Room number is required.");
            RuleFor(x => x.TenantName).NotEmpty().WithMessage("Tenant name is required.");
            RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
            RuleFor(x => x.PaymentDate).InclusiveBetween(1, 31).WithMessage("Payment date must be between 1 and 31.");
        }
    }
}
