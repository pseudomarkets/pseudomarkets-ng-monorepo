using PseudoMarkets.OrderExecution.Core.Interfaces;

namespace PseudoMarkets.OrderExecution.Tests.Support;

internal sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 5, 6, 15, 20, 30, DateTimeKind.Utc);
}
