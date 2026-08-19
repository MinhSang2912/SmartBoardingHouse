
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class MeterReading : BaseModel
    {
        public string RoomNumber { get; set; } = string.Empty;
        public MeterType Type { get; set; }         
        public int Month { get; set; }
        public int Year { get; set; }

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
        public string? PhotoUrl { get; set; }
    }
}