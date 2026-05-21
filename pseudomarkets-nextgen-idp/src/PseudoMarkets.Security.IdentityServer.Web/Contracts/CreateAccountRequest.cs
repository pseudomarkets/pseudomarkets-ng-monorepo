using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

/// <summary>
/// Request used to create a Pseudo Markets identity account.
/// </summary>
public class CreateAccountRequest
{
    /// <summary>
    /// Login name for the new account. This value is used as the account login ID.
    /// </summary>
    /// <example>demo.user</example>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Plain-text password submitted at account creation. The IDP hashes this value before storing it.
    /// </summary>
    /// <example>ChangeMe123!</example>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional account type. Defaults to USER when omitted. SYSTEM creation is restricted.
    /// </summary>
    /// <example>USER</example>
    public string AccountType { get; init; } = string.Empty;
}
