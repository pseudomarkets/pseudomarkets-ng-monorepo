using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;

public interface IAuthenticationManager
{
    AuthenticationResult Authenticate(string loginId, string password);
    AuthenticationResult Refresh(string refreshToken);
    
    string HashPassword(string plainTextPassword);
}
