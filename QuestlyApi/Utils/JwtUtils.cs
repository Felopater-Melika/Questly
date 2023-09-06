using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuestlyApi.Configurations;
using QuestlyApi.Data;
using QuestlyApi.Entities;

namespace QuestlyApi.Utils;

public class JwtUtils : IJwtUtils
{
    private readonly ApplicationDbContext _context;
    private readonly AppSettings _appSettings;
    private readonly IConfiguration _configuration;

    public JwtUtils(
        ApplicationDbContext context,
        IOptions<AppSettings> appSettings,
        IConfiguration configuration
    )
    {
        _context = context;
        _appSettings = appSettings.Value;
        _configuration = configuration;
    }

    public string GenerateJwtToken(Player player)
    {
        // generate token that is valid for 15 minutes
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretString = _configuration.GetValue<string>("JwtSettings:Key"); // Read as string
        var secret = Encoding.ASCII.GetBytes(secretString); // Convert to byte array
        Console.WriteLine("------> " + secretString + " <------");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[] { new Claim("email", player.Email), new Claim("playerId", player.Id) }
            ),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secret),
                SecurityAlgorithms.HmacSha256Signature
            )
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public Guid? ValidateJwtToken(string token)
    {
        if (token == null)
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var secretString = _configuration.GetValue<string>("JwtSettings:Key"); // Read as string
        var secret = Encoding.ASCII.GetBytes(secretString); // Convert to byte array
        try
        {
            tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secret),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                },
                out var validatedToken
            );

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == "playerId").Value);

            // return user id from JWT token if validation successful
            return userId;
        }
        catch
        {
            // return null if validation fails
            return null;
        }
    }

    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = GetUniqueToken(),
            // token is valid for 7 days
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        return refreshToken;

        string GetUniqueToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            // ensure token is unique by checking against db
            var tokenIsUnique = !_context.Players.Any(
                u => u.RefreshTokens.Any(t => t.Token == token)
            );

            if (!tokenIsUnique)
                return GetUniqueToken();

            return token;
        }
    }
}