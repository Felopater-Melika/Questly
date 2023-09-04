using Microsoft.EntityFrameworkCore;
using QuestlyApi.Data;
using QuestlyApi.Entities;

namespace QuestlyApi.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _context;

    public PlayerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Player> GetByIdAsync(Guid id)
    {
        return await _context.Players.FindAsync(id) ?? throw new InvalidOperationException();
    }

    public async Task<Player?> GetByEmailAsync(string email)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<IEnumerable<Player>> GetAllAsync()
    {
        return await _context.Players.ToListAsync();
    }

    public async Task AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var player = await GetByIdAsync(id);
        _context.Players.Remove(player);
        await _context.SaveChangesAsync();
    }
}