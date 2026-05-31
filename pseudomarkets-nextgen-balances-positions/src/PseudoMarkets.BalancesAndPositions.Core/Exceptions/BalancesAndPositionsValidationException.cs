namespace PseudoMarkets.BalancesAndPositions.Core.Exceptions;

public class BalancesAndPositionsValidationException : Exception
{
    public BalancesAndPositionsValidationException(string message)
        : base(message)
    {
    }
}
