using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;

public interface IAuthenticationManager
{
    AuthenticationResult Authenticate(string loginId, string password);
    
    string HashPassword(string plainTextPassword);
}