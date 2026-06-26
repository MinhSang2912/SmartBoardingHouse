using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Common;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
    public class Invoice : BaseModel
    {
        [BsonElement("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [BsonElement("room")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RoomId { get; set; } = string.Empty;

        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("contract")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ContractId { get; set; }

        // Cache hiển thị
        [BsonElement("roomNumber")]
        public string RoomNumber { get; set; } = string.Empty;

        [BsonElement("tenantName")]
        public string TenantName { get; set; } = string.Empty;

        [BsonElement("month")]
        public int BillingMonth { get; set; }

        [BsonElement("year")]
        public int BillingYear { get; set; }

        [BsonElement("roomPrice")]
        public decimal RoomPrice { get; set; }

        [BsonElement("electricUsage")]
        public double ElectricUsage { get; set; }

        [BsonElement("electricPrice")]
        public decimal ElectricPrice { get; set; }

        [BsonElement("waterUsage")]
        public double WaterUsage { get; set; }

        [BsonElement("waterPrice")]
        public decimal WaterPrice { get; set; }

        [BsonElement("serviceFee")]
        public decimal ServiceFee { get; set; }

        // Phụ phí phát sinh khác — khớp items[] bên Client (đã cộng vào Amount khi tính)
        [BsonElement("items")]
        public List<InvoiceItem> Items { get; set; } = new();

        [BsonElement("totalAmount")]
        public decimal Amount { get; set; }

        [BsonElement("paidAmount")]
        public decimal PaidAmount { get; set; } = 0m;

        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        [BsonElement("note")]
        public string? Note { get; set; }

        [BsonElement("status")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    }

    public class InvoiceItem
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; } = 1;

        [BsonElement("unitPrice")]
        public decimal UnitPrice { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }
    }
}
