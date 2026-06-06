using FluentValidation;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class MaintenanceRequest : BaseModel
    {
        public string RoomName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
    }

    public class  MaintenanceRequestValidation : AbstractValidator<MaintenanceRequest>
    {
        public MaintenanceRequestValidation()
        {
            RuleFor(x => x.RoomName).NotEmpty().WithMessage("Room name is required.");
            RuleFor(x => x.TenantName).NotEmpty().WithMessage("Tenant name is required.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
        }
    }
}
