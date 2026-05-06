using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authorization.Implementations;

public class AuthorizationManager : IAuthorizationManager
{
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AuthorizationManager> _logger;

    public AuthorizationManager(
        JwtConfiguration jwtConfiguration,
        IAccountRepository accountRepository,
        ILogger<AuthorizationManager> logger)
    {
        _jwtConfiguration = jwtConfiguration;
        _accountRepository = accountRepository;
        _logger = logger;
    }
    
    public AuthorizationResult Authorize(AuthorizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Action))
        {
            return new AuthorizationResult(false, "Authorization Failed", 0);
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var key = Encoding.ASCII.GetBytes(_jwtConfiguration.Key);

            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtConfiguration.Issuer,
                ValidAudience = _jwtConfiguration.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
            
            var claimsPrincipal = tokenHandler.ValidateToken(request.Token, validationParameters, out var securityToken);

            if (claimsPrincipal != null && securityToken != null)
            {
                var loginId = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
                var roles = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "roles");
                var userId = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
                var tokenType = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "account_type")?.Value;
                if (!string.IsNullOrWhiteSpace(loginId) && roles != null && userId != null && !string.IsNullOrWhiteSpace(tokenType))
                {
                    var id = Convert.ToInt64(userId);
                    var rolesAsList = roles.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var account = _accountRepository.GetAccount(loginId);
                    if (account is null || !account.IsActive)
                    {
                        return new AuthorizationResult(false, "Authorization Failed", 0);
                    }

                    if (account.UserId != id ||
                        !string.Equals(account.AccountType, tokenType, StringComparison.OrdinalIgnoreCase))
                    {
                        return new AuthorizationResult(false, "Authorization Failed", 0);
                    }

                    if (rolesAsList.Contains(request.Action, StringComparer.Ordinal) &&
                        account.Roles.Contains(request.Action, StringComparer.Ordinal))
                    {
                        return new AuthorizationResult(true, "Authorization Successful", id, tokenType);
                    }
                    
                    return new AuthorizationResult(false, "Unauthorized", 0, tokenType);
                }
            }
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogInformation(ex, "Token validation failed during authorization.");
            return new AuthorizationResult(false, "Authorization Failed", 0);
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Authorization request contained an invalid token payload.");
            return new AuthorizationResult(false, "Authorization Failed", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while authorizing action {Action}.", request.Action);
            throw new IdentityServiceException("Unable to complete authorization.", ex);
        }

        return new  AuthorizationResult(false, "Authorization Failed", 0);
    }
}
