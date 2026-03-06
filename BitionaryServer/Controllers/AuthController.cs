using System.Security.Claims;
using BitionaryServer.Configuration;
using BitionaryServer.DTO;
using BitionaryServer.Exceptions;
using BitionaryServer.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BitionaryServer.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RegisterUserDto> _validator;
    private readonly string _frontendBaseUrl;
    private readonly CookieOptions _refreshCookieOptions;

    public AuthController(
        UserService userService,
        AuthService authService,
        JwtService jwtService,
        IOptions<JwtSettings> jwtSettings,
        IValidator<RegisterUserDto> validator,
        IOptions<FrontendSettings> frontendSettings,
        IWebHostEnvironment env
    )
    {
        _userService = userService;
        _authService = authService;
        _jwtService = jwtService;
        _validator = validator;
        _jwtSettings = jwtSettings.Value;
        _frontendBaseUrl = frontendSettings.Value.BaseUrl;
        
        _refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpiryDays)
        };
    }

    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/auth/github/callback"
        };
        
        return Challenge(properties, "GitHub");
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
            return Unauthorized();
        
        var githubId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var username = result.Principal.FindFirstValue(ClaimTypes.Name)!;
        var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? $"{githubId}@github.local";

        try
        {
            var user = await _authService.GetOrCreateGithubUserAsync(githubId, username, email);

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken(user.Id, user.Email);

            Response.Cookies.Append("refreshToken", refreshToken, _refreshCookieOptions);
            return Redirect($"{_frontendBaseUrl}/oauth-callback?accessToken={accessToken}");
        }
        catch (AccountLinkRequiredException)
        {
            return Redirect($"{_frontendBaseUrl}/login?linkRequired=true");
        }
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        var result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            return BadRequest(result.Errors);
        }
        
        var user = await _authService.RegisterAsync(dto);
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, user.Email);
        
        Response.Cookies.Append("refreshToken", refreshToken, _refreshCookieOptions);
        return Ok(new { accessToken });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto dto)
    {
        var user = await _authService.ValidateUserAsync(dto);
        if (user == null)
            return Unauthorized();
        
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, user.Email);
        
        Response.Cookies.Append("refreshToken", refreshToken, _refreshCookieOptions);
        return Ok(new { accessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            return Unauthorized(new { message = "Missing refresh token" });
        
        var principal = await _jwtService.ValidateToken(refreshToken);
        if (principal == null)
            return Unauthorized(new { message = "Invalid refresh token" });

        var userId = new Guid(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)!;
        
        var newAccessToken = _jwtService.GenerateAccessToken(userId, email);
        var newRefreshToken = _jwtService.GenerateRefreshToken(userId, email);
        
        Response.Cookies.Append("refreshToken", newRefreshToken, _refreshCookieOptions);
        return Ok(new { accessToken = newAccessToken });
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized(new { message = "Missing identifier" });

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "Could not find user with ID" });
        
        return Ok(new CurrentUserDto(user));
    }
}