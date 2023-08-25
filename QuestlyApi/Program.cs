using IdentityServer4;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestlyApi.Data;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityServer()
    .AddInMemoryClients(configuration.GetSection("IdentityServer:Clients"))
    .AddInMemoryIdentityResources(configuration.GetSection("IdentityServer:IdentityResources"))
    .AddInMemoryApiResources(configuration.GetSection("IdentityServer:ApiResources"))
    .AddAspNetIdentity<Player>(); // Use real users from the database

// Add Authentication
builder.Services.AddAuthentication()
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.ClientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
    });

// Add ASP.NET Core Identity
builder.Services.AddIdentity<Player, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use Middleware
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();