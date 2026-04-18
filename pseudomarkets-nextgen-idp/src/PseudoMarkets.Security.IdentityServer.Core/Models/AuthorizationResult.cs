namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthorizationResult
{
    public bool Success { get; }
    public string Message { get; }
    
    public AuthorizationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}