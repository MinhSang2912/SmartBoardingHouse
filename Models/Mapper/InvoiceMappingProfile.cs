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
                .ForMember(dest => dest.ContractId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.RoomId, opt => opt.Ignore());

            CreateMap<Invoice, InvoiceResponse>()
                .ForMember(dest => dest.BillingPeriod, opt => opt.Ignore())
                .ForMember(dest => dest.ElectricTotal, opt => opt.Ignore())
                .ForMember(dest => dest.WaterTotal, opt => opt.Ignore())
                .ForMember(dest => dest.StatusLabel, opt => opt.Ignore());
        }
    }
}