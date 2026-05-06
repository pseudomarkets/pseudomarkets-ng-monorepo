namespace PseudoMarkets.OrderExecution.Core.Exceptions;

public sealed class OrderExecutionDependencyException : OrderExecutionException
{
    public OrderExecutionDependencyException(string code, string message, Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}
