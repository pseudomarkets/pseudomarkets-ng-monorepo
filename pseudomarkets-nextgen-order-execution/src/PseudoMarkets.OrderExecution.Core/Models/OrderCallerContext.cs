namespace PseudoMarkets.OrderExecution.Core.Models;

public sealed record OrderCallerContext(long AuthorizedUserId, string TokenType);
