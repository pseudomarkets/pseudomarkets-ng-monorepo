using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
