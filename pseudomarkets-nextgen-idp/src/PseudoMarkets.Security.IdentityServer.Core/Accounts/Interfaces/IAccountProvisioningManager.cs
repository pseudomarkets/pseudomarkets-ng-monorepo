using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;

public interface IAccountProvisioningManager
{
    AccountCreationResult CreateAccount(string loginId, string password, string accountType);
}
