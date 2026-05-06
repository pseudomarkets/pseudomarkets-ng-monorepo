namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
