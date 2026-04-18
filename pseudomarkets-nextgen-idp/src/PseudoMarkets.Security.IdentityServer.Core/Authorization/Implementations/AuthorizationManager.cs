using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authorization.Implementations;

public class AuthorizationManager : IAuthorizationManager
{
    private readonly Dictionary<string, List<string>> _actionToRoleMap = new Dictionary<string, List<string>>()
    {
        { $"{ActionConstants.AccountSummaryView}", new List<string>() { RoleConstants.ViewBalances, RoleConstants.ViewPositions, RoleConstants.ViewTransactions } }
    };
    
    public AuthorizationResult Authorize(AuthorizationRequest request)
    {
        throw new NotImplementedException();
    }
}