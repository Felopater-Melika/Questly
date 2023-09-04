using AutoMapper;
using QuestlyApi.Dtos;
using QuestlyApi.Entities;

namespace QuestlyApi.Profiles;

public class PlayerProfile : Profile
{
    public PlayerProfile()
    {
        CreateMap<SignUpDto, Player>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));

        CreateMap<LoginDto, Player>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));
    }
}