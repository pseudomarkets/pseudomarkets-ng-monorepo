namespace PseudoMarkets.BalancesAndPositions.Core.Exceptions;

public class BalancesAndPositionsNotFoundException : Exception
{
    public BalancesAndPositionsNotFoundException(string message)
        : base(message)
    {
    }
}
