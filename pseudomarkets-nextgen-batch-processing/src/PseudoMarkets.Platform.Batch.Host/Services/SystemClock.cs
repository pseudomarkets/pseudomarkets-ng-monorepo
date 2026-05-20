using PseudoMarkets.Platform.Batch.Host.Interfaces;

namespace PseudoMarkets.Platform.Batch.Host.Services;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
