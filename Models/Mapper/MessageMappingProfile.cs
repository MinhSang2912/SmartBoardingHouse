using AutoMapper;
using SmartBoardingHouse.Models.Entity;
using SmartBoardingHouse.Models.Request;
using SmartBoardingHouse.Models.Response;

namespace SmartBoardingHouse.Models.Mapper
{
    public class MessageMappingProfile : Profile
    {
        public MessageMappingProfile()
        {
            CreateMap<Message, MessageResponse>();
               
            CreateMap<SendMessageRequest, Message>()
                    .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
                    .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => (DateTime?)null));
        }
    }
}
