using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;

public interface IAccountRepository
{
    Account? GetAccount(string loginId);
    bool TryReserveUserId(long userId, string loginId);
    void ReleaseReservedUserId(long userId);
    void UpdateAccount(Account account);
    void CreateAccount(Account account);
}
