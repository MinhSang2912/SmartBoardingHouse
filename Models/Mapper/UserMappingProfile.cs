using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<UserRequest, User>()
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src =>
                    src.DateOfbirth.HasValue
                        ? DateTime.SpecifyKind(src.DateOfbirth.Value.Date, DateTimeKind.Utc)
                        : (DateTime?)null
                ));
            CreateMap<User, UserResponse>();
        }
    }
}