using AutoMapper;
using LoopGame.Application.Dtos.AuthServiceDtos;
using LoopGame.Infrastructure.Identity;

namespace LoopGame.Application.Mappers
{
    public class AuthMapperProfile : Profile
    {
        public AuthMapperProfile()
        {
            CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.FullName));

            CreateMap<ApplicationUser, UserToReturnDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

            CreateMap<RegisterDto, LoopGame.Domain.Entities.Player.Player>();
        }
    }
}
