namespace PseudoMarkets.BalancesAndPositions.Core.Exceptions;

public class BalancesAndPositionsDependencyException : Exception
{
    public BalancesAndPositionsDependencyException(string message)
        : base(message)
    {
    }

    public BalancesAndPositionsDependencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
