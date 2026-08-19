using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Response
{
    public class MeterReadingResponse
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public MeterType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty; 
        public int Month { get; set; }
        public int Year { get; set; }
        /// <summary>
        /// kỳ ghi chỉ số, ví dụ: "28/5/2026" (ngày ghi chỉ số)
        /// </summary>
        public string Period { get; set; } = string.Empty;

        /// <summary>
        /// Số cũ, ví dụ: 100 (kWh hoặc m³)
        /// </summary>
        public double PreviousIndex { get; set; }

        /// <summary>
        /// Số mới, ví dụ: 250 (kWh hoặc m³)
        /// </summary>
        public double CurrentIndex { get; set; }

        /// <summary>
        /// Tiêu thụ, ví dụ: 150 (kWh hoặc m³)
        /// </summary>
        public double Usage { get; set; }                     
        public string UsageLabel { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }              
        public decimal Total { get; set; }                   
        public string? PhotoUrl { get; set; }
    }
}