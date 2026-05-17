namespace PseudoMarkets.OrderExecution.Core.Models;

public sealed record MarketHoursEvaluationResult(bool IsMarketOpen, string? QueueReason);
