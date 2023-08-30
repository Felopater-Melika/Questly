using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPersistedGrantStore _persistedGrantStore;
    private readonly IUserClaimsPrincipalFactory<Player> _claimsFactory;

    public AuthController(
        ILogger<AuthController> logger,
        IPlayerRepository playerRepository,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IPersistedGrantStore persistedGrantStore,
        IUserClaimsPrincipalFactory<Player> claimsFactory)
    {
        _logger = logger;
        _playerRepository = playerRepository;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _persistedGrantStore = persistedGrantStore;
        _claimsFactory = claimsFactory;
    }


    [HttpGet("signin")]
    [SwaggerOperation(Summary = "Initiates Google sign-in process",
        Description = "Redirects to Google login or returns 500 if an error occurs.")]
    public async Task<IActionResult> SignIn()
    {
        _logger.LogInformation("SignIn called");

        try
        {
            _logger.LogDebug("Initiating Google sign-in challenge");

            // Challenge the user to login via Google
            await HttpContext.ChallengeAsync(
                GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") }
            );

            _logger.LogDebug("Google sign-in challenge initiated successfully");

            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") },
                GoogleDefaults.AuthenticationScheme);
        }
        catch (AuthenticationException authEx)
        {
            // Log authentication-specific errors and return 401
            _logger.LogError("Authentication failed: {ExMessage}", authEx.Message);
            return Unauthorized("Authentication failed");
        }
        catch (InvalidOperationException invalidOpEx)
        {
            // Log invalid operation errors and return 400
            _logger.LogError("Invalid operation: {ExMessage}", invalidOpEx.Message);
            return BadRequest("Invalid request");
        }
        catch (Exception ex)
        {
            // Log any other errors and return 500
            _logger.LogError("An unexpected error occurred: {ExMessage}", ex.Message);
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
            _logger.LogDebug("Attempting to authenticate user via Google");

            // Authenticate the user
            var authenticateInfo = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            // Check if authentication was successful
            if (authenticateInfo.Principal == null)
            {
                _logger.LogWarning("Authentication failed: Principal is null");
                return Unauthorized();
            }

            _logger.LogDebug("User authenticated successfully");

            // Extract player information
            var email = authenticateInfo.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = authenticateInfo.Principal.FindFirstValue(ClaimTypes.GivenName);
            var lastName = authenticateInfo.Principal.FindFirstValue(ClaimTypes.Surname);

            _logger.LogDebug("Extracted player information: Email={Email}, FirstName={FirstName}, LastName={LastName}",
                email, firstName, lastName);

            // Check if the player already exists
            _logger.LogDebug("Checking if player already exists in database");
            var existingPlayer =
                await _playerRepository.GetByEmailAsync(email ??
                                                        throw new InvalidOperationException("Email not found"));

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
                _logger.LogInformation("New player added with email: {Email}", email);
            }
            else
            {
                _logger.LogDebug("Player already exists with email: {Email}", email);
            }

            // Log successful authentication
            _logger.LogInformation("Successfully authenticated user: {Email}", email);
            return Ok("Successfully authenticated");
        }
        catch (InvalidOperationException invalidOpEx)
        {
            // Log invalid operation errors and return 400
            _logger.LogError("Invalid operation: {ExMessage}", invalidOpEx.Message);
            return BadRequest("Invalid request");
        }
        catch (DbUpdateException dbEx)
        {
            // Log database update errors and return 500
            _logger.LogError("Database update failed: {ExMessage}", dbEx.Message);
            return StatusCode(500, "Database error");
        }
        catch (Exception ex)
        {
            // Log any other errors and return 500
            _logger.LogError("An unexpected error occurred: {ExMessage}", ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken(string refreshToken)
    {
        var persistedGrant = await _persistedGrantStore.GetAsync(refreshToken);
        if (persistedGrant == null)
        {
            _logger.LogWarning("Invalid refresh token");
            return BadRequest("Invalid refresh token");
        }

        // Get the user
        var user = await _playerRepository.GetByIdAsync(Guid.Parse(persistedGrant.SubjectId));
        if (user == null)
        {
            _logger.LogWarning("User not found");
            return BadRequest("User not found");
        }

        // Generate claims
        var principal = new ClaimsPrincipal(await _claimsFactory.CreateAsync(user));

        // Create new access token
        var tokenCreateRequest = new TokenCreationRequest
        {
            Subject = principal
        };
        var newAccessToken = await _tokenService.CreateAccessTokenAsync(tokenCreateRequest);
        var newTokenValue = await _tokenService.CreateSecurityTokenAsync(newAccessToken);

        // Create new refresh token
        var newRefreshToken = new PersistedGrant
        {
            Key = Guid.NewGuid().ToString(), // Generate a new unique key
            Type = "refresh_token",
            SubjectId = user.Id.ToString(),
            ClientId = persistedGrant.ClientId, // Reuse the ClientId from the old token
            CreationTime = DateTime.UtcNow,
            Expiration = DateTime.UtcNow.AddMinutes(60), // Set your desired expiration time
            Data = newAccessToken.ToString() // Serialize the new access token into the Data field
        };

        // Store the new refresh token
        await _persistedGrantStore.StoreAsync(newRefreshToken);

        // Remove old refresh token
        await _persistedGrantStore.RemoveAsync(refreshToken);

        return Ok(new
        {
            AccessToken = newTokenValue,
            RefreshToken = newRefreshToken.Key // Return the new refresh token key
        });
    }
}