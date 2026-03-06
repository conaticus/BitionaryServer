using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BitionaryServer.Models;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(30)]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required] public bool IsEmailVerified { get; set; } = false;
    
    [MaxLength(100)]
    public string? GithubId { get; set; }

    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}