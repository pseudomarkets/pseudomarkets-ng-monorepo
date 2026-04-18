using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Filters;
using PseudoMarkets.Shared.Authorization.Interfaces;
using PseudoMarkets.Shared.Authorization.Models;

namespace PseudoMarkets.MarketData.Tests.Authorization;

[TestFixture]
public class RequireIdentityActionFilterTests
{
    private Mock<IIdentityAuthorizationClient> _identityAuthorizationClient = null!;
    private RequireIdentityActionFilter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _identityAuthorizationClient = new Mock<IIdentityAuthorizationClient>();
        _sut = new RequireIdentityActionFilter(
            "VIEW_MARKET_DATA",
            _identityAuthorizationClient.Object,
            Mock.Of<ILogger<RequireIdentityActionFilter>>());
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnUnauthorized_WhenAuthorizationHeaderIsMissing()
    {
        var context = CreateContext();

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldReturnForbidden_WhenIdentityProviderRejectsToken()
    {
        _identityAuthorizationClient
            .Setup(x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationDecision.Forbidden("Unauthorized"));

        var context = CreateContext("Bearer token");

        await _sut.OnAuthorizationAsync(context);

        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task OnAuthorizationAsync_ShouldAllowRequest_WhenIdentityProviderApprovesToken()
    {
        _identityAuthorizationClient
            .Setup(x => x.AuthorizeAsync("token", "VIEW_MARKET_DATA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationDecision.Authorized());

        var context = CreateContext("Bearer token");

        await _sut.OnAuthorizationAsync(context);

        context.Result.ShouldBeNull();
    }

    private static AuthorizationFilterContext CreateContext(string? authorizationHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
