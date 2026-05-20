namespace PseudoMarkets.Platform.Batch.Host.Interfaces;

internal interface IQueuedOrderExecutionTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
