namespace PseudoMarkets.OrderExecution.Contracts.Enums;

public enum OrderStatus
{
    Accepted = 1,
    Filled = 2,
    Rejected = 3,
    TransactionPostingFailed = 4,
    Queued = 5
}
