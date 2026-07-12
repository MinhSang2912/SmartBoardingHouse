using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CommonMessage = SmartBoardingHouse.Common.Message;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    /// <summary>
    /// Bảng Contract lưu trữ thông tin về các hợp đồng thuê phòng. 
    /// Mỗi hợp đồng liên kết một phòng với một người thuê và chứa thông tin về thời gian thuê, 
    /// giá thuê, tiền đặt cọc, điều khoản hợp đồng và trạng thái hợp đồng.
    /// </summary>
    public class Contract : BaseModel
    {
        [BsonElement("contractNumber")]
        public string ContractNumber { get; set; } = string.Empty;

        [BsonElement("room")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RoomId { get; set; } = string.Empty;

        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string TenantId { get; set; } = string.Empty;

        // Cache hiển thị, không phải nguồn sự thật
        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = string.Empty;

        [BsonElement("tenantName")]
        public string TenantName { get; set; } = string.Empty;

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("paymentDate")]
        public int PaymentDate { get; set; }

        [BsonElement("monthlyRent")]
        public decimal Price { get; set; }

        [BsonElement("deposit")]
        public decimal RoomDeposit { get; set; }

        [BsonElement("terms")]
        public string? Terms { get; set; }

        [BsonElement("signedDate")]
        public DateTime SignedDate { get; set; } = DateTime.Now;

        [BsonIgnore]
        public int RemainTime => (EndDate > DateTime.Now) ? (EndDate - DateTime.Now).Days : 0;

        [BsonElement("status")]
        public ContractStatus Status { get; set; }
    }
}
