namespace PseudoMarkets.OrderExecution.Core.Exceptions;

public sealed class OrderExecutionAuthorizationException : OrderExecutionException
{
    public OrderExecutionAuthorizationException(string code, string message)
        : base(code, message)
    {
    }
}
