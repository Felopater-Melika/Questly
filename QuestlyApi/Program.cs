using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestlyApi.Data;
using QuestlyApi.Entities;
using QuestlyApi.Repositories;
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

// Update IdentityServer Configuration
ConfigureIdentityServer(builder);

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
    var serilogConfig = new LoggerConfiguration()
        .MinimumLevel.Debug()
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
    databaseBuilder.Services.AddDbContext<ApplicationDbContext>(
        options =>
        {
            options.UseNpgsql(
                databaseBuilder.Configuration.GetConnectionString("DefaultConnection"),
                o => o.UseNodaTime() // Enable NodaTime support
            );
        }
    );
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

// IdentityServer Configuration
void ConfigureIdentityServer(WebApplicationBuilder identityServerBuilder)
{
    // Load client configurations from appsettings.json into a List<Client>
    var clients = identityServerBuilder.Configuration
        .GetSection("IdentityServer:Clients").Get<List<Client>>();

    // Load identity resources like scopes from appsettings.json into a List<IdentityResource>
    var identityResources = identityServerBuilder.Configuration
        .GetSection("IdentityServer:IdentityResources").Get<List<IdentityResource>>();

    // Load API resources from appsettings.json into a List<ApiResource>
    var apiResources = identityServerBuilder.Configuration
        .GetSection("IdentityServer:ApiResources").Get<List<ApiResource>>();

    // Check if any of the configurations are null and log a message if they are
    if (clients == null || identityResources == null || apiResources == null)
        Console.WriteLine("IdentityServer configuration is missing");

    // Initialize IdentityServer and configure it
    identityServerBuilder.Services
        .AddIdentityServer() // Initialize Duende IdentityServer
        .AddInMemoryClients(clients!) // Add the client configurations
        .AddInMemoryIdentityResources(identityResources!) // Add the identity resources
        .AddInMemoryApiResources(apiResources!) // Add the API resources
        .AddAspNetIdentity<
            Player>() // Integrate ASP.NET Core Identity with Duende IdentityServer using the Player entity
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = builder => builder.UseNpgsql(
                identityServerBuilder.Configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("QuestlyApi")); // Replace with your assembly name
        });
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
    authenticationBuilder.Services.AddAuthentication(options =>
        {
            // Set the default scheme to cookies
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // Set the challenge scheme to Google
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie() // Add cookie-based authentication
        .AddGoogle(
            "Google",
            options =>
            {
                // Set the sign-in scheme to cookies
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // Set the Google API client ID and secret
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            }
        );
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
    // Use IdentityServer for OAuth2 and OpenID Connect
    application.UseIdentityServer();
    // Use authentication middleware
    application.UseAuthentication();
    // Use authorization middleware
    application.UseAuthorization();
    // Map controller routes
    application.MapControllers();
}