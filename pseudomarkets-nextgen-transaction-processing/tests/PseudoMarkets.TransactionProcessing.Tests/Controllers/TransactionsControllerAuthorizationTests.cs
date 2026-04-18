using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.TransactionProcessing.Service.Controllers;

namespace PseudoMarkets.TransactionProcessing.Tests.Controllers;

[TestFixture]
public class TransactionsControllerAuthorizationTests
{
    [Test]
    public void TransactionWriteControllers_ShouldRequireUpdateTransactionsAuthorization()
    {
        var controllerTypes = new[]
        {
            typeof(TransactionsController),
            typeof(CashTransactionsController)
        };

        foreach (var controllerType in controllerTypes)
        {
            var controllerAttribute = controllerType.GetCustomAttribute<AuthorizeWithIdentityServer>(inherit: true);
            controllerAttribute.ShouldNotBeNull();
            controllerAttribute.Arguments.ShouldNotBeNull();
            controllerAttribute.Arguments.Single().ShouldBe(PlatformAuthorizationActions.UpdateTransactions);

            var endpointMethods = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .ToList();

            endpointMethods.Count.ShouldBeGreaterThan(0);
        }
    }
}
