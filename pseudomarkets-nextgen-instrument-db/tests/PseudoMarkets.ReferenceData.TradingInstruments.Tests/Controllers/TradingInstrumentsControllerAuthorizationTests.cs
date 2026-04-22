using System.Reflection;
using NUnit.Framework;
using PseudoMarkets.ReferenceData.TradingInstruments.Service.Controllers;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using Shouldly;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Tests.Controllers;

[TestFixture]
public sealed class TradingInstrumentsControllerAuthorizationTests
{
    [Test]
    public void GetBySymbol_RequiresViewMarketDataAction()
    {
        GetRequiredAction(nameof(TradingInstrumentsController.GetBySymbol))
            .ShouldBe(PlatformAuthorizationActions.ViewMarketData);
    }

    [Test]
    public void Create_RequiresUpdateInstrumentsAction()
    {
        GetRequiredAction(nameof(TradingInstrumentsController.Create))
            .ShouldBe(PlatformAuthorizationActions.UpdateInstruments);
    }

    [Test]
    public void UpdateClosingPrice_RequiresUpdateInstrumentsAction()
    {
        GetRequiredAction(nameof(TradingInstrumentsController.UpdateClosingPrice))
            .ShouldBe(PlatformAuthorizationActions.UpdateInstruments);
    }

    private static string GetRequiredAction(string methodName)
    {
        var attribute = typeof(TradingInstrumentsController)
            .GetMethod(methodName)!
            .GetCustomAttributes<AuthorizeWithIdentityServer>()
            .Single();

        attribute.Arguments.ShouldNotBeNull();

        return attribute.Arguments.Single().ShouldBeOfType<string>();
    }
}
