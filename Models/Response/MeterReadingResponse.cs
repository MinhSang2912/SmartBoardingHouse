namespace SmartBoardingHouse.Models.Response
{
    public class MeterReadingResponse
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;  
        public int Month { get; set; }
        public int Year { get; set; }
        public string Period { get; set; } = string.Empty;      

        // Điện
        public double ElectricityIndex { get; set; }            
        public double PreviousElectricityIndex { get; set; }     
        public double ElectricityUsage { get; set; }            
        public decimal ElectricityTotal { get; set; }           

        // Nước
        public double WaterIndex { get; set; }               
        public double PreviousWaterIndex { get; set; }        
        public double WaterUsage { get; set; }                 
        public decimal WaterTotal { get; set; }                 

        public string? PhotoUrl { get; set; }
    }
}