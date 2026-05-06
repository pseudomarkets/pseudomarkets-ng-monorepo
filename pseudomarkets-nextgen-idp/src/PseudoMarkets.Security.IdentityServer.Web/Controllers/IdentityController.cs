using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Models;
using PseudoMarkets.Security.IdentityServer.Web.Constants;
using PseudoMarkets.Security.IdentityServer.Web.Contracts;

namespace PseudoMarkets.Security.IdentityServer.Web.Controllers;

[ApiController]
[Route("api/identity")]
[EnableRateLimiting("identity-sensitive")]
public class IdentityController : ControllerBase
{
    private readonly IAccountProvisioningManager _accountProvisioningManager;
    private readonly IAuthenticationManager _authenticationManager;
    private readonly IAuthorizationManager _authorizationManager;
    private readonly IdentitySecurityConfiguration _identitySecurityConfiguration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IAccountProvisioningManager accountProvisioningManager,
        IAuthenticationManager authenticationManager,
        IAuthorizationManager authorizationManager,
        IdentitySecurityConfiguration identitySecurityConfiguration,
        IWebHostEnvironment environment,
        ILogger<IdentityController> logger)
    {
        _accountProvisioningManager = accountProvisioningManager;
        _authenticationManager = authenticationManager;
        _authorizationManager = authorizationManager;
        _identitySecurityConfiguration = identitySecurityConfiguration;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("create")]
    public ActionResult<AccountCreationResult> Create([FromBody] CreateAccountRequest request)
    {
        var requestedAccountType = NormalizeAccountType(request.AccountType);

        if (requestedAccountType == AccountTypeConstants.SystemType && !_environment.IsDevelopment() && !HasValidSystemAccountBypassKey())
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new AccountCreationResult(
                    false,
                    $"SYSTEM account creation is only allowed in development unless the {HeaderConstants.SystemAccountBypassKeyHeader} header matches the configured system account bypass key.",
                    request.Username,
                    requestedAccountType));
        }

        if (requestedAccountType == AccountTypeConstants.SystemType && !_environment.IsDevelopment())
        {
            _logger.LogWarning("SYSTEM account creation for {Username} authorized outside development by bypass header.", request.Username);
        }

        _logger.LogDebug("Processing account creation for {Username} with requested type {AccountType}.", request.Username, requestedAccountType);
        var result = _accountProvisioningManager.CreateAccount(request.Username, request.Password, requestedAccountType);

        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Message == "Unsupported account type.")
        {
            return BadRequest(result);
        }

        return Conflict(result);
    }

    [HttpPost("authenticate")]
    public ActionResult<AuthenticationResult> Authenticate([FromBody] AuthenticateRequest request)
    {
        _logger.LogDebug("Processing authentication request for {LoginId}.", request.LoginId);
        var result = _authenticationManager.Authenticate(request.LoginId, request.Password);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    public ActionResult<AuthenticationResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        _logger.LogDebug("Processing refresh token request.");
        var result = _authenticationManager.Refresh(request.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("authorize")]
    public ActionResult<AuthorizationResult> Authorize([FromBody] AuthorizeRequest request)
    {
        _logger.LogDebug("Processing authorization request for action {Action}.", request.Action);
        var result = _authorizationManager.Authorize(new AuthorizationRequest(request.Token, request.Action));
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status403Forbidden, result);
    }

    private static string NormalizeAccountType(string? accountType)
    {
        if (string.IsNullOrWhiteSpace(accountType))
        {
            return AccountTypeConstants.UserType;
        }

        return accountType.Trim().ToUpperInvariant();
    }

    private bool HasValidSystemAccountBypassKey()
    {
        if (!Request.Headers.TryGetValue(HeaderConstants.SystemAccountBypassKeyHeader, out var providedHeaderValues))
        {
            return false;
        }

        var providedKey = providedHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(providedKey) || string.IsNullOrWhiteSpace(_identitySecurityConfiguration.SystemAccountBypassKey))
        {
            return false;
        }

        var configuredKeyBytes = Encoding.UTF8.GetBytes(_identitySecurityConfiguration.SystemAccountBypassKey);
        var providedKeyBytes = Encoding.UTF8.GetBytes(providedKey);

        return CryptographicOperations.FixedTimeEquals(providedKeyBytes, configuredKeyBytes);
    }
}
