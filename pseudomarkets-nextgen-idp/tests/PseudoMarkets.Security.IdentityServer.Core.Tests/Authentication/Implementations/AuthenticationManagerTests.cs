using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Implementations;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Database.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Core.Models;

namespace PseudoMarkets.Security.IdentityServer.Core.Tests.Authentication.Implementations;

[TestFixture]
public class AuthenticationManagerTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<ILogger<AuthenticationManager>> _logger = null!;
    private JwtConfiguration _jwtConfiguration = null!;

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
    }

    [Test]
    public void Authenticate_ShouldReturnTokenWithRolesClaim_WhenCredentialsAreValid()
    {
        var sut = CreateSut();
        _accountRepository.Setup(x => x.GetAccount("user")).Returns(new Account
        {
            LoginId = "user",
            HashedPassword = sut.HashPassword("correct-password"),
            Roles = ["VIEW_BALANCES", "UPDATE_BALANCES"]
        });

        var result = sut.Authenticate("user", "correct-password");

        result.Success.ShouldBeTrue();
        result.Token.ShouldNotBeNullOrWhiteSpace();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.FirstOrDefault(x => x.Type == "roles")?.Value.ShouldBe("VIEW_BALANCES,UPDATE_BALANCES");
        token.Claims.Any(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ShouldBeFalse();
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

    private AuthenticationManager CreateSut() => new(_jwtConfiguration, _accountRepository.Object, _logger.Object);
}
