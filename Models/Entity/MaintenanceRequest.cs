using SmartBoardingHouse.Models;
using static SmartBoardingHouse.Common.Enums;

public class MaintenanceRequest : BaseModel
{
    public string RequestNumber { get; set; } = string.Empty; 
    public string RoomNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PriotyRequest Priority { get; set; } = PriotyRequest.Low;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
}