using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.MarketData.Service.Controllers;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;

namespace PseudoMarkets.MarketData.Tests.Authorization;

[TestFixture]
public class MarketDataControllerAuthorizationTests
{
    [Test]
    public void QuoteEndpoints_ShouldRequireViewMarketDataAuthorization()
    {
        var endpointMethods = typeof(MarketDataController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
            .ToList();

        endpointMethods.Count.ShouldBe(3);

        foreach (var method in endpointMethods)
        {
            var attribute = method.GetCustomAttribute<RequireIdentityActionAttribute>(inherit: true);
            attribute.ShouldNotBeNull();
            attribute.Arguments.ShouldNotBeNull();
            attribute.Arguments.Single().ShouldBe(PlatformAuthorizationActions.ViewMarketData);
        }
    }
}
