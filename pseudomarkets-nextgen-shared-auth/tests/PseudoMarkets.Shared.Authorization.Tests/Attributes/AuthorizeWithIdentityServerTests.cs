using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Shared.Authorization.Attributes;
using PseudoMarkets.Shared.Authorization.Filters;

namespace PseudoMarkets.Shared.Authorization.Tests.Attributes;

[TestFixture]
public class AuthorizeWithIdentityServerTests
{
    [Test]
    public void Constructor_ShouldConfigureTypeFilterWithRequiredAction()
    {
        var attribute = new AuthorizeWithIdentityServer("VIEW_MARKET_DATA");

        (attribute is TypeFilterAttribute).ShouldBeTrue();
        attribute.ImplementationType.ShouldBe(typeof(RequireIdentityActionFilter));
        attribute.Arguments.ShouldNotBeNull();
        attribute.Arguments.Single().ShouldBe("VIEW_MARKET_DATA");
    }

    [Test]
    public void AttributeUsage_ShouldAllowMultipleAndInheritedUsage()
    {
        var usage = typeof(AuthorizeWithIdentityServer)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Method);
        usage.AllowMultiple.ShouldBeTrue();
        usage.Inherited.ShouldBeTrue();
    }
}
