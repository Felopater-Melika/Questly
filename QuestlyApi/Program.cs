using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestlyApi.Data;
using QuestlyApi.Entities;
using QuestlyApi.Repositories;
using QuestlyApi.Validators;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// Initialize the web application builder
var builder = WebApplication.CreateBuilder(args);

// Configure Logger
ConfigureLogger(builder);

// Configure Database
ConfigureDatabase(builder);

// Configure Identity
ConfigureIdentity(builder);

// Configure Authentication
ConfigureAuthentication(builder);

// Configure Additional Services
ConfigureServices(builder);

// Build the application
var app = builder.Build();

// Configure Middleware
ConfigureMiddleware(app);

app.Run();

// Logger Configuration
void ConfigureLogger(WebApplicationBuilder loggerBuilder)
{
    var serilogConfig = new LoggerConfiguration().MinimumLevel
        .Information()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code
        )
        .Enrich.FromLogContext()
        .CreateLogger();

    Log.Logger = serilogConfig;

    loggerBuilder.Host.UseSerilog();
}

// Database Configuration
void ConfigureDatabase(WebApplicationBuilder databaseBuilder)
{
    // Setup Entity Framework with PostgreSQL
    databaseBuilder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(
            databaseBuilder.Configuration.GetConnectionString("DefaultConnection"),
            o => o.UseNodaTime() // Enable NodaTime support
        );
    });
}

// Identity Configuration
void ConfigureIdentity(WebApplicationBuilder identityBuilder)
{
    // Setup Identity for user management
    identityBuilder.Services
        .AddIdentity<Player, IdentityRole>() // Use Player entity instead of IdentityUser
        .AddEntityFrameworkStores<ApplicationDbContext>() // Use ApplicationDbContext for storing user data
        .AddDefaultTokenProviders(); // Add default token providers for things like email confirmation, password reset, etc.
}

// Authentication Configuration
void ConfigureAuthentication(WebApplicationBuilder authenticationBuilder)
{
    // Retrieve Google authentication settings from appsettings.json
    var clientId = authenticationBuilder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = authenticationBuilder.Configuration["Authentication:Google:ClientSecret"];

    // Validate that Google authentication settings are present
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        throw new InvalidOperationException("Google authentication settings are missing.");

    // Configure authentication services
    var jwtSettings = authenticationBuilder.Configuration.GetSection("JwtSettings");
    authenticationBuilder.Services
        .AddAuthentication(options =>
        {
            // Set the default scheme to cookies
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            // Set the challenge scheme to Google
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie() // Add cookie-based authentication
        .AddGoogle(
            "Google",
            options =>
            {
                // Set the sign-in scheme to cookies
                options.SignInScheme = IdentityConstants.ApplicationScheme;
                // Set the Google API client ID and secret
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            }
        )
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        jwtSettings["Key"] ?? throw new InvalidOperationException()
                    )
                )
            };
        });
}

// Additional Services Configuration
void ConfigureServices(WebApplicationBuilder servicesBuilder)
{
    //Add Player Repository
    servicesBuilder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
    // Add controllers for MVC
    servicesBuilder.Services.AddControllers();
    // Add API explorer for generating Swagger documentation
    servicesBuilder.Services.AddEndpointsApiExplorer();
    // Add Swagger generator
    servicesBuilder.Services.AddSwaggerGen();
    // Add AutoMapper
    servicesBuilder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    // Add FluentValidation
    servicesBuilder.Services
        .AddFluentValidationAutoValidation()
        .AddFluentValidationClientsideAdapters()
        .AddValidatorsFromAssemblyContaining<LoginDtoValidator>()
        .AddValidatorsFromAssemblyContaining<SignUpDtoValidator>();
}

// Middleware Configuration
void ConfigureMiddleware(WebApplication application)
{
    // Check if the application is in development mode
    if (application.Environment.IsDevelopment())
    {
        // Use developer-friendly exception page
        application.UseDeveloperExceptionPage();
        // Enable Swagger UI
        application.UseSwagger();
        application.UseSwaggerUI();
    }

    // Use Serilog for logging HTTP requests
    application.UseSerilogRequestLogging();
    // Use custom error handling
    application.UseExceptionHandler("/error");
    // Redirect HTTP to HTTPS
    application.UseHttpsRedirection();
    // Use authentication middleware
    application.UseAuthentication();
    // Use authorization middleware
    application.UseAuthorization();
    // Map controller routes
    application.MapControllers();
}