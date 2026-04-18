using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;

public interface IAuthorizationManager
{
    AuthorizationResult Authorize(AuthorizationRequest request);
}