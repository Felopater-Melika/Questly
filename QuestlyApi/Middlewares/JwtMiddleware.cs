using Microsoft.Extensions.Options;
using QuestlyApi.Configurations;
using QuestlyApi.Repositories;
using QuestlyApi.Utils;

namespace QuestlyApi.Middlewares;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(
        HttpContext context,
        IPlayerRepository playerRepository,
        IJwtUtils jwtUtils
    )
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        var userId = jwtUtils.ValidateJwtToken(token);
        if (userId != null)
            // attach user to context on successful jwt validation
            context.Items["User"] = playerRepository.GetByIdAsync(userId.Value);

        await _next(context);
    }
}