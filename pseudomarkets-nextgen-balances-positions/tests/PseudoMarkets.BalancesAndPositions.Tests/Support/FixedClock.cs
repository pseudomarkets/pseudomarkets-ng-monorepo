using PseudoMarkets.BalancesAndPositions.Core.Models;

namespace PseudoMarkets.BalancesAndPositions.Tests.Support;

public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}
