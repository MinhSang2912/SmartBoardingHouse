using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using CommonMessage = SmartBoardingHouse.Common.Message;
using static SmartBoardingHouse.Common.Enums;

namespace SmartBoardingHouse.Models.Entity
{
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

    public class ContractValidation : AbstractValidator<Contract>
    {
        public ContractValidation()
        {
            RuleFor(x => x.ContractNumber)
                .NotEmpty().WithMessage(CommonMessage.ContractNumberIsRequired());

            RuleFor(x => x.RoomNumber)
                .NotEmpty().WithMessage(CommonMessage.ContractRoomNumberIsRequired());

            RuleFor(x => x.TenantName)
                .NotEmpty().WithMessage(CommonMessage.ContractTenantNameIsRequired());

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage(CommonMessage.ContractStartDateIsRequired())
                .LessThan(x => x.EndDate).WithMessage(CommonMessage.ContractStartDateMustBeBeforeEndDate());

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage(CommonMessage.ContractEndDateIsRequired())
                .GreaterThan(x => x.StartDate).WithMessage(CommonMessage.ContractEndDateMustBeAfterStartDate());

            RuleFor(x => x.PaymentDate)
                .InclusiveBetween(1, 31).WithMessage(CommonMessage.ContractPaymentDateIsInvalid());

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage(CommonMessage.ContractStatusIsInvalid());
        }
    }
}
