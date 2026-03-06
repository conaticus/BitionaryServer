using System.Security.Claims;
using System.Text;
using BitionaryServer.Configuration;
using BitionaryServer.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BitionaryServer.Services;

public class JwtService(JwtSettings jwtSettings)
{
    public string GenerateAccessToken(Guid userId, string email)
    {
        return GenerateToken(userId, email, jwtSettings.SecretKey, DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes));
    }
    
    public string GenerateRefreshToken(Guid userId, string email)
    {
        return GenerateToken(userId, email, jwtSettings.RefreshSecretKey, DateTime.UtcNow.AddDays(jwtSettings.RefreshExpiryDays));
    }
    
    public string GenerateToken(Guid userId, string email, string keyRaw, DateTime expireDate)
    {
        var key = Encoding.UTF8.GetBytes(keyRaw);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
            Issuer = jwtSettings.Issuer,     // TODO: configure for dev/prod
            Audience = jwtSettings.Audience,
            SigningCredentials = signingCredentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }
    
    
    public async Task<ClaimsPrincipal?> ValidateToken(string token)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.RefreshSecretKey);

        try {
            var result = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            });

            if (!result.IsValid)
            {
                return null;
            }

            var claimsIdentity = new ClaimsIdentity(result.ClaimsIdentity.Claims);
            return new ClaimsPrincipal(claimsIdentity);
        } catch {
            return null;
        }
    }
}