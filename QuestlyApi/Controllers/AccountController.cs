using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuestlyApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("signin")]
    public async Task SignIn()
    {
        await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties()
        {
            RedirectUri = Url.Action("GoogleResponse")
        });
    }

    [HttpGet("signout")]
    public IActionResult SignOut()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, "Google");
    }

    [HttpGet("GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        _logger.LogInformation("Inside SignInGoogle method");

        // Retrieve the user info
        var authenticateInfo = await HttpContext.AuthenticateAsync("Google");
        if (authenticateInfo?.Principal == null)
        {
            _logger.LogInformation("Could not authenticate user.");
            return Unauthorized();
        }

        // Log user claims (like email, name, etc.)
        foreach (var claim in authenticateInfo.Principal.Claims)
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");

        return Ok("Successfully enticated user.");
    }
}