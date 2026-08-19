using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Mappings
{
    public class ItemFeeMappingProfile : Profile
    {
        public ItemFeeMappingProfile()
        {
            CreateMap<ItemFeeRequest, ItemFee>();
            CreateMap<ItemFee, ItemFeeResponse>();
        }
    }
}
