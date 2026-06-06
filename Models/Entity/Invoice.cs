namespace SmartBoardingHouse.Models.Entity
{
    public class Invoice : BaseModel
    {
        public int ContractId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
