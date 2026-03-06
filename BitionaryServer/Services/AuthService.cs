using BitionaryServer.Data;
using BitionaryServer.DTO;
using BitionaryServer.Exceptions;
using BitionaryServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BitionaryServer.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApplicationUser> RegisterAsync(RegisterUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email.ToLower()))
        {
            throw new UserAlreadyExistsException();
        }
        
        var user = new ApplicationUser()
        {
            Username = dto.Username,
            Email = dto.Email.ToLower(),
        };
        
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
        _db.Users.Add(user);
        
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<ApplicationUser?> ValidateUserAsync(LoginUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return null;

        // TODO: Handle no password hash - perhaps prompt a continue with github if github user (if no password && github id)
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        return result == PasswordVerificationResult.Success ? user : null;
    }
    
    public async Task<ApplicationUser> GetOrCreateGithubUserAsync(string githubId, string username, string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GithubId == githubId);
        if (user != null)
            return user;
        
        user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is { IsEmailVerified: true })
        {
            user.GithubId = githubId;
            await _db.SaveChangesAsync();
            return user;
        }

        if (user is { IsEmailVerified: false })
        {
            throw new AccountLinkRequiredException();
        }

        user = new ApplicationUser
        {
            Username = username,
            Email = email,
            IsEmailVerified = true,
            GithubId = githubId,
        };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
}