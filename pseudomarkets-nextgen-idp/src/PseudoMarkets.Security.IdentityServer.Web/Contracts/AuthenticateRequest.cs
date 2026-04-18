using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

public class AuthenticateRequest
{
    [Required]
    public string LoginId { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
