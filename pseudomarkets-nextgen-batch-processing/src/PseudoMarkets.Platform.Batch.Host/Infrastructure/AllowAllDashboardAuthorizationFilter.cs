using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace PseudoMarkets.Platform.Batch.Host.Infrastructure;

internal sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        return true;
    }
}
