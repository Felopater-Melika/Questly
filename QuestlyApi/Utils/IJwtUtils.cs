using QuestlyApi.Entities;

namespace QuestlyApi.Utils;

public interface IJwtUtils
{
    public string GenerateJwtToken(Player player);
    public Guid? ValidateJwtToken(string token);
    public RefreshToken GenerateRefreshToken(string ipAddress);
}