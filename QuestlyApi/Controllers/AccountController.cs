using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace QuestlyApi.Controllers;

// API Controller for Authentication
[ApiController]
[Route("[controller]")]
[Authorize] // Protect all endpoints in this controller
public class AuthController : ControllerBase
{
    // Dependency injection for logger
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    // Endpoint for signing in with Google
    [HttpGet("signin")]
    [AllowAnonymous] // Allow anonymous access to this endpoint
    public async Task<IActionResult> SignIn()
    {
        try
        {
            // Challenge the user to login via Google
            await HttpContext.ChallengeAsync(
                GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") }
            );
            return Ok();
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the sign-in process
            _logger.LogError("An error occurred in SignIn: {ExMessage}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    // Endpoint for signing out
    [HttpGet("signout")]
    public new IActionResult SignOut()
    {
        // Sign the user out and redirect to home
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, GoogleDefaults.AuthenticationScheme);
    }

    // Endpoint for handling Google's response after authentication
    [HttpGet("GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        try
        {
            // Authenticate the user
            var authenticateInfo = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            // Check if authentication was successful
            if (authenticateInfo.Principal == null)
            {
                _logger.LogInformation("Could not authenticate user");
                return Unauthorized();
            }

            // Log successful authentication
            _logger.LogInformation("Successfully authenticated user");
            return Ok("Successfully authenticated user.");
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the Google response process
            _logger.LogError("An error occurred in GoogleResponse: {ExMessage}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }
}