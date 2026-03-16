using Application.DTOs.Auth;
using Application.DTOs.Auth;
using Application.DTOs.Merchants;
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

            // Merchant mappings
            CreateMap<Merchant, MerchantDto>()
                .ForMember(dest => dest.MunicipalityName, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.Name : null))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Municipality != null && src.Municipality.Department != null ? src.Municipality.Department.Name : null))
                .ForMember(dest => dest.EstablishmentCount, opt => opt.MapFrom(src => src.Establishments != null ? src.Establishments.Count : 0))
                .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src => src.Establishments != null ? src.Establishments.Sum(e => e.Revenue) : 0))
                .ForMember(dest => dest.TotalEmployees, opt => opt.MapFrom(src => src.Establishments != null ? src.Establishments.Sum(e => e.EmployeeCount) : 0));
        }
    }
}