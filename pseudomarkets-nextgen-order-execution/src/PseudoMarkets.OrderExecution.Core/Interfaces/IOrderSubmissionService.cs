using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.OrderExecution.Core.Models;

namespace PseudoMarkets.OrderExecution.Core.Interfaces;

public interface IOrderSubmissionService
{
    Task<SubmitOrderResponse> SubmitAsync(
        SubmitOrderRequest request,
        OrderCallerContext callerContext,
        CancellationToken cancellationToken);
}
