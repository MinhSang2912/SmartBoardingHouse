namespace SmartBoardingHouse.Models.Entity
{
    public class Notification : BaseModel
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = "general";
        public int? RefId { get; set; }
        public string? RefModel { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
