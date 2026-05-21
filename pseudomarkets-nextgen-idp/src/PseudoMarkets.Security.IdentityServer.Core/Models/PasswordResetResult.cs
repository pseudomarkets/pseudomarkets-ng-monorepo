namespace PseudoMarkets.Security.IdentityServer.Core.Models;

/// <summary>
/// Response returned after a password reset attempt.
/// </summary>
public class PasswordResetResult
{
    /// <summary>
    /// Indicates whether the password reset succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Human-readable password reset result message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Login ID for the account that was reset.
    /// </summary>
    public string LoginId { get; }

    /// <summary>
    /// Replacement one-time password reset key. This value is shown only after a successful reset.
    /// </summary>
    public string? PasswordResetKey { get; }

    public PasswordResetResult(bool success, string message, string loginId, string? passwordResetKey)
    {
        Success = success;
        Message = message;
        LoginId = loginId;
        PasswordResetKey = passwordResetKey;
    }
}
