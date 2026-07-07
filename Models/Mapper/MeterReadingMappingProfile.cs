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
            CreateMap<MeterReadingRequest, MeterReading>()
                .ForMember(dest => dest.CurrentIndex, opt => opt.MapFrom(src => src.CurrentIndex))
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.Year, opt => opt.Ignore())
                .ForMember(dest => dest.PreviousIndex, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.PhotoUrl, opt => opt.Ignore());

            CreateMap<MeterReading, MeterReadingResponse>()
                .ForMember(dest => dest.TenantName, opt => opt.Ignore())
                .ForMember(dest => dest.TypeLabel, opt => opt.Ignore())
                .ForMember(dest => dest.Period, opt => opt.Ignore())
                .ForMember(dest => dest.UsageLabel, opt => opt.Ignore())
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Total, opt => opt.Ignore());
        }
    }
}