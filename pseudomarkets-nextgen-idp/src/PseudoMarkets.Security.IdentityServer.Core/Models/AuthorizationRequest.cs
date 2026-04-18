namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthorizationRequest
{
    public string Token { get; }
    public string Action { get; }
    public AuthorizationRequest(string token, string action)
    {
        Token = token;
        Action = action;
    }
}