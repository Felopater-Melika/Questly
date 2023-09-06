using System.Text.Json.Serialization;
using QuestlyApi.Entities;

namespace QuestlyApi.Models;

public class AuthenticateResponse
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string JwtToken { get; set; }

    [JsonIgnore] // refresh token is returned in http only cookie
    public string RefreshToken { get; set; }

    public AuthenticateResponse(Player player, string jwtToken, string refreshToken)
    {
        Id = player.Id;
        FirstName = player.FirstName;
        LastName = player.LastName;
        JwtToken = jwtToken;
        RefreshToken = refreshToken;
    }
}