namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthenticationResult
{
    public bool Success { get; }
    public string Token { get; }
    public DateTime Expires { get; }
    
    public AuthenticationResult(bool success, string token, DateTime expires)
    {
        Success = success;
        Token = token;
        Expires = expires;
    }
}