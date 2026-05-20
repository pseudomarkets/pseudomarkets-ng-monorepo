using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PseudoMarkets.Security.IdentityServer.Core.Accounts.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authentication.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Authorization.Interfaces;
using PseudoMarkets.Security.IdentityServer.Core.Configuration;
using PseudoMarkets.Security.IdentityServer.Core.Constants;
using PseudoMarkets.Security.IdentityServer.Core.Models;
using PseudoMarkets.Security.IdentityServer.Web.Constants;
using PseudoMarkets.Security.IdentityServer.Web.Contracts;
using PseudoMarkets.Security.IdentityServer.Web.Controllers;
using Shouldly;

namespace PseudoMarkets.Security.IdentityServer.Web.Tests.Controllers;

[TestFixture]
public class IdentityControllerTests
{
    private Mock<IAccountProvisioningManager> _accountProvisioningManager = null!;
    private Mock<IAuthenticationManager> _authenticationManager = null!;
    private Mock<IAuthorizationManager> _authorizationManager = null!;
    private Mock<IPasswordResetManager> _passwordResetManager = null!;
    private Mock<IWebHostEnvironment> _environment = null!;
    private Mock<ILogger<IdentityController>> _logger = null!;
    private IdentitySecurityConfiguration _identitySecurityConfiguration = null!;

    [SetUp]
    public void SetUp()
    {
        _accountProvisioningManager = new Mock<IAccountProvisioningManager>();
        _authenticationManager = new Mock<IAuthenticationManager>();
        _authorizationManager = new Mock<IAuthorizationManager>();
        _passwordResetManager = new Mock<IPasswordResetManager>();
        _environment = new Mock<IWebHostEnvironment>();
        _logger = new Mock<ILogger<IdentityController>>();
        _identitySecurityConfiguration = new IdentitySecurityConfiguration { SystemAccountBypassKey = "test-bypass-key" };
        _environment.Setup(x => x.EnvironmentName).Returns("Production");
    }

    [Test]
    public void Create_ShouldAllowPublicUserCreation_OutsideDevelopment()
    {
        _accountProvisioningManager
            .Setup(x => x.CreateAccount("public-user", "password", AccountTypeConstants.UserType))
            .Returns(new AccountCreationResult(true, "USER account created successfully.", "public-user", AccountTypeConstants.UserType, "00000000-0000-0000-0000-000000000001"));

        var sut = CreateSut();

        var result = sut.Create(new CreateAccountRequest { Username = "public-user", Password = "password" });

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = okResult.Value.ShouldBeOfType<AccountCreationResult>();
        payload.AccountType.ShouldBe(AccountTypeConstants.UserType);
        payload.PasswordResetKey.ShouldBe("00000000-0000-0000-0000-000000000001");
    }

