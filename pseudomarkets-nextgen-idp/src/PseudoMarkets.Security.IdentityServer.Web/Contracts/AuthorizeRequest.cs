using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

public class AuthorizeRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    public string Action { get; init; } = string.Empty;
}
