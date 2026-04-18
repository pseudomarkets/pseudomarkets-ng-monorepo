namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthenticationRequest
{
    public string LoginId { get; }
    public string Password { get; }
    
    public AuthenticationRequest(string loginId, string password)
    {
        LoginId = loginId;
        Password = password;
    }
}