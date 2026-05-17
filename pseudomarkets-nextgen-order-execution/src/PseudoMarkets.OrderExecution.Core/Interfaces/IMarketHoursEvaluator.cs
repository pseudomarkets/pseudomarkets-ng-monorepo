using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IMarketHoursEvaluator
{
    Task<MarketHoursEvaluationResult> EvaluateAsync(CancellationToken cancellationToken);
}
