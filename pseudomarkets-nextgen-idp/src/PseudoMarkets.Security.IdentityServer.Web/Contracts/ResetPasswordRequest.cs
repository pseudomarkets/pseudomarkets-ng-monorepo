using System.ComponentModel.DataAnnotations;

namespace PseudoMarkets.Security.IdentityServer.Web.Contracts;

public class ResetPasswordRequest
{
    [Required]
    public string LoginId { get; init; } = string.Empty;

    [Required]
    public string PasswordResetKey { get; init; } = string.Empty;

    [Required]
    public string NewPassword { get; init; } = string.Empty;
}
