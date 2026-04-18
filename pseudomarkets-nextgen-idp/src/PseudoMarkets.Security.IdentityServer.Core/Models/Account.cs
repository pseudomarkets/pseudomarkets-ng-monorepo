namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class Account
{
    public string LoginId { get; set; }
    public long UserId { get; set; }
    public string HashedPassword { get; set; }
    public string AccountType { get; set; }
    public List<string> Roles { get; set; }
    public bool IsActive { get; set; }
}