using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Request
{
    public class MaintenanceRequestRequest
    {
        public string RequestNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PriotyRequest Priority { get; set; } = PriotyRequest.Low;
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
    }

    public class MaintenanceRequestRequestValidation : AbstractValidator<MaintenanceRequestRequest>
    {
        public MaintenanceRequestRequestValidation()
        {
            RuleFor(x => x.RequestNumber)
                .NotEmpty().WithMessage(Message.MaintenanceRequestNumberIsRequired());
            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(Message.MaintenanceRoomNumberIsRequired());
            RuleFor(x => x.TenantName)
                .NotEmpty().WithMessage(Message.MaintenanceTenantNameIsRequired());
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(Message.MaintenanceTitleIsRequired())
                .MaximumLength(200).WithMessage(Message.MaintenanceTitleIsTooLong());
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage(Message.MaintenanceDescriptionIsRequired())
                .MaximumLength(1000).WithMessage(Message.MaintenanceDescriptionIsTooLong());
            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage(Message.MaintenancePriorityIsInvalid());
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(Message.MaintenanceStatusIsInvalid());
        }
    }
}