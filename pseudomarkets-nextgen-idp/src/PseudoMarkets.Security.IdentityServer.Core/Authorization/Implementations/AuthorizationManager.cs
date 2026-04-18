using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Authorization.Implementations;

public class AuthorizationManager : IAuthorizationManager
{
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly ILogger<AuthorizationManager> _logger;

    public AuthorizationManager(JwtConfiguration jwtConfiguration, ILogger<AuthorizationManager> logger)
    {
        _jwtConfiguration = jwtConfiguration;
        _logger = logger;
    }
    
    public AuthorizationResult Authorize(AuthorizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Action))
        {
            return new AuthorizationResult(false, "Authorization Failed");
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
                var roles = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "roles");
                if (roles != null)
                {
                    var rolesAsList = roles.Value.Split(',').ToList();
                    if (rolesAsList.Contains(request.Action))
                    {
                        return new AuthorizationResult(true, $"Authorization Successful");
                    }
                    
                    return new AuthorizationResult(false, "Unauthorized");
                }
            }
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogInformation(ex, "Token validation failed during authorization.");
            return new AuthorizationResult(false, "Authorization Failed");
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Authorization request contained an invalid token payload.");
            return new AuthorizationResult(false, "Authorization Failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while authorizing action {Action}.", request.Action);
            throw new IdentityServiceException("Unable to complete authorization.", ex);
        }

        return new  AuthorizationResult(false, "Authorization Failed");
    }
}
