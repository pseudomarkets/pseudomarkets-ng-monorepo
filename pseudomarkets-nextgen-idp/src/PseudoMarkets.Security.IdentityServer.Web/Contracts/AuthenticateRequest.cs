using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

/// <summary>
/// Request used to authenticate an existing identity account.
/// </summary>
public class AuthenticateRequest
{
    /// <summary>
    /// Account login ID.
    /// </summary>
    /// <example>demo.user</example>
    [Required]
    public string LoginId { get; init; } = string.Empty;

    /// <summary>
    /// Plain-text account password.
    /// </summary>
    /// <example>ChangeMe123!</example>
    [Required]
    public string Password { get; init; } = string.Empty;
}
