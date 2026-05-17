namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchJobConfiguration
{
    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = string.Empty;
    public string Queue { get; set; } = "default";
    public bool DisableConcurrentExecution { get; set; } = true;
    public string TimeZoneId { get; set; } = "UTC";
}
