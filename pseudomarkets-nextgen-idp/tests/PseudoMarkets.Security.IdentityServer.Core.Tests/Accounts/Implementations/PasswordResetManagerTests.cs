using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Accounts;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Accounts.Implementations;

[TestFixture]
public class PasswordResetManagerTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IAuthenticationManager> _authenticationManager = null!;
    private Mock<ILogger<PasswordResetManager>> _logger = null!;
    private PasswordResetManager _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _authenticationManager = new Mock<IAuthenticationManager>();
        _logger = new Mock<ILogger<PasswordResetManager>>();
        _authenticationManager.Setup(x => x.HashPassword(It.IsAny<string>())).Returns<string>(password => $"salt:{password}-hash");
        _sut = new PasswordResetManager(_accountRepository.Object, _authenticationManager.Object, _logger.Object);
    }

    [TestCase("", "00000000-0000-0000-0000-000000000001", "new-password")]
    [TestCase("user", "", "new-password")]
    [TestCase("user", "00000000-0000-0000-0000-000000000001", "")]
    public void ResetPassword_ShouldFailValidation_WhenRequiredFieldsAreBlank(string loginId, string resetKey, string newPassword)
    {
        var result = _sut.ResetPassword(loginId, resetKey, newPassword);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Login ID, password reset key, and new password are required.");
        result.PasswordResetKey.ShouldBeNull();
        _accountRepository.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public void ResetPassword_ShouldFail_WhenAccountDoesNotExist()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Returns((Account?)null);

        var result = _sut.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Password reset failed.");
        _accountRepository.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public void ResetPassword_ShouldFail_WhenAccountIsSystemType()
    {
        _accountRepository.Setup(x => x.GetAccount("system")).Returns(new Account
        {
            LoginId = "system",
            AccountType = AccountTypeConstants.SystemType,
            IsActive = true,
            HashedPasswordResetKey = PasswordResetKeyProtector.HashKey("00000000-0000-0000-0000-000000000001")
        });

        var result = _sut.ResetPassword("system", "00000000-0000-0000-0000-000000000001", "new-password");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Password reset failed.");
    }

    [Test]
    public void ResetPassword_ShouldFail_WhenAccountIsInactive()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            AccountType = AccountTypeConstants.UserType,
            IsActive = false,
            HashedPasswordResetKey = PasswordResetKeyProtector.HashKey("00000000-0000-0000-0000-000000000001")
        });

        var result = _sut.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Password reset failed.");
    }

    [Test]
    public void ResetPassword_ShouldFail_WhenResetKeyDoesNotMatch()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            AccountType = AccountTypeConstants.UserType,
            IsActive = true,
            HashedPasswordResetKey = PasswordResetKeyProtector.HashKey("00000000-0000-0000-0000-000000000001")
        });

        var result = _sut.ResetPassword("user", "00000000-0000-0000-0000-000000000002", "new-password");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Password reset failed.");
        _accountRepository.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public void ResetPassword_ShouldUpdatePassword_ResetKeyAndLockoutState_WhenRequestIsValid()
    {
        var originalResetKey = "00000000-0000-0000-0000-000000000001";
        Account? updatedAccount = null;
        var account = new Account
        {
            LoginId = "user",
            AccountType = AccountTypeConstants.UserType,
            IsActive = true,
            HashedPassword = "old-password-hash",
            HashedPasswordResetKey = PasswordResetKeyProtector.HashKey(originalResetKey),
            FailedLoginAttempts = 3,
            LockoutUntilUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _accountRepository.Setup(x => x.GetAccount("user")).Returns(account);
        _accountRepository.Setup(x => x.UpdateAccount(It.IsAny<Account>())).Callback<Account>(candidate => updatedAccount = candidate);

        var result = _sut.ResetPassword("user", originalResetKey, "new-password");

        result.Success.ShouldBeTrue();
        result.Message.ShouldBe("Password reset successfully.");
        result.PasswordResetKey.ShouldNotBeNullOrWhiteSpace();
        Guid.TryParse(result.PasswordResetKey, out _).ShouldBeTrue();
        updatedAccount.ShouldNotBeNull();
        updatedAccount!.HashedPassword.ShouldBe("salt:new-password-hash");
        updatedAccount.HashedPasswordResetKey.ShouldNotBe(PasswordResetKeyProtector.HashKey(originalResetKey));
        PasswordResetKeyProtector.VerifyHash(updatedAccount.HashedPasswordResetKey, result.PasswordResetKey!).ShouldBeTrue();
        updatedAccount.FailedLoginAttempts.ShouldBe(0);
        updatedAccount.LockoutUntilUtc.ShouldBeNull();
    }

    [Test]
    public void ResetPassword_ShouldInvalidatePriorResetKey_AfterSuccessfulReset()
    {
        var originalResetKey = "00000000-0000-0000-0000-000000000001";
        var account = new Account
        {
            LoginId = "user",
            AccountType = AccountTypeConstants.UserType,
            IsActive = true,
            HashedPasswordResetKey = PasswordResetKeyProtector.HashKey(originalResetKey)
        };

        _accountRepository.Setup(x => x.GetAccount("user")).Returns(account);

        var firstReset = _sut.ResetPassword("user", originalResetKey, "new-password");
        var secondReset = _sut.ResetPassword("user", originalResetKey, "newer-password");

        firstReset.Success.ShouldBeTrue();
        secondReset.Success.ShouldBeFalse();
        secondReset.Message.ShouldBe("Password reset failed.");
    }

    [Test]
    public void ResetPassword_ShouldRethrowDependencyExceptions()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new IdentityDependencyException("boom"));

        Should.Throw<IdentityDependencyException>(() =>
            _sut.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password"));
    }

    [Test]
    public void ResetPassword_ShouldWrapUnexpectedExceptions()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new InvalidOperationException("boom"));

        var ex = Should.Throw<IdentityServiceException>(() =>
            _sut.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password"));

        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
}
