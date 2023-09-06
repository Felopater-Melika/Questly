using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using NodaTime;

namespace QuestlyApi.Entities;

public class Player : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public LocalDate DateOfBirth { get; set; }

    [JsonIgnore] public List<RefreshToken> RefreshTokens { get; set; }
}