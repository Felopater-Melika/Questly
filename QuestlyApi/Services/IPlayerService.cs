using QuestlyApi.Entities;
using QuestlyApi.Models;

namespace QuestlyApi.Services;

public interface IPlayerService
{
    AuthenticateResponse RefreshToken(string token, string ipAddress);
    Player GetById(Guid id);
}