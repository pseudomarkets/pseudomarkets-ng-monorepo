using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
[Produces("application/json")]
public class IdentityController : ControllerBase
{
    private readonly IAccountProvisioningManager _accountProvisioningManager;
    private readonly IAuthenticationManager _authenticationManager;
    private readonly IAuthorizationManager _authorizationManager;
    private readonly IPasswordResetManager _passwordResetManager;
    private readonly IdentitySecurityConfiguration _identitySecurityConfiguration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IAccountProvisioningManager accountProvisioningManager,
        IAuthenticationManager authenticationManager,
        IAuthorizationManager authorizationManager,
        IPasswordResetManager passwordResetManager,
        IdentitySecurityConfiguration identitySecurityConfiguration,
        IWebHostEnvironment environment,
        ILogger<IdentityController> logger)
    {
        _accountProvisioningManager = accountProvisioningManager;
        _authenticationManager = authenticationManager;
        _authorizationManager = authorizationManager;
        _passwordResetManager = passwordResetManager;
        _identitySecurityConfiguration = identitySecurityConfiguration;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Creates an identity account.
    /// </summary>
    /// <remarks>
    /// Creates a USER account by default. SYSTEM accounts can be created only in Development mode, or when the
    /// configured system-account bypass key is supplied in the X-PseudoMarkets-System-Key header.
    /// USER account creation returns a password reset key that is shown only at sign-up or after password reset.
    /// </remarks>
    [HttpPost("create")]
    [EndpointSummary("Create an identity account")]
    [EndpointDescription("Creates a USER account by default. SYSTEM creation is restricted to Development mode or a valid system-account bypass header.")]
    [ProducesResponseType<AccountCreationResult>(
        StatusCodes.Status200OK,
        Description = "The account was created successfully.")]
    [ProducesResponseType<AccountCreationResult>(
        StatusCodes.Status400BadRequest,
        Description = "The request contains an unsupported account type or invalid account creation data.")]
    [ProducesResponseType<AccountCreationResult>(
        StatusCodes.Status403Forbidden,
        Description = "SYSTEM account creation was requested outside Development without a valid bypass header.")]
    [ProducesResponseType<AccountCreationResult>(
        StatusCodes.Status409Conflict,
        Description = "The requested account could not be created because it conflicts with existing identity data.")]
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

    /// <summary>
    /// Authenticates an identity account.
    /// </summary>
    /// <remarks>
    /// Validates the login ID and password, then returns a signed JWT access token and an opaque refresh token.
    /// </remarks>
    [HttpPost("authenticate")]
    [EndpointSummary("Authenticate an identity account")]
    [EndpointDescription("Validates credentials and returns access-token plus refresh-token material when authentication succeeds.")]
    [ProducesResponseType<AuthenticationResult>(
        StatusCodes.Status200OK,
        Description = "Authentication succeeded and token material was issued.")]
    [ProducesResponseType<AuthenticationResult>(
        StatusCodes.Status401Unauthorized,
        Description = "Authentication failed because the credentials are invalid or the account is locked.")]
    public ActionResult<AuthenticationResult> Authenticate([FromBody] AuthenticateRequest request)
    {
        _logger.LogDebug("Processing authentication request for {LoginId}.", request.LoginId);
        var result = _authenticationManager.Authenticate(request.LoginId, request.Password);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Refreshes authentication tokens.
    /// </summary>
    /// <remarks>
    /// Consumes a valid refresh token, rotates it, and returns a new JWT access token plus replacement refresh token.
    /// Refresh tokens are single-use.
    /// </remarks>
    [HttpPost("refresh")]
    [EndpointSummary("Refresh authentication tokens")]
    [EndpointDescription("Rotates a valid refresh token and returns a new access token plus replacement refresh token.")]
    [ProducesResponseType<AuthenticationResult>(
        StatusCodes.Status200OK,
        Description = "Refresh succeeded and replacement token material was issued.")]
    [ProducesResponseType<AuthenticationResult>(
        StatusCodes.Status401Unauthorized,
        Description = "Refresh failed because the refresh token is invalid, expired, or already consumed.")]
    public ActionResult<AuthenticationResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        _logger.LogDebug("Processing refresh token request.");
        var result = _authenticationManager.Refresh(request.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Resets a USER account password.
    /// </summary>
    /// <remarks>
    /// Validates the one-time password reset key, updates the password, clears login lockout state, and returns a
    /// replacement password reset key. SYSTEM accounts cannot reset passwords through this endpoint.
    /// </remarks>
    [HttpPost("reset-password")]
    [EndpointSummary("Reset a USER account password")]
    [EndpointDescription("Uses a one-time password reset key to set a new USER account password and returns a replacement reset key.")]
    [ProducesResponseType<PasswordResetResult>(
        StatusCodes.Status200OK,
        Description = "The password was reset successfully and a replacement reset key was issued.")]
    [ProducesResponseType<PasswordResetResult>(
        StatusCodes.Status400BadRequest,
        Description = "The login ID, password reset key, or new password was missing.")]
    [ProducesResponseType<PasswordResetResult>(
        StatusCodes.Status401Unauthorized,
        Description = "Password reset failed because the reset key is invalid, the account is not a USER account, or the account was not found.")]
    public ActionResult<PasswordResetResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LoginId) ||
            string.IsNullOrWhiteSpace(request.PasswordResetKey) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new PasswordResetResult(
                false,
                "Login ID, password reset key, and new password are required.",
                request.LoginId,
                null));
        }

        _logger.LogDebug("Processing password reset request for {LoginId}.", request.LoginId);
        var result = _passwordResetManager.ResetPassword(request.LoginId, request.PasswordResetKey, request.NewPassword);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Authorizes a token for a platform action.
    /// </summary>
    /// <remarks>
    /// Validates a JWT issued by the IDP, rechecks the current account state in Aerospike, and confirms that the
    /// token has the role required for the requested platform action.
    /// </remarks>
    [HttpPost("authorize")]
    [EndpointSummary("Authorize a platform action")]
    [EndpointDescription("Validates a JWT and verifies that the authenticated account is allowed to perform the requested platform action.")]
    [ProducesResponseType<AuthorizationResult>(
        StatusCodes.Status200OK,
        Description = "The token is valid and authorized for the requested action.")]
    [ProducesResponseType<AuthorizationResult>(
        StatusCodes.Status403Forbidden,
        Description = "The token is invalid, the account is inactive or missing, or the token lacks the required role for the action.")]
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
