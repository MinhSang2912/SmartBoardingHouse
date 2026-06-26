namespace SmartBoardingHouse.Models.Request
{
    public class NotificationReadRequest
    {
        public List<string>? NotificationIds { get; set; }
        public bool All { get; set; }
    }
}
