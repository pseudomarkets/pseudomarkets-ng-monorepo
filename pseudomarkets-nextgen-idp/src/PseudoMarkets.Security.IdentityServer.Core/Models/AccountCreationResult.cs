namespace PseudoMarkets.Security.IdentityServer.Core.Models;

/// <summary>
/// Response returned after an account creation attempt.
/// </summary>
public class AccountCreationResult
{
    /// <summary>
    /// Indicates whether account creation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Human-readable result message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Login ID assigned to the created account.
    /// </summary>
    public string LoginId { get; }

    /// <summary>
    /// Account type that was created.
    /// </summary>
    public string AccountType { get; }

    /// <summary>
    /// One-time password reset key for USER accounts. This value is shown only after sign-up or password reset.
    /// </summary>
    public string? PasswordResetKey { get; }

    public AccountCreationResult(bool success, string message, string loginId, string accountType, string? passwordResetKey = null)
    {
        Success = success;
        Message = message;
        LoginId = loginId;
        AccountType = accountType;
        PasswordResetKey = passwordResetKey;
    }
}
