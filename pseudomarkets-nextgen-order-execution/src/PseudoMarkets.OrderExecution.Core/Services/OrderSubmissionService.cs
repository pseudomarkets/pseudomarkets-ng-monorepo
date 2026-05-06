using System.Text.Json;
using PseudoMarkets.OrderExecution.Contracts.Enums;
using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;
using PseudoMarkets.Shared.Entities.Entities.OrderExecution;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;

namespace PseudoMarkets.OrderExecution.Core.Services;

public sealed class OrderSubmissionService : IOrderSubmissionService
{
    private const string SupportedPrimaryInstrumentType = "Equity";
    private static readonly HashSet<string> SystemOnlyRoles =
    [
        "UPDATE_TRANSACTIONS",
        "UPDATE_BALANCES",
        "UPDATE_INSTRUMENTS"
    ];

    private readonly ITradingInstrumentsClient _tradingInstrumentsClient;
    private readonly IMarketDataClient _marketDataClient;
    private readonly ITransactionProcessingClient _transactionProcessingClient;
    private readonly IOrderExecutionRepository _repository;
    private readonly IClock _clock;

    public OrderSubmissionService(
        ITradingInstrumentsClient tradingInstrumentsClient,
        IMarketDataClient marketDataClient,
        ITransactionProcessingClient transactionProcessingClient,
        IOrderExecutionRepository repository,
        IClock clock)
    {
        _tradingInstrumentsClient = tradingInstrumentsClient;
        _marketDataClient = marketDataClient;
        _transactionProcessingClient = transactionProcessingClient;
        _repository = repository;
        _clock = clock;
    }

