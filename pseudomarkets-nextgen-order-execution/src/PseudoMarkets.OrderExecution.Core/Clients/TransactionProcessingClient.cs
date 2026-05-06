using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.OrderExecution.Core.Clients;

public sealed class TransactionProcessingClient : ITransactionProcessingClient
{
    private readonly HttpClient _httpClient;
    private readonly ISystemTokenProvider _systemTokenProvider;

    public TransactionProcessingClient(HttpClient httpClient, ISystemTokenProvider systemTokenProvider)
    {
        _httpClient = httpClient;
        _systemTokenProvider = systemTokenProvider;
    }

    public async Task<TransactionCommandResponse> PostTradeAsync(
        PostTradeTransactionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/transactions/trades")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _systemTokenProvider.GetTokenAsync(cancellationToken));

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.DownstreamUnauthorized,
                    "Transaction Processing rejected the Order Execution system account token.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new OrderExecutionDependencyException(
                    OrderExecutionErrorCodes.TransactionPostingFailed,
                    "Transaction Processing could not post the completed trade execution.");
            }

            var payload = await response.Content.ReadFromJsonAsync<TransactionCommandResponse>(cancellationToken);
            return payload ?? throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.TransactionPostingFailed,
                "Transaction Processing returned an empty trade-posting response.");
        }
        catch (OrderExecutionException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new OrderExecutionDependencyException(
                OrderExecutionErrorCodes.TransactionPostingFailed,
                "Transaction Processing could not be reached while posting the trade execution.",
                ex);
        }
    }
}
