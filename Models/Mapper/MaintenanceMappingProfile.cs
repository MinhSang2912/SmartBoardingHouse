using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class MaintenanceMappingProfile : Profile
    {
        public MaintenanceMappingProfile()
        {
            CreateMap<MaintenanceRequestRequest, MaintenanceRequest>();
            CreateMap<MaintenanceRequest, MaintenanceRequestResponse>()
                .ForMember(dest => dest.PriorityLabel, opt => opt.Ignore())
                .ForMember(dest => dest.StatusLabel, opt => opt.Ignore());
        }
    }
}