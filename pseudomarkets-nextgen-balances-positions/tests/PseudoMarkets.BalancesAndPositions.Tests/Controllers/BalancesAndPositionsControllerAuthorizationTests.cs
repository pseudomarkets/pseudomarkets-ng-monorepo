using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using PseudoMarkets.BalancesAndPositions.Service.Controllers;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using Shouldly;

namespace PseudoMarkets.BalancesAndPositions.Tests.Controllers;

[TestFixture]
public sealed class BalancesAndPositionsControllerAuthorizationTests
{
    [Test]
    public void ReadControllers_ShouldRequireViewTransactionsAuthorization()
    {
        var controllerTypes = new[]
        {
            typeof(BalancesController),
            typeof(PositionsController)
        };

        foreach (var controllerType in controllerTypes)
        {
            var controllerAttribute = controllerType.GetCustomAttribute<AuthorizeWithIdentityServer>(inherit: true);
            controllerAttribute.ShouldNotBeNull();
            controllerAttribute.Arguments.ShouldNotBeNull();
            controllerAttribute.Arguments.Single().ShouldBe(PlatformAuthorizationActions.ViewTransactions);

            var endpointMethods = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .ToList();

            endpointMethods.Count.ShouldBeGreaterThan(0);
        }
    }
}
