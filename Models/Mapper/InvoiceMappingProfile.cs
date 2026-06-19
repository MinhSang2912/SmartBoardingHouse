using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class InvoiceMappingProfile : Profile
    {
        public InvoiceMappingProfile()
        {
            CreateMap<InvoiceRequest, Invoice>()
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src =>
                    src.RoomPrice +
                    (decimal)src.ElectricUsage * src.ElectricPrice +
                    (decimal)src.WaterUsage * src.WaterPrice +
                    src.ServiceFee));

            CreateMap<Invoice, InvoiceResponse>()
                .ForMember(dest => dest.BillingPeriod, opt => opt.Ignore())
                .ForMember(dest => dest.ElectricTotal, opt => opt.Ignore())
                .ForMember(dest => dest.WaterTotal, opt => opt.Ignore())
                .ForMember(dest => dest.StatusLabel, opt => opt.Ignore());
        }
    }
}