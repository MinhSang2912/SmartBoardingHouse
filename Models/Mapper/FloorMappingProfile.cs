using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class FloorMappingProfile : Profile
    {
        public FloorMappingProfile()
        {
            CreateMap<FloorRequest, Floor>();
            CreateMap<Floor, FloorItemResponse>();
        }
    }
}