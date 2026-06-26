using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartBoardingHouse.Models.Entity
{
    public class Payment : BaseModel
    {
        [BsonElement("tenant")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string TenantId { get; set; } = string.Empty;

        [BsonElement("invoice")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string InvoiceId { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("method")]
        public string Method { get; set; } = "qr";

        [BsonElement("status")]
        public string Status { get; set; } = "pending";

        [BsonElement("transactionId")]
        public string? TransactionId { get; set; }

        [BsonElement("qrData")]
        public string? QrData { get; set; }

        [BsonElement("paidAt")]
        public DateTime? PaidAt { get; set; }

        [BsonElement("note")]
        public string? Note { get; set; }
    }
}
