namespace PseudoMarkets.BalancesAndPositions.Contracts.Responses;

public class QueryWarningResponse
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Symbol { get; init; }
}
