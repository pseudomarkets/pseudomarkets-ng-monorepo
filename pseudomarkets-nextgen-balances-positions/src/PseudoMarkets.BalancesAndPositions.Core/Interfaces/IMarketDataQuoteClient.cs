using PseudoMarkets.BalancesAndPositions.Core.Models;

namespace PseudoMarkets.BalancesAndPositions.Core.Interfaces;

public interface IMarketDataQuoteClient
{
    Task<QuoteLookupResult> GetQuoteAsync(string symbol, CancellationToken cancellationToken);
}
