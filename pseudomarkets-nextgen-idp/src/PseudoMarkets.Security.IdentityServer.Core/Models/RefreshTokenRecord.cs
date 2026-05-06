namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public sealed class RefreshTokenRecord
{
    public string TokenId { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string LoginId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsConsumed { get; set; }
    public bool IsRevoked { get; set; }
    public int Generation { get; set; }
}
