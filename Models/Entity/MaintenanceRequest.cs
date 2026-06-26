using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Models;
using static SmartBoardingHouse.Common.Enums;

public class MaintenanceRequest : BaseModel
{
    [BsonElement("requestNumber")]
    public string RequestNumber { get; set; } = string.Empty;

    [BsonElement("room")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoomId { get; set; } = string.Empty;

    [BsonElement("tenant")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("roomNumber")]
    public string RoomNumber { get; set; } = string.Empty;

    [BsonElement("tenantName")]
    public string TenantName { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("images")]
    public List<string> Images { get; set; } = new();

    [BsonElement("category")]
    public MaintenanceCategory Category { get; set; }

    [BsonElement("priority")]
    public PriotyRequest Priority { get; set; } = PriotyRequest.Low;

    [BsonElement("status")]
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("adminNote")]
    public string? AdminNote { get; set; }
}
