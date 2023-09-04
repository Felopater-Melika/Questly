using AutoMapper;
using QuestlyApi.Entities;

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