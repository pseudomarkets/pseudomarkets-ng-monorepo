using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

/// <summary>
/// Request used by platform services to authorize a JWT for a specific action.
/// </summary>
public class AuthorizeRequest
{
    /// <summary>
    /// JWT access token issued by the IDP.
    /// </summary>
    [Required]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Platform authorization action being requested.
    /// </summary>
    /// <example>VIEW_MARKET_DATA</example>
    [Required]
    public string Action { get; init; } = string.Empty;
}
