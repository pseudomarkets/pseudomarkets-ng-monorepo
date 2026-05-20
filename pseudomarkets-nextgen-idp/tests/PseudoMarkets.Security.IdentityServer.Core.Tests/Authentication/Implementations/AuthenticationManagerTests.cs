using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;
using Shouldly;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Authentication.Implementations;

[TestFixture]
public class AuthenticationManagerTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<ILogger<AuthenticationManager>> _logger = null!;
    private JwtConfiguration _jwtConfiguration = null!;
    private IdentitySecurityConfiguration _identitySecurityConfiguration = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _logger = new Mock<ILogger<AuthenticationManager>>();
        _jwtConfiguration = new JwtConfiguration
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Key = "test-signing-key-1234567890-abcdef"
        };
        _identitySecurityConfiguration = new IdentitySecurityConfiguration
        {
            FailedLoginAttemptLimit = 5,
            LockoutDurationMinutes = 15
        };
    }

    [TestCase("", "password")]
    [TestCase("user", "")]
    [TestCase(" ", "password")]
    public void Authenticate_ShouldReturnFailure_WhenCredentialsAreBlank(string loginId, string password)
    {
        var sut = CreateSut();

        var result = sut.Authenticate(loginId, password);

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
    }

    [Test]
    public void Authenticate_ShouldReturnFailure_WhenAccountDoesNotExist()
    {
        _accountRepository.Setup(x => x.GetAccount("missing-user")).Returns((Account?)null);
        var sut = CreateSut();

        var result = sut.Authenticate("missing-user", "password");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
    }

    [Test]
    public void Authenticate_ShouldReturnFailure_WhenPasswordDoesNotMatch()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"]
        });

        var result = sut.Authenticate("user", "wrong-password");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
        _accountRepository.Verify(x => x.UpdateAccount(It.Is<Account>(account =>
            account.LoginId == "user" &&
            account.FailedLoginAttempts == 1 &&
            account.LockoutUntilUtc == null)), Times.Once);
    }

    [Test]
    public void Authenticate_ShouldReturnTokenWithRolesClaim_WhenCredentialsAreValid()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES", "UPDATE_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IsActive = true
        });

        var result = sut.Authenticate("user", "correct-password");

        result.Success.ShouldBeTrue();
        result.Token.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotContain("..");
        result.RefreshTokenExpires.ShouldBeGreaterThan(result.Expires.AddSeconds(-1));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.FirstOrDefault(x => x.Type == "roles")?.Value.ShouldBe("VIEW_BALANCES,UPDATE_BALANCES");
        token.Claims.FirstOrDefault(x => x.Type == "account_type")?.Value.ShouldBe("USER");
        token.Claims.Any(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ShouldBeFalse();

        _accountRepository.Verify(x => x.CreateRefreshToken(It.Is<RefreshTokenRecord>(record =>
            record.LoginId == "user" &&
            record.UserId == 1_234_567_890L &&
            record.AccountType == "USER" &&
            !record.IsConsumed &&
            !record.IsRevoked &&
            !string.IsNullOrWhiteSpace(record.SecretHash))), Times.Once);
    }

    [Test]
    public void Authenticate_ShouldReturnFailure_WhenAccountIsInactive()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IsActive = false
        });

        var result = sut.Authenticate("user", "correct-password");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
        result.RefreshToken.ShouldBeEmpty();
    }

    [Test]
    public void Authenticate_ShouldReturnFailure_WhenAccountIsLockedOut()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IsActive = true,
            LockoutUntilUtc = DateTime.UtcNow.AddMinutes(10)
        });

        var result = sut.Authenticate("user", "correct-password");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
        _accountRepository.Verify(x => x.CreateRefreshToken(It.IsAny<RefreshTokenRecord>()), Times.Never);
    }

    [Test]
    public void Authenticate_ShouldLockAccount_WhenFailedAttemptLimitIsReached()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IsActive = true,
            FailedLoginAttempts = 4
        });

        var result = sut.Authenticate("user", "wrong-password");

        result.Success.ShouldBeFalse();
        _accountRepository.Verify(x => x.UpdateAccount(It.Is<Account>(account =>
            account.LoginId == "user" &&
            account.FailedLoginAttempts == 0 &&
            account.LockoutUntilUtc.HasValue)), Times.Once);
    }

    [Test]
    public void Refresh_ShouldRotateRefreshToken_WhenSubmittedTokenIsValid()
    {
        var sut = CreateSut();
        RefreshTokenRecord? issuedRefreshTokenRecord = null;
        RefreshTokenRecord? updatedRefreshTokenRecord = null;
        _accountRepository.Setup(x => x.CreateRefreshToken(It.IsAny<RefreshTokenRecord>()))
            .Callback<RefreshTokenRecord>(created =>
            {
                if (issuedRefreshTokenRecord is null)
                {
                    issuedRefreshTokenRecord = created;
                }
            });
        _accountRepository.Setup(x => x.UpdateRefreshToken(It.IsAny<RefreshTokenRecord>()))
            .Callback<RefreshTokenRecord>(updated => updatedRefreshTokenRecord = updated);
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "SYSTEM",
            IsActive = true
        });

        var authenticateResult = sut.Authenticate("user", "correct-password");
        issuedRefreshTokenRecord.ShouldNotBeNull();
        _accountRepository.Invocations.Clear();
        _accountRepository.Setup(x => x.GetRefreshToken(issuedRefreshTokenRecord!.TokenId)).Returns(issuedRefreshTokenRecord);
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES"],
            UserId = 1_234_567_890L,
            AccountType = "SYSTEM",
            IsActive = true
        });
        _accountRepository.Setup(x => x.TryConsumeRefreshToken(issuedRefreshTokenRecord.TokenId, issuedRefreshTokenRecord.Generation))
            .Returns(true);

        var refreshResult = sut.Refresh(authenticateResult.RefreshToken);

        refreshResult.Success.ShouldBeTrue();
        refreshResult.Token.ShouldNotBe(authenticateResult.Token);
        refreshResult.RefreshToken.ShouldNotBe(authenticateResult.RefreshToken);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(refreshResult.Token);
        token.Claims.FirstOrDefault(x => x.Type == "account_type")?.Value.ShouldBe("SYSTEM");

        updatedRefreshTokenRecord.ShouldBeNull();
        _accountRepository.Verify(x => x.TryConsumeRefreshToken(issuedRefreshTokenRecord.TokenId, issuedRefreshTokenRecord.Generation), Times.Once);
        _accountRepository.Verify(x => x.CreateRefreshToken(It.IsAny<RefreshTokenRecord>()), Times.Once);
    }

    [Test]
    public void Refresh_ShouldReturnFailure_WhenRefreshTokenHasAlreadyBeenConsumed()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetRefreshToken("token-id")).Returns(new RefreshTokenRecord
        {
            TokenId = "token-id",
            SecretHash = ComputeSecretHash("secret"),
            LoginId = "user",
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(55),
            IsConsumed = true
        });

        var result = sut.Refresh("token-id.secret");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
    }

    [Test]
    public void Refresh_ShouldReturnFailure_WhenRefreshTokenWasAlreadyConsumedConcurrently()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetRefreshToken("token-id")).Returns(new RefreshTokenRecord
        {
            TokenId = "token-id",
            SecretHash = ComputeSecretHash("secret"),
            LoginId = "user",
            UserId = 1_234_567_890L,
            AccountType = "USER",
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(55),
            Generation = 7
        });
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            UserId = 1_234_567_890L,
            HashedPassword = sut.HashPassword("correct-password"),
            AccountType = "USER",
            Roles = ["VIEW_BALANCES"],
            IsActive = true
        });
        _accountRepository.Setup(x => x.TryConsumeRefreshToken("token-id", 7)).Returns(false);

        var result = sut.Refresh("token-id.secret");

        result.Success.ShouldBeFalse();
        result.Token.ShouldBeEmpty();
    }

    [Test]
    public void Authenticate_ShouldRethrowIdentityDependencyException()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new IdentityDependencyException("boom"));
        var sut = CreateSut();

        Should.Throw<IdentityDependencyException>(() => sut.Authenticate("user", "password"));
    }

    [Test]
    public void Authenticate_ShouldWrapUnexpectedExceptions()
    {
        _accountRepository.Setup(x => x.GetAccount("user")).Throws(new InvalidOperationException("boom"));
        var sut = CreateSut();

        var ex = Should.Throw<IdentityServiceException>(() => sut.Authenticate("user", "password"));

        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Test]
    public void HashPassword_ShouldReturnSaltAndHashSegments()
    {
        var sut = CreateSut();

        var hashedPassword = sut.HashPassword("password");

        hashedPassword.ShouldContain(':');
        var parts = hashedPassword.Split(':');
        parts.Length.ShouldBe(2);
        Convert.FromBase64String(parts[0]).Length.ShouldBeGreaterThan(0);
        Convert.FromBase64String(parts[1]).Length.ShouldBeGreaterThan(0);
    }

    private AuthenticationManager CreateSut() => new(
        _jwtConfiguration,
        Options.Create(_identitySecurityConfiguration),
        _accountRepository.Object,
        _logger.Object);

    private static string ComputeSecretHash(string secret)
    {
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret)));
    }
}
