namespace PseudoMarkets.Platform.Batch.Host.Constants;

internal static class QueuedOrderExecutionConstants
{
    public const string PendingStatus = "Pending";
    public const string InProgressStatus = "InProgress";
    public const string SucceededStatus = "Succeeded";
    public const string FailedStatus = "Failed";
    public const int FailureMessageMaxLength = 512;
}
