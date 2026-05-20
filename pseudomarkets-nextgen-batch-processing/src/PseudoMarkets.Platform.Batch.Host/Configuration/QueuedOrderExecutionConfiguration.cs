namespace PseudoMarkets.Platform.Batch.Host.Configuration;

public sealed class QueuedOrderExecutionConfiguration
{
    public const string SectionName = "QueuedOrderExecution";
    public string IdentityServerBaseUrl { get; set; } = "http://localhost:5051";
    public string OrderExecutionBaseUrl { get; set; } = "http://localhost:8084";
    public string SystemAccountLoginId { get; set; } = string.Empty;
    public string SystemAccountPassword { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int TokenRefreshBufferSeconds { get; set; } = 60;
    public int MaxBatchSize { get; set; } = 1000;
}
