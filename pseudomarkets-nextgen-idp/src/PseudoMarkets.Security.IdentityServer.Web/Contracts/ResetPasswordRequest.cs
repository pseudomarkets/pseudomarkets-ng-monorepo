using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

/// <summary>
/// Request used to reset a USER account password with a one-time reset key.
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Account login ID for the USER account being reset.
    /// </summary>
    /// <example>demo.user</example>
    [Required]
    public string LoginId { get; init; } = string.Empty;

    /// <summary>
    /// One-time password reset key shown during account creation or the previous password reset.
    /// </summary>
    [Required]
    public string PasswordResetKey { get; init; } = string.Empty;

    /// <summary>
    /// New plain-text password. The IDP hashes this value before storing it.
    /// </summary>
    /// <example>NewChangeMe123!</example>
    [Required]
    public string NewPassword { get; init; } = string.Empty;
}
