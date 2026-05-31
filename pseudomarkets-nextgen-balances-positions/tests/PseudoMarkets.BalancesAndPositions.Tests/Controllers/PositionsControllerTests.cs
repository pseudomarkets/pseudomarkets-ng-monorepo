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
public sealed class PositionsControllerTests
{
    private Mock<IPositionQueryService> _positionQueryService = null!;
    private PositionsController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _positionQueryService = new Mock<IPositionQueryService>();
        _sut = new PositionsController(_positionQueryService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Test]
    public async Task Post_ShouldReturnOk_WhenSystemTokenRequestsDifferentUser()
    {
        var request = new PositionQueryRequest { UserId = 1_000_000_002, View = PositionView.All };
        var response = new PositionQueryResponse
        {
            RequestedUserId = request.UserId,
            View = PositionView.All,
            Positions = [],
            Warnings = []
        };

        SetAuthorizedContext(1_000_000_001, "SYSTEM");
        _positionQueryService
            .Setup(service => service.GetPositionsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.Post(request, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(response);
    }

    [Test]
    public void Post_ShouldThrowForbidden_WhenUserTokenRequestsDifferentUser()
    {
        var request = new PositionQueryRequest { UserId = 1_000_000_002, View = PositionView.Unsettled };

        SetAuthorizedContext(1_000_000_001, "USER");

        Should.ThrowAsync<BalancesAndPositionsForbiddenException>(() => _sut.Post(request, CancellationToken.None));
    }

    [Test]
    public void Post_ShouldThrowValidation_WhenViewEnumValueIsInvalid()
    {
        var request = new PositionQueryRequest
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
