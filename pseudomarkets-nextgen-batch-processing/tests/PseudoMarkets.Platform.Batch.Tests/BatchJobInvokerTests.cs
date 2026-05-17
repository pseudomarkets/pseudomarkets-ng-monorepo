using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PseudoMarkets.Platform.Batch.Core.Configuration;
using PseudoMarkets.Platform.Batch.Core.Interfaces;
using PseudoMarkets.Platform.Batch.Core.Models;
using PseudoMarkets.Platform.Batch.Core.Services;
using Shouldly;

namespace PseudoMarkets.Platform.Batch.Tests;

[TestFixture]
public sealed class BatchJobInvokerTests
{
    [SetUp]
    public void SetUp()
    {
        FakeBatchJob.Reset();
    }

    [Test]
    public async Task ExecuteRecurringJobAsync_ShouldAcquireDistributedLock_WhenConcurrencyProtectionIsEnabled()
    {
        var services = new ServiceCollection();
        services.AddScoped<FakeBatchJob>();
        services.AddScoped<IBatchJob, FakeBatchJob>();
        using var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var lockProvider = new RecordingBatchJobLockProvider();
        var registry = new BatchJobRegistry(
        [
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "* * * * *", "default", true, "UTC")
        ]);
        var resolver = new BatchJobConfigurationResolver(Options.Create(new BatchProcessingConfiguration()));
        var sut = new BatchJobInvoker(scopeFactory, registry, lockProvider, resolver, NullLogger<BatchJobInvoker>.Instance);

        await sut.ExecuteRecurringJobAsync("job-1", CancellationToken.None);

        lockProvider.AcquiredJobNames.ShouldContain("job-1");
        FakeBatchJob.ExecutionCount.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteRecurringJobAsync_ShouldSkipDistributedLock_WhenConcurrencyProtectionIsDisabledByConfiguration()
    {
        var services = new ServiceCollection();
        services.AddScoped<FakeBatchJob>();
        services.AddScoped<IBatchJob, FakeBatchJob>();
        using var serviceProvider = services.BuildServiceProvider();

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var lockProvider = new RecordingBatchJobLockProvider();
        var registry = new BatchJobRegistry(
        [
            new BatchJobDefinition(typeof(FakeBatchJob), "job-1", "* * * * *", "default", true, "UTC")
        ]);
        var resolver = new BatchJobConfigurationResolver(Options.Create(new BatchProcessingConfiguration
        {
            Jobs = new Dictionary<string, BatchJobConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["job-1"] = new() { DisableConcurrentExecution = false }
            }
        }));
        var sut = new BatchJobInvoker(scopeFactory, registry, lockProvider, resolver, NullLogger<BatchJobInvoker>.Instance);

        await sut.ExecuteRecurringJobAsync("job-1", CancellationToken.None);

        lockProvider.AcquiredJobNames.ShouldBeEmpty();
        FakeBatchJob.ExecutionCount.ShouldBe(1);
    }
}
