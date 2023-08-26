using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestlyApi.Data;
using Serilog;

// Initialize the web application builder and application
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Configure Logger
ConfigureLogger(builder);

// Configure Database
ConfigureDatabase(builder);

// Configure Identity
ConfigureIdentity(builder);

// Configure IdentityServer
ConfigureIdentityServer(builder);

// Configure Authentication
ConfigureAuthentication(builder);

// Configure Additional Services
ConfigureServices(builder);

// Configure Middleware
ConfigureMiddleware(app);

app.Run();

// Logger Configuration
void ConfigureLogger(WebApplicationBuilder loggerBuilder)
{
    var serilogConfig = new LoggerConfiguration()
        .ReadFrom.Configuration(loggerBuilder.Configuration) // Read from appsettings.json
        .CreateLogger();

    Log.Logger = serilogConfig;

    loggerBuilder.Host.UseSerilog();
}

// Database Configuration
void ConfigureDatabase(WebApplicationBuilder databaseBuilder)
{
    // Setup Entity Framework with PostgreSQL
    databaseBuilder.Services.AddDbContext<ApplicationDbContext>(
        options => options.UseNpgsql(databaseBuilder.Configuration.GetConnectionString("DefaultConnection"))
    );
}

// Identity Configuration
void ConfigureIdentity(WebApplicationBuilder identityBuilder)
{
    // Setup Identity for user management
    identityBuilder.Services
        .AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
}

// IdentityServer Configuration
void ConfigureIdentityServer(WebApplicationBuilder identityServerBuilder)
{
    // Setup IdentityServer4 for OAuth2 and OpenID Connect
    identityServerBuilder.Services
        .AddIdentityServer()
        .AddInMemoryClients(
            identityServerBuilder.Configuration.GetSection("IdentityServer:Clients").Get<List<Client>>())
        .AddInMemoryIdentityResources(
            identityServerBuilder.Configuration.GetSection("IdentityServer:IdentityResources")
                .Get<List<IdentityResource>>()
        )
        .AddInMemoryApiResources(
            identityServerBuilder.Configuration.GetSection("IdentityServer:ApiResources").Get<List<ApiResource>>()
        )
        .AddAspNetIdentity<IdentityUser>();
}

// Authentication Configuration
void ConfigureAuthentication(WebApplicationBuilder authenticationBuilder)
{
    // Setup Google authentication
    var clientId = authenticationBuilder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = authenticationBuilder.Configuration["Authentication:Google:ClientSecret"];

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        throw new InvalidOperationException("Google authentication settings are missing.");

    authenticationBuilder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddGoogle(
            "Google",
            options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
            }
        );
}

// Additional Services Configuration
void ConfigureServices(WebApplicationBuilder servicesBuilder)
{
    // Setup controllers and Swagger for API documentation
    servicesBuilder.Services.AddControllers();
    servicesBuilder.Services.AddEndpointsApiExplorer();
    servicesBuilder.Services.AddSwaggerGen();
}

// Middleware Configuration
void ConfigureMiddleware(WebApplication application)
{
    // Setup middleware for development and production environments
    if (application.Environment.IsDevelopment())
    {
        application.UseDeveloperExceptionPage();
        application.UseSwagger();
        application.UseSwaggerUI();
    }

    application.UseSerilogRequestLogging();
    application.UseExceptionHandler("/error");
    application.UseHttpsRedirection();
    application.UseIdentityServer();
    application.UseAuthentication();
    application.UseAuthorization();
    application.MapControllers();
}