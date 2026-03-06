namespace BitionaryServer.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; }
    public int ExpiryMinutes { get; set; }
    
    public string RefreshSecretKey { get; set; }
    public int RefreshExpiryDays { get; set; }
    
    public string Issuer { get; set; }
    public string Audience { get; set; }
}