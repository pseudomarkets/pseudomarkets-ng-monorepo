using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Accounts.Implementations;

[TestFixture]
public class AccountProvisioningManagerTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IAuthenticationManager> _authenticationManager = null!;
    private Mock<ILogger<AccountProvisioningManager>> _logger = null!;
    private AccountProvisioningManager _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _authenticationManager = new Mock<IAuthenticationManager>();
        _logger = new Mock<ILogger<AccountProvisioningManager>>();
        _authenticationManager.Setup(x => x.HashPassword(It.IsAny<string>())).Returns<string>(password => $"salt:{password}-hash");
        _sut = new AccountProvisioningManager(_accountRepository.Object, _authenticationManager.Object, _logger.Object);
    }

    [TestCase("", "password", AccountTypeConstants.UserType)]
    [TestCase("user", "", AccountTypeConstants.UserType)]
    public void CreateAccount_ShouldFailValidation_WhenCredentialsAreBlank(string loginId, string password, string accountType)
    {
        var result = _sut.CreateAccount(loginId, password, accountType);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Username and password are required.");
    }

    [Test]
    public void CreateAccount_ShouldFailValidation_WhenAccountTypeIsUnsupported()
    {
        var result = _sut.CreateAccount("user", "password", "ADMIN");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Unsupported account type.");
    }

    [Test]
    public void CreateAccount_ShouldReturnConflict_WhenLoginAlreadyExists()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account { LoginId = "user", AccountType = AccountTypeConstants.UserType });

        var result = _sut.CreateAccount("user", "password", AccountTypeConstants.UserType);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("An account with that username already exists.");
    }

    [Test]
    public void CreateAccount_ShouldCreateUserAccount_WithHashedPasswordAndNoRoles()
    {
        Account? createdAccount = null;
        _accountRepository.Setup(x => x.GetAccount("public-user")).Returns((Account?)null);
        _accountRepository.Setup(x => x.TryReserveUserId(It.IsAny<long>(), "public-user")).Returns(true);
        _accountRepository.Setup(x => x.CreateAccount(It.IsAny<Account>())).Callback<Account>(account => createdAccount = account);

        var result = _sut.CreateAccount("public-user", "password", AccountTypeConstants.UserType);

        result.Success.ShouldBeTrue();
        result.AccountType.ShouldBe(AccountTypeConstants.UserType);
        createdAccount.ShouldNotBeNull();
        createdAccount!.HashedPassword.ShouldBe("salt:password-hash");
        createdAccount.HashedPassword.ShouldNotBe("password");
        createdAccount.AccountType.ShouldBe(AccountTypeConstants.UserType);
        createdAccount.Roles.ShouldBeEmpty();
        createdAccount.UserId.ShouldBeGreaterThanOrEqualTo(1_000_000_000);
        createdAccount.UserId.ShouldBeLessThanOrEqualTo(9_999_999_999);
    }

    [Test]
    public void CreateAccount_ShouldCreateSystemAccount_WithAllRoles()
    {
        Account? createdAccount = null;
        _accountRepository.Setup(x => x.GetAccount("system-user")).Returns((Account?)null);
        _accountRepository.Setup(x => x.TryReserveUserId(It.IsAny<long>(), "system-user")).Returns(true);
        _accountRepository.Setup(x => x.CreateAccount(It.IsAny<Account>())).Callback<Account>(account => createdAccount = account);

        var result = _sut.CreateAccount("system-user", "password", AccountTypeConstants.SystemType);

        result.Success.ShouldBeTrue();
        createdAccount.ShouldNotBeNull();
        createdAccount!.Roles.ShouldBe(RoleConstants.AllRoles, ignoreOrder: true);
    }

    [Test]
    public void CreateAccount_ShouldRetryUserIdReservation_UntilReservationSucceeds()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Returns((Account?)null);
        _accountRepository.SetupSequence(x => x.TryReserveUserId(It.IsAny<long>(), "user"))
            .Returns(false)
            .Returns(false)
            .Returns(true);

        var result = _sut.CreateAccount("user", "password", AccountTypeConstants.UserType);

        result.Success.ShouldBeTrue();
        _accountRepository.Verify(x => x.TryReserveUserId(It.IsAny<long>(), "user"), Times.Exactly(3));
    }

    [Test]
    public void CreateAccount_ShouldReleaseReservedUserId_WhenAccountCreationFails()
    {
        long reservedUserId = 0;
        _accountRepository.Setup(x => x.GetAccount("user")).Returns((Account?)null);
        _accountRepository
            .Setup(x => x.TryReserveUserId(It.IsAny<long>(), "user"))
            .Callback<long, string>((userId, _) => reservedUserId = userId)
            .Returns(true);
        _accountRepository.Setup(x => x.CreateAccount(It.IsAny<Account>())).Throws(new IdentityDependencyException("write failed"));

        Should.Throw<IdentityDependencyException>(() => _sut.CreateAccount("user", "password", AccountTypeConstants.UserType));

        _accountRepository.Verify(x => x.ReleaseReservedUserId(reservedUserId), Times.Once);
    }

    [Test]
    public void CreateAccount_ShouldRethrowDependencyExceptions()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new IdentityDependencyException("boom"));

        Should.Throw<IdentityDependencyException>(() => _sut.CreateAccount("user", "password", AccountTypeConstants.UserType));
    }

    [Test]
    public void CreateAccount_ShouldWrapUnexpectedExceptions()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new InvalidOperationException("boom"));

        var ex = Should.Throw<IdentityServiceException>(() => _sut.CreateAccount("user", "password", AccountTypeConstants.UserType));

        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
}
