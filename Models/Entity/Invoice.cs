using FluentValidation;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Invoice : BaseModel
    {
        public string InvoiceNumber { get; set; } = string.Empty;
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
        public decimal Amount { get; set; }      
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    }
}