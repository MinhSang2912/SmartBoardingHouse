using SmartBoardingHouse.Models.Entity;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class InvoiceResponse
    {
        // Thông tin chung
        public string Id { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }

        // Chi tiết thanh toán
        public decimal RoomPrice { get; set; }
        public double ElectricUsage { get; set; }
        public decimal ElectricPrice { get; set; }
        public decimal ElectricTotal { get; set; }
        public double WaterUsage { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal WaterTotal { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
