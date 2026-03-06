using BitionaryServer.Models;

namespace BitionaryServer.DTO;

public class CurrentUserDto(ApplicationUser user)
{
    public string Id { get; set; } = user.Id.ToString();
    public string Username { get; set; } = user.Username;
    public string Email { get; set; } = user.Email;

    public DateTime CreatedAt { get; set; } = user.CreatedAt;
    public DateTime UpdatedAt { get; set; } = user.UpdatedAt;

    public bool IsActive { get; set; } = user.IsActive;
}