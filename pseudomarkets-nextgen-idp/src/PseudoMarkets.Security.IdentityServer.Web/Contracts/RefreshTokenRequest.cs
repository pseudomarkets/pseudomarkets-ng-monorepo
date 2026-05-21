using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

/// <summary>
/// Request used to rotate refresh-token material.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Opaque refresh token previously issued by the IDP.
    /// </summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
