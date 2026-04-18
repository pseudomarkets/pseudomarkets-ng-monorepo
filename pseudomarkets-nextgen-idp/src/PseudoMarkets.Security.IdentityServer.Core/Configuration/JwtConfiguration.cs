namespace PseudoMarkets.Security.IdentityServer.Core.Configuration;

public class JwtConfiguration
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
