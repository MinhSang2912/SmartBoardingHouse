using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Contract : BaseModel
    {
        public string ContractNumber { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PaymentDate { get; set; }
        public int RemainDays => (EndDate - DateTime.Now).Days;
        public ContractStatus Status { get; set; }
    }
}
