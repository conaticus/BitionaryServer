namespace BitionaryServer.Models;

public class GithubEmail
{
    public string Email { get; } = null!;
    public bool Primary { get; }
    public bool Verified { get; }
}