using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class ContractResponse
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PaymentDate { get; set; }
        public string PaymentDateLabel { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public decimal RoomDeposit { get; set; }
        public ContractStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty; 
        public int RemainTime { get; set; }
    }
}