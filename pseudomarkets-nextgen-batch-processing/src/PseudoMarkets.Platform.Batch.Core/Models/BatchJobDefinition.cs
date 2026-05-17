namespace PseudoMarkets.Platform.Batch.Core.Models;

public sealed record BatchJobDefinition(
    Type JobType,
    string JobName,
    string DefaultCronExpression,
    string DefaultQueue,
    bool DisableConcurrentExecution,
    string DefaultTimeZoneId);
