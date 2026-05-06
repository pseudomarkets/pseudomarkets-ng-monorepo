using PseudoMarkets.OrderExecution.Core.Interfaces;

namespace PseudoMarkets.OrderExecution.Core.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
