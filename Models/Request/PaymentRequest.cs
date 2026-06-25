namespace SmartBoardingHouse.Models.Request
{
    public class PaymentRequest
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = "qr";
    }
}
