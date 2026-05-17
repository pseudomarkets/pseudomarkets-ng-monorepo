namespace PseudoMarkets.Platform.Batch.Core.Interfaces;

public interface IBatchJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
