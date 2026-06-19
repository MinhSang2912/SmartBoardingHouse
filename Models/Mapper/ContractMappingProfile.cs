using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class ContractMappingProfile : Profile
    {
        public ContractMappingProfile()
        {
            CreateMap<ContractRequest, Contract>();
            CreateMap<Contract, ContractResponse>()
                .ForMember(dest => dest.StatusLabel, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentDateLabel, opt => opt.Ignore());
        }
    }
}