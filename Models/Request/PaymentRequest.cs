namespace SmartBoardingHouse.Models.Request
{
    public class PaymentRequest
    {
        public string InvoiceId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = "qr";
    }
}
