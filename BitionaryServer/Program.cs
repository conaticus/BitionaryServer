using System.Security.Claims;
using System.Text;
using BitionaryServer.Auth.Github;
using BitionaryServer.Configuration;
using BitionaryServer.Data;
using BitionaryServer.DTO;
using BitionaryServer.Middleware;
using BitionaryServer.Models;
using BitionaryServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var frontendSettings = builder.Configuration.GetSection("Frontend").Get<FrontendSettings>();
if (frontendSettings == null)
    throw new InvalidConfigurationException("Frontend required in configuration");

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
{
    throw new InvalidConfigurationException("JwtSettings required in configuration");
}

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(bearerOptions =>
    {
        bearerOptions.TokenValidationParameters = new TokenValidationParameters()
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidateIssuer = true,
            ValidateAudience = true
        };
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddGitHub("GitHub", options =>
    {
        var githubAuthSettings = builder.Configuration.GetSection("GithubAuth").Get<GithubAuthSettings>();
        if (githubAuthSettings == null)
        {
            throw new InvalidConfigurationException("GithubAuth required in configuration");
        }

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClientId = githubAuthSettings.ClientId;
        options.ClientSecret = githubAuthSettings.ClientSecret;
        options.Scope.Add("user:email");
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");

        options.Events.OnCreatingTicket = GithubTicketHandler.OnCreatingTicket;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(frontendSettings.BaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserDto>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();