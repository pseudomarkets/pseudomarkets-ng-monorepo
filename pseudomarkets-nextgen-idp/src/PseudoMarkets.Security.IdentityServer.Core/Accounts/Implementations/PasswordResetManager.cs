using Microsoft.Extensions.Logging;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Accounts.Implementations;

public class PasswordResetManager : IPasswordResetManager
{
    private const string ValidationFailureMessage = "Login ID, password reset key, and new password are required.";
    private const string ResetFailureMessage = "Password reset failed.";

    private readonly IAccountRepository _accountRepository;
    private readonly IAuthenticationManager _authenticationManager;
    private readonly ILogger<PasswordResetManager> _logger;

    public PasswordResetManager(
        IAccountRepository accountRepository,
        IAuthenticationManager authenticationManager,
        ILogger<PasswordResetManager> logger)
    {
        _accountRepository = accountRepository;
        _authenticationManager = authenticationManager;
        _logger = logger;
    }

    public PasswordResetResult ResetPassword(string loginId, string passwordResetKey, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(loginId) ||
            string.IsNullOrWhiteSpace(passwordResetKey) ||
            string.IsNullOrWhiteSpace(newPassword))
        {
            return new PasswordResetResult(false, ValidationFailureMessage, loginId, null);
        }

        try
        {
            var account = _accountRepository.GetAccount(loginId);
            if (account is null ||
                !account.IsActive ||
                account.AccountType != AccountTypeConstants.UserType ||
                !PasswordResetKeyProtector.VerifyHash(account.HashedPasswordResetKey, passwordResetKey))
            {
                return new PasswordResetResult(false, ResetFailureMessage, loginId, null);
            }

            var newResetKey = PasswordResetKeyProtector.GenerateKey();
            account.HashedPassword = _authenticationManager.HashPassword(newPassword);
            account.HashedPasswordResetKey = PasswordResetKeyProtector.HashKey(newResetKey);
            account.FailedLoginAttempts = 0;
            account.LockoutUntilUtc = null;

            _accountRepository.UpdateAccount(account);

            return new PasswordResetResult(true, "Password reset successfully.", loginId, newResetKey);
        }
        catch (IdentityDependencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while resetting password for {LoginId}.", loginId);
            throw new IdentityServiceException("Unable to reset the password.", ex);
        }
    }
}
