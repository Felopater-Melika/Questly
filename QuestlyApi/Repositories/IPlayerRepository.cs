using QuestlyApi.Entities;

namespace QuestlyApi.Repositories;

public interface IPlayerRepository
{
    Task<Player> GetByIdAsync(Guid id);
    Task<Player> GetByEmailAsync(string email);

    Task<IEnumerable<Player>> GetAllAsync();
    Task AddAsync(Player player);
    Task UpdateAsync(Player player);
    Task DeleteAsync(Guid id);
}