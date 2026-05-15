using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PseudoMarkets.Shared.ServiceHelpers;

public static class OperationalEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPseudoMarketsOperationalEndpoints<TMarker>(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/info", () => Results.Ok(ApplicationInfoProvider.GetInfo<TMarker>()))
            .AllowAnonymous();

        endpoints.MapHealthChecks(
                "/health",
                new HealthCheckOptions
                {
                    ResponseWriter = HealthCheckJsonResponseWriter.WriteAsync
                })
            .AllowAnonymous();

        return endpoints;
    }
}
