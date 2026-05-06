namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface ISystemTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
