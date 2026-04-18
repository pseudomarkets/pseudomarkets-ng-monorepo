namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class Account
{
    public string LoginId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string HashedPassword { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
}
