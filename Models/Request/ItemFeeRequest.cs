using FluentValidation;
using System.Linq;

namespace SmartBoardingHouse.Models.Request
{
    public class ItemFeeRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Unit { get; set; } = "tháng";
        public string Type { get; set; } = "mandatory"; // mandatory, wifi, parking
        public bool IsActive { get; set; } = true;
    }

    public class ItemFeeRequestValidator : AbstractValidator<ItemFeeRequest>
    {
        public ItemFeeRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(Common.Message.ItemFeeNameIsRequired());
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage(Common.Message.ItemFeePriceMustBeNonNegative());
            RuleFor(x => x.Unit).NotEmpty().WithMessage(Common.Message.ItemFeeUnitIsRequired());
        }
    }
}
