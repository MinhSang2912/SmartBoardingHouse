namespace SmartBoardingHouse.Models.Request
{
    public class MessageRequest
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
    }
}
