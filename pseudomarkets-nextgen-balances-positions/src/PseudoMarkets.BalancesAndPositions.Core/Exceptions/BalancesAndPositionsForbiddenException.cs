namespace PseudoMarkets.BalancesAndPositions.Core.Exceptions;

public class BalancesAndPositionsForbiddenException : Exception
{
    public BalancesAndPositionsForbiddenException(string message)
        : base(message)
    {
    }
}
