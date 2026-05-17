namespace PseudoMarkets.Platform.Batch.Core.Configuration;

public sealed class BatchJobRegistrationOptions
{
    public string Queue { get; set; } = "default";
    public bool DisableConcurrentExecution { get; set; } = true;
    public string TimeZoneId { get; set; } = "UTC";
}
