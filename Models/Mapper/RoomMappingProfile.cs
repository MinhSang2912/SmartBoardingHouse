// Mappings/RoomMappingProfile.cs
using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class RoomMappingProfile : Profile
    {
        public RoomMappingProfile()
        {
            CreateMap<RoomRequest, Room>();
            CreateMap<RoomRequest, Room>();
            CreateMap<Room, RoomResponse>()
                .ForMember(dest => dest.StatusLabel, opt => opt.Ignore())
                .ForMember(dest => dest.TenantName, opt => opt.Ignore());
        }
    }
}