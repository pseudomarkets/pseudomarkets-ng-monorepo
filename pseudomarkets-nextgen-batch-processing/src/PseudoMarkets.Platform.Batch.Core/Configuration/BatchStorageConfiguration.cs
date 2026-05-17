namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchStorageConfiguration
{
    public string SchemaName { get; set; } = "hangfire";
    public int QueuePollIntervalSeconds { get; set; } = 15;
    public int InvisibilityTimeoutMinutes { get; set; } = 30;
}
