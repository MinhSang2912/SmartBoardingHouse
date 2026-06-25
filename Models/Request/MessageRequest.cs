namespace SmartBoardingHouse.Models.Request
{
    public class MessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
    }
}
