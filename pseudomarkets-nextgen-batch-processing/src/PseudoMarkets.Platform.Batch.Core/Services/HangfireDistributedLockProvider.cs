using Hangfire;
using PseudoMarkets.Platform.Batch.Core.Interfaces;

namespace PseudoMarkets.Platform.Batch.Core.Services;

public sealed class HangfireDistributedLockProvider : IBatchJobLockProvider
{
    private readonly JobStorage _jobStorage;

    public HangfireDistributedLockProvider(JobStorage jobStorage)
    {
        _jobStorage = jobStorage;
    }

    public IDisposable Acquire(string jobName, TimeSpan timeout)
    {
        var connection = _jobStorage.GetConnection();

        try
        {
            var distributedLock = connection.AcquireDistributedLock($"batch-job:{jobName}", timeout);
            return new DistributedLockLease(connection, distributedLock);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private sealed class DistributedLockLease : IDisposable
    {
        private readonly IDisposable _connection;
        private readonly IDisposable _distributedLock;

        public DistributedLockLease(IDisposable connection, IDisposable distributedLock)
        {
            _connection = connection;
            _distributedLock = distributedLock;
        }

        public void Dispose()
        {
            _distributedLock.Dispose();
            _connection.Dispose();
        }
    }
}