    [Test]
    public void Create_ShouldAllowSystemCreation_InDevelopment()
    {
        _environment.Setup(x => x.EnvironmentName).Returns("Development");
        _accountProvisioningManager
            .Setup(x => x.CreateAccount("system-user", "password", AccountTypeConstants.SystemType))
            .Returns(new AccountCreationResult(true, "SYSTEM account created successfully.", "system-user", AccountTypeConstants.SystemType));

        var sut = CreateSut();

        var result = sut.Create(new CreateAccountRequest { Username = "system-user", Password = "password", AccountType = AccountTypeConstants.SystemType });

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Create_ShouldForbidSystemCreation_OutsideDevelopmentWithoutBypassHeader()
    {
        var sut = CreateSut();

        var result = sut.Create(new CreateAccountRequest { Username = "system-user", Password = "password", AccountType = AccountTypeConstants.SystemType });

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Test]
    public void Create_ShouldAllowSystemCreation_OutsideDevelopmentWithBypassHeader()
    {
        _accountProvisioningManager
            .Setup(x => x.CreateAccount("system-user", "password", AccountTypeConstants.SystemType))
            .Returns(new AccountCreationResult(true, "SYSTEM account created successfully.", "system-user", AccountTypeConstants.SystemType));

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers[HeaderConstants.SystemAccountBypassKeyHeader] = _identitySecurityConfiguration.SystemAccountBypassKey;

        var result = sut.Create(new CreateAccountRequest { Username = "system-user", Password = "password", AccountType = AccountTypeConstants.SystemType });

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Create_ShouldReturnBadRequest_ForUnsupportedAccountType()
    {
        _accountProvisioningManager
            .Setup(x => x.CreateAccount("user", "password", "ADMIN"))
            .Returns(new AccountCreationResult(false, "Unsupported account type.", "user", "ADMIN"));

        var sut = CreateSut();

        var result = sut.Create(new CreateAccountRequest { Username = "user", Password = "password", AccountType = "ADMIN" });

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void Create_ShouldReturnConflict_WhenProvisioningFails()
    {
        _accountProvisioningManager
            .Setup(x => x.CreateAccount("user", "password", AccountTypeConstants.UserType))
            .Returns(new AccountCreationResult(false, "An account with that username already exists.", "user", AccountTypeConstants.UserType));

        var sut = CreateSut();

        var result = sut.Create(new CreateAccountRequest { Username = "user", Password = "password" });

        result.Result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Test]
    public void Authenticate_ShouldReturnOk_WhenAuthenticationSucceeds()
    {
        _authenticationManager
            .Setup(x => x.Authenticate("user", "password"))
            .Returns(new AuthenticationResult(true, "token", DateTime.UtcNow.AddMinutes(1), "refresh", DateTime.UtcNow.AddMinutes(1)));

        var sut = CreateSut();

        var result = sut.Authenticate(new AuthenticateRequest { LoginId = "user", Password = "password" });

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Authenticate_ShouldReturnUnauthorized_WhenAuthenticationFails()
    {
        _authenticationManager
            .Setup(x => x.Authenticate("user", "password"))
            .Returns(new AuthenticationResult(false, string.Empty, DateTime.MinValue));

        var sut = CreateSut();

        var result = sut.Authenticate(new AuthenticateRequest { LoginId = "user", Password = "password" });

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public void Refresh_ShouldReturnOk_WhenRefreshSucceeds()
    {
        _authenticationManager
            .Setup(x => x.Refresh("refresh"))
            .Returns(new AuthenticationResult(true, "token", DateTime.UtcNow.AddMinutes(1), "refresh-2", DateTime.UtcNow.AddMinutes(1)));

        var sut = CreateSut();

        var result = sut.Refresh(new RefreshTokenRequest { RefreshToken = "refresh" });

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Refresh_ShouldReturnUnauthorized_WhenRefreshFails()
    {
        _authenticationManager
            .Setup(x => x.Refresh("refresh"))
            .Returns(new AuthenticationResult(false, string.Empty, DateTime.MinValue));

        var sut = CreateSut();

        var result = sut.Refresh(new RefreshTokenRequest { RefreshToken = "refresh" });

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public void ResetPassword_ShouldReturnBadRequest_WhenFieldsAreBlank()
    {
        var sut = CreateSut();

        var result = sut.ResetPassword(new ResetPasswordRequest
        {
            LoginId = "user",
            PasswordResetKey = "",
            NewPassword = "new-password"
        });

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public void ResetPassword_ShouldReturnOk_WhenResetSucceeds()
    {
        _passwordResetManager
            .Setup(x => x.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password"))
            .Returns(new PasswordResetResult(true, "Password reset successfully.", "user", "00000000-0000-0000-0000-000000000002"));

        var sut = CreateSut();

        var result = sut.ResetPassword(new ResetPasswordRequest
        {
            LoginId = "user",
            PasswordResetKey = "00000000-0000-0000-0000-000000000001",
            NewPassword = "new-password"
        });

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = okResult.Value.ShouldBeOfType<PasswordResetResult>();
        payload.PasswordResetKey.ShouldBe("00000000-0000-0000-0000-000000000002");
    }

    [Test]
    public void ResetPassword_ShouldReturnUnauthorized_WhenResetFails()
    {
        _passwordResetManager
            .Setup(x => x.ResetPassword("user", "00000000-0000-0000-0000-000000000001", "new-password"))
            .Returns(new PasswordResetResult(false, "Password reset failed.", "user", null));

        var sut = CreateSut();

        var result = sut.ResetPassword(new ResetPasswordRequest
        {
            LoginId = "user",
            PasswordResetKey = "00000000-0000-0000-0000-000000000001",
            NewPassword = "new-password"
        });

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Test]
    public void Authorize_ShouldReturnOk_WhenAuthorizationSucceeds()
    {
        _authorizationManager
            .Setup(x => x.Authorize(It.IsAny<AuthorizationRequest>()))
            .Returns(new AuthorizationResult(true, "Authorization Successful", 1, AccountTypeConstants.SystemType));

        var sut = CreateSut();

        var result = sut.Authorize(new AuthorizeRequest { Token = "token", Action = "VIEW_BALANCES" });

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public void Authorize_ShouldReturnForbidden_WhenAuthorizationFails()
    {
        _authorizationManager
            .Setup(x => x.Authorize(It.IsAny<AuthorizationRequest>()))
            .Returns(new AuthorizationResult(false, "Unauthorized", 0, AccountTypeConstants.UserType));

        var sut = CreateSut();

        var result = sut.Authorize(new AuthorizeRequest { Token = "token", Action = "VIEW_BALANCES" });

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Test]
    public void Controller_ShouldApplySensitiveRateLimitingPolicy()
    {
        var attribute = typeof(IdentityController)
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .Single();

        attribute.PolicyName.ShouldBe("identity-sensitive");
    }

    private IdentityController CreateSut()
    {
        var sut = new IdentityController(
            _accountProvisioningManager.Object,
            _authenticationManager.Object,
            _authorizationManager.Object,
            _passwordResetManager.Object,
            _identitySecurityConfiguration,
            _environment.Object,
            _logger.Object);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return sut;
    }
}
