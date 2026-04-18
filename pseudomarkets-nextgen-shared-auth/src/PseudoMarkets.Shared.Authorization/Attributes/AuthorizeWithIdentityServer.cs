using Microsoft.AspNetCore.Mvc;
using PseudoMarkets.Shared.Authorization.Filters;

namespace PseudoMarkets.Shared.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeWithIdentityServer : TypeFilterAttribute
{
    public AuthorizeWithIdentityServer(string requiredAction)
        : base(typeof(RequireIdentityActionFilter))
    {
        Arguments = [requiredAction];
    }
}
