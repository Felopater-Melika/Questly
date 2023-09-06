using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestlyApi.Configurations;
using QuestlyApi.Data;
using QuestlyApi.Entities;
using QuestlyApi.Models;
using QuestlyApi.Utils;

namespace QuestlyApi.Services;

public class PlayerService : IPlayerService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtUtils _jwtUtils;
    private readonly AppSettings _appSettings;

    public PlayerService(
        ApplicationDbContext context,
        IJwtUtils jwtUtils,
        IOptions<AppSettings> appSettings
    )
    {
        _context = context;
        _jwtUtils = jwtUtils;
        _appSettings = appSettings.Value;
    }

    public AuthenticateResponse RefreshToken(string token, string ipAddress)
    {
        Console.WriteLine(token);
        var player = GetUserByRefreshToken(token);
        var refreshToken = player.RefreshTokens.Single(x => x.Token == token);

        // if (refreshToken.IsRevoked)
        // {
        //     // revoke all descendant tokens in case this token has been compromised
        //     RevokeDescendantRefreshTokens(
        //         refreshToken,
        //         player,
        //         ipAddress,
        //         $"Attempted reuse of revoked ancestor token: {token}"
        //     );
        //     _context.Update(player);
        //     _context.SaveChanges();
        // }
        //
        // if (!refreshToken.IsActive)
        //     throw new Exception("Invalid token");

        // replace old refresh token with a new one (rotate token)
        var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
        player.RefreshTokens.Add(newRefreshToken);

        // remove old refresh tokens from user
        RemoveOldRefreshTokens(player);

        // save changes to db
        _context.Update(player);
        _context.SaveChanges();

        // generate new jwt
        var jwtToken = _jwtUtils.GenerateJwtToken(player);

        return new AuthenticateResponse(player, jwtToken, newRefreshToken.Token);
    }

    public Player GetById(Guid id)
    {
        var player = _context.Players.Find(id);
        if (player == null)
            throw new KeyNotFoundException("User not found");
        return player;
    }

    private RefreshToken RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
    {
        var newRefreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
        RevokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
        return newRefreshToken;
    }

    private static void RevokeRefreshToken(
        RefreshToken token,
        string ipAddress,
        string reason = null,
        string replacedByToken = null
    )
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
        token.ReplacedByToken = replacedByToken;
    }

    private void RemoveOldRefreshTokens(Player player)
    {
        // remove old inactive refresh tokens from user based on TTL in app settings
        player.RefreshTokens.RemoveAll(
            x => !x.IsActive && x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow
        );
    }

    private void RevokeDescendantRefreshTokens(
        RefreshToken refreshToken,
        Player player,
        string ipAddress,
        string reason
    )
    {
        // recursively traverse the refresh token chain and ensure all descendants are revoked
        if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
        {
            var childToken = player.RefreshTokens.SingleOrDefault(
                x => x.Token == refreshToken.ReplacedByToken
            );
            if (childToken.IsActive)
                RevokeRefreshToken(childToken, ipAddress, reason);
            else
                RevokeDescendantRefreshTokens(childToken, player, ipAddress, reason);
        }
    }

    private Player GetUserByRefreshToken(string token)
    {
        Console.WriteLine("THE FUCKING TOKEN IS: " + token);
        var player = _context.Players.SingleOrDefault(
            u => u.RefreshTokens.Any(t => t.Token == token)
        );

        if (player == null)
            throw new Exception("Invalid token");

        return player;
    }
}