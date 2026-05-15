namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class PasswordResetResult
{
    public bool Success { get; }
    public string Message { get; }
    public string LoginId { get; }
    public string? PasswordResetKey { get; }

    public PasswordResetResult(bool success, string message, string loginId, string? passwordResetKey)
    {
        Success = success;
        Message = message;
        LoginId = loginId;
        PasswordResetKey = passwordResetKey;
    }
}
