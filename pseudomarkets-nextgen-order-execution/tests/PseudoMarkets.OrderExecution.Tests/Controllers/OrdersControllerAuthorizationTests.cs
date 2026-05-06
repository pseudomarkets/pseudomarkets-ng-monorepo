using System.Reflection;
using NUnit.Framework;
using PseudoMarkets.OrderExecution.Service.Controllers;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using Shouldly;

namespace PseudoMarkets.OrderExecution.Tests.Controllers;

[TestFixture]
public sealed class OrdersControllerAuthorizationTests
{
    [Test]
    public void Controller_ShouldRequireExecuteTradesAction()
    {
        var attribute = typeof(OrdersController)
            .GetCustomAttributes<AuthorizeWithIdentityServer>()
            .Single();

        attribute.Arguments.ShouldNotBeNull();
        attribute.Arguments.Single().ShouldBe(PlatformAuthorizationActions.ExecuteTrades);
    }
}
