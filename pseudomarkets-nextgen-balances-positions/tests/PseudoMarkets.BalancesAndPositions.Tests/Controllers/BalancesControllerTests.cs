using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Contracts.Enums;
using PseudoMarkets.BalancesAndPositions.Contracts.Requests;
using PseudoMarkets.BalancesAndPositions.Contracts.Responses;
using PseudoMarkets.BalancesAndPositions.Core.Exceptions;
using PseudoMarkets.BalancesAndPositions.Core.Interfaces;
using PseudoMarkets.BalancesAndPositions.Service.Controllers;
using PseudoMarkets.Shared.Authorization.Models;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Controllers;

[TestFixture]
public sealed class BalancesControllerTests
{
    private Mock<IBalanceQueryService> _balanceQueryService = null!;
    private BalancesController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _balanceQueryService = new Mock<IBalanceQueryService>();
        _sut = new BalancesController(_balanceQueryService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Test]
    public async Task Post_ShouldReturnOk_WhenUserTokenMatchesRequestedUserId()
    {
        var request = new BalanceQueryRequest { UserId = 1_000_000_001, View = PositionView.All };
        var response = new BalanceQueryResponse
        {
            RequestedUserId = request.UserId,
            View = PositionView.All,
            AggregateCashBalance = 100m,
            SettledCashBalance = 60m,
            UnsettledCashBalance = 40m
        };

        SetAuthorizedContext(request.UserId, "USER");
        _balanceQueryService
            .Setup(service => service.GetBalanceAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Post(request, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(response);
    }

    [Test]
    public void Post_ShouldThrowForbidden_WhenUserTokenRequestsDifferentUser()
    {
        var request = new BalanceQueryRequest { UserId = 1_000_000_002, View = PositionView.All };

        SetAuthorizedContext(1_000_000_001, "USER");

        Should.ThrowAsync<BalancesAndPositionsForbiddenException>(() => _sut.Post(request, CancellationToken.None));
    }

    [Test]
    public async Task Post_ShouldAllowSystemTokenToRequestDifferentUser()
    {
        var request = new BalanceQueryRequest { UserId = 1_000_000_002, View = PositionView.Settled };
        var response = new BalanceQueryResponse
        {
            RequestedUserId = request.UserId,
            View = PositionView.Settled,
            AggregateCashBalance = null,
            SettledCashBalance = 75m,
            UnsettledCashBalance = null
        };

        SetAuthorizedContext(1_000_000_001, "SYSTEM");
        _balanceQueryService
            .Setup(service => service.GetBalanceAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Post(request, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(response);
    }

    [Test]
    public void Post_ShouldThrowValidation_WhenViewEnumValueIsInvalid()
    {
        var request = new BalanceQueryRequest
        {
            UserId = 1_000_000_001,
            View = (PositionView)999
        };

        SetAuthorizedContext(1_000_000_001, "USER");

        Should.ThrowAsync<BalancesAndPositionsValidationException>(() => _sut.Post(request, CancellationToken.None));
    }

    private void SetAuthorizedContext(long userId, string tokenType)
    {
        _sut.HttpContext.Items[AuthorizedIdentityContext.UserIdItemKey] = userId;
        _sut.HttpContext.Items[AuthorizedIdentityContext.TokenTypeItemKey] = tokenType;
    }
}
