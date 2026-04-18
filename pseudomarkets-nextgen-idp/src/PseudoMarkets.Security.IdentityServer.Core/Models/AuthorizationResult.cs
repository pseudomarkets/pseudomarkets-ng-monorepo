namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AuthorizationResult
{
    public bool Success { get; }
    public string Message { get; }
    public long UserId { get; set; }
    
    public AuthorizationResult(bool success, string message, long userId)
    {
        Success = success;
        Message = message;
        UserId = userId;
    }
}