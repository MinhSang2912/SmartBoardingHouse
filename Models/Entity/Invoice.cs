using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Invoice : BaseModel
    {
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    }

    public class InvoiceValidation : AbstractValidator<Invoice>
    {
        public InvoiceValidation()
        {
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.InvoiceRoomNumberIsRequired());

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage(Message.InvoiceAmountMustBeGreaterThanZero());

            RuleFor(x => x.DueDate)
                .NotEmpty().WithMessage(Message.InvoiceDueDateIsRequired())
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage(Message.InvoiceDueDateMustBeInFuture());

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(Message.InvoiceStatusIsInvalid());
        }
    }
}