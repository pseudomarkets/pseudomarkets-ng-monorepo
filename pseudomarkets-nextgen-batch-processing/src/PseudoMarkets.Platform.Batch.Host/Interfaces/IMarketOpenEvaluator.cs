namespace PseudoMarkets.Platform.Batch.Host.Interfaces;

internal interface IMarketOpenEvaluator
{
    Task<bool> IsMarketOpenAsync(CancellationToken cancellationToken);
}
