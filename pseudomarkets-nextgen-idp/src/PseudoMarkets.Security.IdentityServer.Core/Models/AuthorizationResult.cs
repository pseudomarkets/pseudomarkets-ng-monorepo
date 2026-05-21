namespace PseudoMarkets.Security.IdentityServer.Core.Models;

/// <summary>
/// Response returned after validating whether a JWT can perform a platform action.
/// </summary>
public class AuthorizationResult
{
    /// <summary>
    /// Indicates whether the token is authorized for the requested action.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Human-readable authorization result message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Ten-digit Pseudo Markets user ID associated with the authorized token.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Token account type from the JWT account_type claim.
    /// </summary>
    public string TokenType { get; }

    public AuthorizationResult(bool success, string message, long userId, string tokenType = "")
    {
        Success = success;
        Message = message;
        UserId = userId;
        TokenType = tokenType;
    }
}
