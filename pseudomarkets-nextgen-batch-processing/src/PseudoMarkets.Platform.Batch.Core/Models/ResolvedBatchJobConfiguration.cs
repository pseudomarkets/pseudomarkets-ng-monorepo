namespace PseudoMarkets.Platform.Batch.Core.Models;

public sealed record ResolvedBatchJobConfiguration(
    bool Enabled,
    string CronExpression,
    string Queue,
    bool DisableConcurrentExecution,
    string TimeZoneId);
