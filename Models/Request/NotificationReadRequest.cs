namespace SmartBoardingHouse.Models.Request
{
    public class NotificationReadRequest
    {
        public List<int>? NotificationIds { get; set; }
        public bool All { get; set; }
    }
}
