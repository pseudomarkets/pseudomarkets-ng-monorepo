using PseudoMarkets.Platform.Batch.Core.Interfaces;

namespace PseudoMarkets.Platform.Batch.Tests;

internal sealed class FakeBatchJob : IBatchJob
{
    public static int ExecutionCount { get; private set; }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        ExecutionCount = 0;
    }
}

internal sealed class SecondFakeBatchJob : IBatchJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

internal sealed class RecordingBatchJobLockProvider : IBatchJobLockProvider
{
    public List<string> AcquiredJobNames { get; } = [];

    public IDisposable Acquire(string jobName, TimeSpan timeout)
    {
        AcquiredJobNames.Add(jobName);
        return new NullDisposable();
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
