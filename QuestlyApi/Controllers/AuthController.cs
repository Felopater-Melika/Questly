using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestlyApi.Dtos;
using QuestlyApi.Entities;
using QuestlyApi.Repositories;
using Swashbuckle.AspNetCore.Annotations;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;


namespace QuestlyApi.Controllers;

// API Controller for Authentication
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    // Dependency injection for logger
    private readonly ILogger<AuthController> _logger;
    private readonly IPlayerRepository _playerRepository;
    private readonly IConfiguration _configuration;


    public AuthController(
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IPlayerRepository playerRepository
    )
    {
        _configuration = configuration;
        _logger = logger;
        _playerRepository = playerRepository;
    }


    [HttpGet("signin")]
    [SwaggerOperation(Summary = "Initiates Google sign-in process",
        Description = "Redirects to Google's sign-in page and returns 401 if unauthorized, 500 if an error occurs.")]
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
            var jwtSettings = _configuration.GetSection("JwtSettings");

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

            // Create JWT token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Generate JWT token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                jwtSettings["Issuer"],
                jwtSettings["Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Log successful authentication
            _logger.LogInformation("Successfully authenticated user: {Email}", email);
            return Ok(new { Token = tokenString });
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

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Logs the user in using JWT",
        Description = "Returns 200 OK if successful, 401 if unauthorized, 500 if an error occurs.")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            // Log the login attempt
            _logger.LogInformation("Login attempt for {Email}", dto.Email);

            // Fetch JWT settings from configuration
            var jwtSettings = _configuration.GetSection("JwtSettings");

            // Retrieve the user by email
            var user = await _playerRepository.GetByEmailAsync(dto.Email);

            // Check if the email exists in the database
            if (user == null)
            {
                _logger.LogWarning("Invalid email for {Email}", dto.Email);
                return Unauthorized("Invalid email or password");
            }

            // Initialize password hasher and verify the provided password
            var passwordHasher = new PasswordHasher<Player>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            // Check if the password is valid
            if (verificationResult != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Invalid password for {Email}", dto.Email);
                return Unauthorized("Invalid email or password");
            }

            // Define the claims for the JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
                // Add more claims as needed
            };

            // Initialize JWT signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Generate the JWT token
            var token = new JwtSecurityToken(
                jwtSettings["Issuer"],
                jwtSettings["Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            // Return the generated token
            return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        catch (InvalidOperationException invalidOpEx)
        {
            // Log and handle invalid operations
            _logger.LogError(invalidOpEx, "Invalid operation for {Email}", dto.Email);
            return BadRequest("Invalid request");
        }
        catch (DbUpdateException dbEx)
        {
            // Log and handle database errors
            _logger.LogError(dbEx, "Database update failed for {Email}", dto.Email);
            return StatusCode(500, "Database error");
        }
        catch (Exception ex)
        {
            // Log and handle any other exceptions
            _logger.LogError(ex, "An unexpected error occurred for {Email}", dto.Email);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpPost("signup")]
    [SwaggerOperation(Summary = "Registers a new user",
        Description = "Returns 201 Created if successful, 400 if validation fails, 500 if an error occurs.")]
    public async Task<IActionResult> SignUp(SignUpDto dto)
    {
        try
        {
            _logger.LogInformation("SignUp attempt for {Email}", dto.Email);


            // Check if a user with the same email already exists
            var existingUser = await _playerRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email already in use for {Email}", dto.Email);
                return BadRequest("Email already in use");
            }

            // Hash the password
            var passwordHasher = new PasswordHasher<Player>();
            var hashedPassword = passwordHasher.HashPassword(null, dto.Password);

            // Create a new Player entity and set its properties
            var newPlayer = new Player
            {
                Email = dto.Email,
                PasswordHash = hashedPassword
            };

            // Add the new player to the database
            await _playerRepository.AddAsync(newPlayer);

            // Log successful registration
            _logger.LogInformation("Successfully registered user: {Email}", dto.Email);

            // Return a 201 Created response
            return CreatedAtAction(nameof(SignUp), new { id = newPlayer.Id }, newPlayer);
        }
        catch (InvalidOperationException invalidOpEx)
        {
            _logger.LogError(invalidOpEx, "Invalid operation for {Email}", dto.Email);
            return BadRequest("Invalid request");
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update failed for {Email}", dto.Email);
            return StatusCode(500, "Database error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred for {Email}", dto.Email);
            return StatusCode(500, "Internal server error");
        }
    }
}