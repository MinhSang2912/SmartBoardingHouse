namespace SmartBoardingHouse.Models.Entity
{
    public class User: BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string IDCardNumber { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
    }
}
