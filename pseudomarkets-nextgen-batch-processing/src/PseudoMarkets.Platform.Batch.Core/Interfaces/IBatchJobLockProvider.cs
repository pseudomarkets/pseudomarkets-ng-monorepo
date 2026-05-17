namespace PseudoMarkets.Platform.Batch.Core.Interfaces;

public interface IBatchJobLockProvider
{
    IDisposable Acquire(string jobName, TimeSpan timeout);
}