    public async Task<SubmitOrderResponse> SubmitAsync(
        SubmitOrderRequest request,
        OrderCallerContext callerContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(callerContext);

        ValidateCaller(request, callerContext);

        if (request.OrderType != OrderType.Market)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.UnsupportedOrderType,
                "Only market orders are supported.");
        }

        if (request.Side is not (OrderSide.Buy or OrderSide.Sell))
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.UnsupportedOrderType,
                "Order side must be Buy or Sell.");
        }

        if (request.Quantity <= 0)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InvalidQuantity,
                "Order quantity must be greater than zero.");
        }

        var symbol = NormalizeAndValidateSymbol(request.Symbol);
        var instrument = await _tradingInstrumentsClient.GetBySymbolAsync(symbol, cancellationToken);
        ValidateTradableInstrument(symbol, instrument.Symbol, instrument.TradingStatus, instrument.PrimaryInstrumentType);

        var quote = await _marketDataClient.GetQuoteAsync(symbol, cancellationToken);
        if (quote.Price <= 0)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InvalidMarketPrice,
                "Market order rejected because the quote price must be greater than zero.");
        }

        var fillPrice = quote.Price;
        var grossAmount = request.Quantity * fillPrice;
        var fees = 0m;
        var netAmount = grossAmount + fees;

        if (request.Side == OrderSide.Buy)
        {
            await ValidateBuyAsync(request.UserId, netAmount, cancellationToken);
        }
        else
        {
            await ValidateSellAsync(request.UserId, symbol, request.Quantity, cancellationToken);
        }

        var now = _clock.UtcNow;
        var orderExecution = new OrderExecutionEntity
        {
            OrderId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            UserId = request.UserId,
            Symbol = symbol,
            OrderSide = request.Side.ToString(),
            OrderType = request.OrderType.ToString(),
            Quantity = request.Quantity,
            FillPrice = fillPrice,
            GrossAmount = grossAmount,
            Fees = fees,
            NetAmount = netAmount,
            Status = OrderStatus.Accepted.ToString(),
            SubmittedAtUtc = now,
            ExecutedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repository.AddAsync(orderExecution, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        try
        {
            var transactionResponse = await _transactionProcessingClient.PostTradeAsync(
                new PostTradeTransactionRequest
                {
                    IdempotencyKey = orderExecution.OrderId.ToString("N"),
                    UserId = request.UserId,
                    Symbol = symbol,
                    TradeSide = request.Side == OrderSide.Buy ? TradeSide.Buy : TradeSide.Sell,
                    Quantity = request.Quantity,
                    ExecutionPrice = fillPrice,
                    GrossAmount = grossAmount,
                    Fees = fees,
                    NetAmount = netAmount,
                    ExecutedAtUtc = now,
                    ExternalExecutionId = orderExecution.ExecutionId.ToString("D")
                },
                cancellationToken);

            orderExecution.Status = OrderStatus.Filled.ToString();
            orderExecution.TransactionId = transactionResponse.TransactionId;
            orderExecution.PostingBatchId = transactionResponse.PostingBatchId;
            orderExecution.UpdatedAtUtc = _clock.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);

            return ToResponse(orderExecution);
        }
        catch (OrderExecutionException ex)
        {
            orderExecution.Status = OrderStatus.TransactionPostingFailed.ToString();
            orderExecution.FailureCode = ex.Code;
            orderExecution.FailureMessage = ex.Message;
            orderExecution.UpdatedAtUtc = _clock.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateCaller(SubmitOrderRequest request, OrderCallerContext callerContext)
    {
        if (callerContext.AuthorizedUserId <= 0)
        {
            throw new OrderExecutionAuthorizationException(
                OrderExecutionErrorCodes.InvalidUser,
                "The authorized token did not resolve to a valid user.");
        }

        if (IsSystemToken(callerContext.BearerToken))
        {
            return;
        }

        if (callerContext.AuthorizedUserId != request.UserId)
        {
            throw new OrderExecutionAuthorizationException(
                OrderExecutionErrorCodes.UserOwnershipViolation,
                "User tokens may only submit orders for the authenticated user.");
        }
    }

    private static bool IsSystemToken(string bearerToken)
    {
        foreach (var role in ReadRoles(bearerToken))
        {
            if (SystemOnlyRoles.Contains(role))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ReadRoles(string bearerToken)
    {
        var parts = bearerToken.Split('.');
        if (parts.Length < 2)
        {
            return [];
        }

        try
        {
            var payloadJson = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                return [];
            }

            return rolesElement.ValueKind switch
            {
                JsonValueKind.String => rolesElement.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
                JsonValueKind.Array => rolesElement.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!)
                    .ToArray(),
                _ => []
            };
        }
        catch
        {
            return [];
        }
    }

    private static string Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private static string NormalizeAndValidateSymbol(string symbol)
    {
        var normalizedSymbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedSymbol) || normalizedSymbol.Length > 32)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InvalidSymbolFormat,
                "Order symbol must be 1 to 32 alphanumeric characters.");
        }

        if (normalizedSymbol.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InvalidSymbolFormat,
                "Order symbol may only contain letters and numbers.");
        }

        return normalizedSymbol;
    }

    private static void ValidateTradableInstrument(
        string requestedSymbol,
        string returnedSymbol,
        bool tradingStatus,
        string primaryInstrumentType)
    {
        if (!string.Equals(requestedSymbol, returnedSymbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.UnsupportedSymbol,
                $"Trading instrument '{requestedSymbol}' was not found.");
        }

        if (!tradingStatus)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.SymbolNotTradable,
                $"Trading instrument '{requestedSymbol}' is not enabled for trading.");
        }

        if (!string.Equals(primaryInstrumentType, SupportedPrimaryInstrumentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.UnsupportedInstrumentType,
                $"Trading instrument '{requestedSymbol}' is outside the supported instrument scope.");
        }
    }

    private async Task ValidateBuyAsync(long userId, decimal netAmount, CancellationToken cancellationToken)
    {
        var balance = await _repository.GetAccountBalanceAsync(userId, cancellationToken);
        if (balance is null)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.MissingBalance,
                "Buy order rejected because no account balance exists for the user.");
        }

        if (balance.SettledCashBalance < netAmount)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InsufficientSettledCash,
                "Buy order rejected because settled cash is insufficient.");
        }
    }

    private async Task ValidateSellAsync(
        long userId,
        string symbol,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var position = await _repository.GetPositionAsync(userId, symbol, cancellationToken);
        if (position is null || position.SettledQuantity <= 0)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.MissingPosition,
                "Sell order rejected because no settled position exists for the user and symbol.");
        }

        if (position.SettledQuantity < quantity)
        {
            throw new OrderExecutionValidationException(
                OrderExecutionErrorCodes.InsufficientSettledPosition,
                "Sell order rejected because settled position quantity is insufficient.");
        }
    }

    private static SubmitOrderResponse ToResponse(OrderExecutionEntity orderExecution)
    {
        return new SubmitOrderResponse
        {
            OrderId = orderExecution.OrderId,
            ExecutionId = orderExecution.ExecutionId,
            TransactionId = orderExecution.TransactionId,
            PostingBatchId = orderExecution.PostingBatchId,
            UserId = orderExecution.UserId,
            Symbol = orderExecution.Symbol,
            Side = Enum.Parse<OrderSide>(orderExecution.OrderSide),
            OrderType = Enum.Parse<OrderType>(orderExecution.OrderType),
            Quantity = orderExecution.Quantity,
            FillPrice = orderExecution.FillPrice,
            GrossAmount = orderExecution.GrossAmount,
            Fees = orderExecution.Fees,
            NetAmount = orderExecution.NetAmount,
            Status = Enum.Parse<OrderStatus>(orderExecution.Status),
            SubmittedAtUtc = orderExecution.SubmittedAtUtc,
            ExecutedAtUtc = orderExecution.ExecutedAtUtc
        };
    }
}
