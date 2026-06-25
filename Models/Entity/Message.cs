namespace SmartBoardingHouse.Models.Entity
{
    public class Message : BaseModel
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string SenderModel { get; set; } = "Tenant";
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
