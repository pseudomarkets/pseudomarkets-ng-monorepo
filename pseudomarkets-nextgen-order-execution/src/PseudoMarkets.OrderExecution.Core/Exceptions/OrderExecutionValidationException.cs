namespace PseudoMarkets.OrderExecution.Core.Exceptions;

public sealed class OrderExecutionValidationException : OrderExecutionException
{
    public OrderExecutionValidationException(string code, string message)
        : base(code, message)
    {
    }
}
