namespace SmartBoardingHouse.Models.Entity
{
    public class Payment : BaseModel
    {
        public int TenantId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = "qr";
        public string Status { get; set; } = "pending";
        public string? TransactionId { get; set; }
        public string? QrData { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Note { get; set; }
    }
}
