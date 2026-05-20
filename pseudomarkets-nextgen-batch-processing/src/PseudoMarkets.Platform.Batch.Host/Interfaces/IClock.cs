namespace PseudoMarkets.Platform.Batch.Host.Interfaces;

internal interface IClock
{
    DateTime UtcNow { get; }
}
