using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;

namespace SmartBoardingHouse.Mappings
{
    public class FloorMappingProfile : Profile
    {
        public FloorMappingProfile()
        {
            CreateMap<FloorRequest, Floor>();
        }
    }
}