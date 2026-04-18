using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.Shared.Authorization.Interfaces;

public interface IIdentityAuthorizationClient
{
    Task<AuthorizationDecision> AuthorizeAsync(string token, string action, CancellationToken cancellationToken = default);
}
