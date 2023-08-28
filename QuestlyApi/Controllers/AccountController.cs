using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestlyApi.Entities;
using QuestlyApi.Repositories;
using Swashbuckle.AspNetCore.Annotations;


namespace QuestlyApi.Controllers;

// API Controller for Authentication
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    // Dependency injection for logger
    private readonly ILogger<AuthController> _logger;
    private readonly IPlayerRepository _playerRepository;

    public AuthController(ILogger<AuthController> logger, IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
        _logger = logger;
    }


    [HttpGet("signin")]
    [SwaggerOperation(Summary = "Initiates Google sign-in process",
        Description = "Redirects to Google login or returns 500 if an error occurs.")]
    public async Task<IActionResult> SignIn()
    {
        try
        {
            // Challenge the user to login via Google
            await HttpContext.ChallengeAsync(
                GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") }
            );
            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") },
                GoogleDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the sign-in process
            _logger.LogError("An error occurred in SignIn: {ExMessage}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("signout")]
    [Authorize]
    [SwaggerOperation(Summary = "Signs the user out and redirects to home")]
    public new IActionResult SignOut()
    {
        // Sign the user out and redirect to home
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("GoogleResponse")]
    [SwaggerOperation(Summary = "Handles Google's response after authentication",
        Description = "Returns 200 OK if successful, 401 if unauthorized, 500 if an error occurs.")]
    public async Task<IActionResult> GoogleResponse()
    {
        _logger.LogInformation("GoogleResponse called");
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

            // Extract player information
            var email = authenticateInfo.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = authenticateInfo.Principal.FindFirstValue(ClaimTypes.GivenName);
            var lastName = authenticateInfo.Principal.FindFirstValue(ClaimTypes.Surname);

            // Check if the player already exists
            var existingPlayer =
                await _playerRepository.GetByEmailAsync(email); // Assuming you have a method to get a player by email

            if (existingPlayer == null)
            {
                // Create a new player
                var player = new Player()
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName
                };

                // Add the player to the database
                await _playerRepository.AddAsync(player);
                _logger.LogInformation("New player added");
            }
            else
            {
                _logger.LogInformation("Player already exists");
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