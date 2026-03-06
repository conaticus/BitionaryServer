using System.ComponentModel.DataAnnotations;

namespace BitionaryServer.DTO;

public class RegisterUserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginUserDto
{
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}