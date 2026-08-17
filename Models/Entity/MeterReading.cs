using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng MeterReading lưu trữ thông tin về các chỉ số điện, nước của từng phòng trong nhà trọ.
    /// </summary>
    public class MeterReading : BaseModel
    {
        [BsonElement("room")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RoomId { get; set; } = string.Empty;

        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("contract")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ContractId { get; set; } = string.Empty;
        
        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = string.Empty;

        [BsonElement("type")]
        public MeterType Type { get; set; }

        [BsonElement("month")]
        public int Month { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        /// <summary>Số cũ (kWh hoặc m³) — field "previousReading" khớp Client</summary>
        [BsonElement("previousReading")]
        public double PreviousIndex { get; set; }

        /// <summary>Số mới (kWh hoặc m³) — field "currentReading" khớp Client</summary>
        [BsonElement("currentReading")]
        public double CurrentIndex { get; set; }

        /// <summary>Tiêu thụ = current - previous</summary>
        [BsonElement("usage")]
        public double Usage { get; set; }

        [BsonElement("unitPrice")]
        public decimal UnitPrice { get; set; }

        [BsonElement("totalCost")]
        public decimal TotalCost { get; set; }

        [BsonElement("imageUrl")]
        public string? PhotoUrl { get; set; }

        [BsonElement("readingDate")]
        public DateTime ReadingDate { get; set; } = DateTime.Now;

        [BsonElement("ocrRawText")]
        public string? OcrRawText { get; set; }

        [BsonElement("isVerified")]
        public bool IsVerified { get; set; }
    }
}
