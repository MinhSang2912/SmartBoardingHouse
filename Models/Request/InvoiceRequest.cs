using FluentValidation;
using SmartBoardingHouse.Models.Entity;
using Message = SmartBoardingHouse.Common.Message;

namespace SmartBoardingHouse.Models.Request
{
    public class InvoiceRequest
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public decimal RoomPrice { get; set; }
        public double ElectricUsage { get; set; }
        public decimal ElectricPrice { get; set; }
        public double WaterUsage { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public DateTime DueDate { get; set; }
        public string? Note { get; set; }
        public InvoiceItemRequest[]? Items { get; set; }
    }

    public class InvoiceItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class InvoiceRequestValidation : AbstractValidator<InvoiceRequest>
    {
        public InvoiceRequestValidation()
        {
            RuleFor(x => x.InvoiceNumber)
                .NotEmpty().WithMessage(Message.InvoiceNumberIsRequired());
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.InvoiceRoomNumberIsRequired());
            RuleFor(x => x.TenantName)
                .NotEmpty().WithMessage(Message.ContractTenantNameIsRequired());
            RuleFor(x => x.BillingMonth)
                .InclusiveBetween(1, 12).WithMessage(Message.InvoiceBillingMonthIsInvalid());
            RuleFor(x => x.BillingYear)
                .GreaterThan(2000).WithMessage(Message.InvoiceBillingYearIsInvalid());
            RuleFor(x => x.RoomPrice)
                .GreaterThan(0).WithMessage(Message.RoomPriceMustBeGreaterThanZero());
            RuleFor(x => x.ElectricUsage)
                .GreaterThanOrEqualTo(0).WithMessage(Message.ElectricUsageIsInvalid());
            RuleFor(x => x.ElectricPrice)
                .GreaterThan(0).WithMessage(Message.ElectricPriceMustBeGreaterThanZero());
            RuleFor(x => x.WaterUsage)
                .GreaterThanOrEqualTo(0).WithMessage(Message.WaterUsageIsInvalid());
            RuleFor(x => x.WaterPrice)
                .GreaterThan(0).WithMessage(Message.WaterPriceMustBeGreaterThanZero());
            RuleFor(x => x.ServiceFee)
                .GreaterThanOrEqualTo(0).WithMessage(Message.ServiceFeeIsInvalid());
            RuleFor(x => x.DueDate)
                .NotEmpty().WithMessage(Message.InvoiceDueDateIsRequired());
            RuleFor(x => x.ContractNumber)
                .NotEmpty().WithMessage(Message.ContractNumberIsRequired());
            RuleFor(x => x.Note)
                .MaximumLength(200).WithMessage(Message.DescriptionTooLong());
            RuleForEach(x => x.Items).SetValidator(new InvoiceItemRequestValidation());
        }
    }

    public class InvoiceItemRequestValidation : AbstractValidator<InvoiceItemRequest>
    {
        public InvoiceItemRequestValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(Message.InvoiceItemNameIsRequired());
            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage(Message.InvoiceItemPriceMustBeGreaterThanZero());
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage(Message.InvoiceItemQuantityMustBeGreaterThanZero());
        }
    }
}