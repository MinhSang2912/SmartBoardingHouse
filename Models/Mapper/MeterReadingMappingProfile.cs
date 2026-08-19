using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class MeterReadingMappingProfile : Profile
    {
        public MeterReadingMappingProfile()
        {
            CreateMap<MeterReadingRequest, MeterReading>();
            CreateMap<MeterReading, MeterReadingResponse>()
                .ForMember(dest => dest.TenantName, opt => opt.Ignore())
                .ForMember(dest => dest.Period, opt => opt.Ignore())
                .ForMember(dest => dest.PreviousElectricityIndex, opt => opt.Ignore())
                .ForMember(dest => dest.ElectricityUsage, opt => opt.Ignore())
                .ForMember(dest => dest.ElectricityTotal, opt => opt.Ignore())
                .ForMember(dest => dest.PreviousWaterIndex, opt => opt.Ignore())
                .ForMember(dest => dest.WaterUsage, opt => opt.Ignore())
                .ForMember(dest => dest.WaterTotal, opt => opt.Ignore());
        }
    }
}