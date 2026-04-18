namespace PseudoMarkets.Security.IdentityServer.Core.Models;

public class AccountCreationResult
{
    public bool Success { get; }
    public string Message { get; }
    public string LoginId { get; }
    public string AccountType { get; }

    public AccountCreationResult(bool success, string message, string loginId, string accountType)
    {
        Success = success;
        Message = message;
        LoginId = loginId;
        AccountType = accountType;
    }
}
