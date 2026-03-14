using Application.DTOs.Auth;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;

namespace Infrastructure.Mapping
{
    /// <summary>
    /// AutoMapper profile for mapping between domain entities, DTOs, and persistence entities.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, UserEntity>().ReverseMap();
            CreateMap<User, UserInfoDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.GetFullName()));

            // RefreshToken mappings
            CreateMap<RefreshToken, RefreshTokenEntity>().ReverseMap();

            // PasswordResetToken mappings
            CreateMap<PasswordResetToken, PasswordResetTokenEntity>().ReverseMap();
        }
    }
}
