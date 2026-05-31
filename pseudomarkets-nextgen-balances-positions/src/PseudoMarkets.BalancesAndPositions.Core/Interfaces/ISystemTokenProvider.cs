namespace PseudoMarkets.BalancesAndPositions.Core.Interfaces;

public interface ISystemTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
