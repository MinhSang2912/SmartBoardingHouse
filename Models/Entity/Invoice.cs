using FluentValidation;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Invoice : BaseModel
    {
        public string RoomName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    }

    public class InvoiceValidation : AbstractValidator<Invoice>
    {
        public InvoiceValidation()
        {
            RuleFor(x => x.RoomName).NotEmpty().WithMessage("Room name is required.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
            RuleFor(x => x.DueDate).GreaterThan(DateTime.Now).WithMessage("Due date must be in the future.");
        }
    }
}
