using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmartBoardingHouse.Models.Entity;

namespace SmartBoardingHouse.Models
{
    public class Floor : BaseModel
    {
        public string FloorNumber { get; set; } = string.Empty;
        public int RoomCount { get; set; }
    }

    public class FloorValidation : AbstractValidator<Floor>
    {
        public FloorValidation()
        {
            RuleFor(x => x.FloorNumber).NotEmpty().WithMessage("Floor number is required.");
            RuleFor(x => x.RoomCount).GreaterThanOrEqualTo(0).WithMessage("Room count must be greater than or equal to 0.");
        }
    }
}