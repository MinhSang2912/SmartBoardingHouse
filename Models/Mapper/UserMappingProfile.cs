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
                .ForMember(dest => dest.Id, opt => opt.Ignore())         
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src =>
                    src.DateOfBirth.HasValue
                        ? DateTime.SpecifyKind(src.DateOfBirth.Value.Date, DateTimeKind.Utc)
                        : (DateTime?)null
                ));
            CreateMap<User, UserResponse>();
        }
    }
}