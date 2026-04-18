using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

public class CreateAccountRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string AccountType { get; init; } = string.Empty;
}
