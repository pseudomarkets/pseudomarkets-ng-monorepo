using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class TradeTransactionPostingService : TransactionProcessingServiceBase, ITradeTransactionPostingService
{
    public TradeTransactionPostingService(
        PseudoMarketsDbContext dbContext,
        ITransactionDescriptionService transactionDescriptionService,
        IMarketCalendarService marketCalendarService,
        ILogger<TradeTransactionPostingService> logger)
        : base(dbContext, transactionDescriptionService, logger)
    {
        MarketCalendarService = marketCalendarService;
    }

    private IMarketCalendarService MarketCalendarService { get; }

    public async Task<TransactionCommandResponse> PostTradeAsync(
        PostTradeTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var normalizedSymbol = NormalizeSymbol(request.Symbol);
        var normalizedExecutedAtUtc = NormalizeUtc(request.ExecutedAtUtc);
        var normalizedQuantity = NormalizeQuantity(request.Quantity);
        var normalizedExecutionPrice = NormalizeUnitPrice(request.ExecutionPrice);
        var normalizedGrossAmount = NormalizeCurrency(request.GrossAmount);
        var normalizedFees = NormalizeCurrency(request.Fees);
        var normalizedNetAmount = NormalizeCurrency(request.NetAmount);
        var transactionKind = request.TradeSide == TradeSide.Buy
            ? TransactionKind.TradeBuy
            : TransactionKind.TradeSell;
        var ledgerDirection = request.TradeSide == TradeSide.Buy
            ? LedgerDirection.Debit
            : LedgerDirection.Credit;
        var requestType = transactionKind.ToString();

        var existingResponse = await TryGetExistingResponseAsync(request.IdempotencyKey, requestType, request.UserId, cancellationToken);
        if (existingResponse is not null)
        {
            return existingResponse;
        }

        var now = DateTime.UtcNow;
        var tradeDate = MarketCalendarService.GetTradeDate(normalizedExecutedAtUtc);
        var settlementDate = await MarketCalendarService.GetSettlementDateAsync(tradeDate, cancellationToken);

        await using var databaseTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (await DbContext.TradeExecutions.AnyAsync(
                    x => x.ExternalExecutionId == request.ExternalExecutionId,
                    cancellationToken))
            {
                throw new TransactionProcessingConflictException(
                    $"The external execution '{request.ExternalExecutionId}' has already been posted.");
            }

            var postingBatch = CreatePostingBatch(request.UserId, request.IdempotencyKey, requestType, now);
            await DbContext.PostingBatches.AddAsync(postingBatch, cancellationToken);

            var balance = await GetOrCreateBalanceAsync(request.UserId, now, cancellationToken);
            var description = TransactionDescriptionService.DescribeTrade(transactionKind, normalizedSymbol, normalizedNetAmount);
            var ledgerTransaction = CreateLedgerTransaction(
                postingBatch.Id,
                request.UserId,
                transactionKind,
                ledgerDirection,
                normalizedNetAmount,
                description,
                normalizedExecutedAtUtc,
                now,
                request.ExternalExecutionId);

            await DbContext.LedgerTransactions.AddAsync(ledgerTransaction, cancellationToken);

            if (request.TradeSide == TradeSide.Buy)
            {
                PostBuyTrade(balance, ledgerTransaction, normalizedSymbol, normalizedQuantity, normalizedNetAmount, now);
            }
            else
            {
                await PostSellTradeAsync(
                    balance,
                    ledgerTransaction.TransactionId,
                    request.UserId,
                    normalizedSymbol,
                    normalizedQuantity,
                    normalizedNetAmount,
                    now,
                    cancellationToken);
            }

            await DbContext.TradeExecutions.AddAsync(
                new TradeExecutionEntity
                {
                    TransactionId = ledgerTransaction.TransactionId,
                    UserId = request.UserId,
                    ExternalExecutionId = request.ExternalExecutionId.Trim(),
                    Symbol = normalizedSymbol,
                    TradeSide = request.TradeSide.ToString(),
                    Quantity = normalizedQuantity,
                    ExecutionPrice = normalizedExecutionPrice,
                    GrossAmount = normalizedGrossAmount,
                    Fees = normalizedFees,
                    NetAmount = normalizedNetAmount,
                    ExecutedAtUtc = normalizedExecutedAtUtc,
                    TradeDate = tradeDate,
                    SettlementDate = settlementDate,
                    CreatedAtUtc = now
                },
                cancellationToken);

            MarkBatchPosted(postingBatch, now);

            await DbContext.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);

            return CreateResponse(postingBatch.Id, ledgerTransaction);
        }
        catch (TransactionProcessingValidationException)
        {
            throw;
        }
        catch (TransactionProcessingConflictException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            Logger.LogError(exception, "Trade posting failed for external execution {ExternalExecutionId}.", request.ExternalExecutionId);
            throw new TransactionProcessingDependencyException("Failed to persist the trade transaction.", exception);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Trade posting failed unexpectedly for external execution {ExternalExecutionId}.", request.ExternalExecutionId);
            throw new TransactionProcessingServiceException("Trade posting failed unexpectedly.", exception);
        }
    }

    private void PostBuyTrade(
        AccountBalanceEntity balance,
        LedgerTransactionEntity ledgerTransaction,
        string symbol,
        decimal quantity,
        decimal netAmount,
        DateTime now)
    {
        if (balance.SettledCashBalance < netAmount)
        {
            throw new TransactionProcessingConflictException("The user does not have enough settled cash available to post the buy trade.");
        }

        balance.CashBalance = NormalizeCurrency(balance.CashBalance - netAmount);
        balance.SettledCashBalance = NormalizeCurrency(balance.SettledCashBalance - netAmount);
        balance.UpdatedAtUtc = now;

        var position = DbContext.Positions.FirstOrDefault(x => x.UserId == ledgerTransaction.UserId && x.Symbol == symbol);
        if (position is null)
        {
            position = new PositionEntity
            {
                UserId = ledgerTransaction.UserId,
                Symbol = symbol,
                PositionSide = PositionSide.Long.ToString(),
                Quantity = 0m,
                SettledQuantity = 0m,
                UnsettledQuantity = 0m,
                CostBasisTotal = 0m,
                SettledCostBasisTotal = 0m,
                UnsettledCostBasisTotal = 0m,
                UpdatedAtUtc = now
            };

            DbContext.Positions.Add(position);
        }

        position.Quantity = NormalizeQuantity(position.Quantity + quantity);
        position.UnsettledQuantity = NormalizeQuantity(position.UnsettledQuantity + quantity);
        position.CostBasisTotal = NormalizeCurrency(position.CostBasisTotal + netAmount);
        position.UnsettledCostBasisTotal = NormalizeCurrency(position.UnsettledCostBasisTotal + netAmount);
        position.UpdatedAtUtc = now;

        DbContext.PositionLots.Add(
            new PositionLotEntity
            {
                UserId = ledgerTransaction.UserId,
                Symbol = symbol,
                OpeningTransactionId = ledgerTransaction.TransactionId,
                ClosingTransactionId = null,
                LotEntryType = LotEntryType.Open.ToString(),
                QuantityOpened = quantity,
                QuantityRemaining = quantity,
                SettledQuantityRemaining = 0m,
                UnsettledQuantityRemaining = quantity,
                Price = NormalizeUnitPrice(netAmount / quantity),
                OpenedAtUtc = ledgerTransaction.OccurredAtUtc,
                UpdatedAtUtc = now
            });
    }

    private async Task PostSellTradeAsync(
        AccountBalanceEntity balance,
        Guid transactionId,
        long userId,
        string symbol,
        decimal quantity,
        decimal netAmount,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var position = await DbContext.Positions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Symbol == symbol, cancellationToken);

        if (position is null || position.SettledQuantity < quantity)
        {
            throw new TransactionProcessingConflictException("The user does not have enough settled position quantity available to post the sell trade.");
        }

        var lots = await DbContext.PositionLots
            .Where(x => x.UserId == userId && x.Symbol == symbol && x.SettledQuantityRemaining > 0m)
            .OrderBy(x => x.OpenedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var totalAvailableQuantity = NormalizeQuantity(lots.Sum(x => x.SettledQuantityRemaining));
        if (totalAvailableQuantity < quantity)
        {
            throw new TransactionProcessingConflictException("The user does not have enough settled lot inventory available to post the sell trade.");
        }

        balance.CashBalance = NormalizeCurrency(balance.CashBalance + netAmount);
        balance.UnsettledCashBalance = NormalizeCurrency(balance.UnsettledCashBalance + netAmount);
        balance.UpdatedAtUtc = now;

        var remainingQuantity = quantity;
        var totalCostBasisReduced = 0m;

        foreach (var lot in lots)
        {
            if (remainingQuantity <= 0m)
            {
                break;
            }

            var quantityToClose = Math.Min(lot.SettledQuantityRemaining, remainingQuantity);
            var costBasisAmount = NormalizeCurrency(lot.Price * quantityToClose);

            DbContext.PositionLotClosures.Add(
                new PositionLotClosureEntity
                {
                    PositionLot = lot,
                    OpeningTransactionId = lot.OpeningTransactionId,
                    ClosingTransactionId = transactionId,
                    UserId = userId,
                    Symbol = symbol,
                    QuantityClosed = quantityToClose,
                    CostBasisAmount = costBasisAmount,
                    ClosedAtUtc = now,
                    CreatedAtUtc = now
                });

            lot.QuantityRemaining = NormalizeQuantity(lot.QuantityRemaining - quantityToClose);
            lot.SettledQuantityRemaining = NormalizeQuantity(lot.SettledQuantityRemaining - quantityToClose);
            lot.UpdatedAtUtc = now;

            if (lot.QuantityRemaining == 0m)
            {
                lot.ClosingTransactionId = transactionId;
            }

            remainingQuantity = NormalizeQuantity(remainingQuantity - quantityToClose);
            totalCostBasisReduced = NormalizeCurrency(totalCostBasisReduced + costBasisAmount);
        }

        if (remainingQuantity > 0m)
        {
            throw new TransactionProcessingServiceException("Failed to fully consume lots for the sell trade.");
        }

        position.Quantity = NormalizeQuantity(position.Quantity - quantity);
        position.SettledQuantity = NormalizeQuantity(position.SettledQuantity - quantity);
        position.CostBasisTotal = NormalizeCurrency(position.CostBasisTotal - totalCostBasisReduced);
        position.SettledCostBasisTotal = NormalizeCurrency(position.SettledCostBasisTotal - totalCostBasisReduced);
        position.UpdatedAtUtc = now;

        if (position.Quantity == 0m)
        {
            DbContext.Positions.Remove(position);
        }
        else if (position.CostBasisTotal < 0m)
        {
            position.CostBasisTotal = 0m;
        }

        if (position.SettledCostBasisTotal < 0m)
        {
            position.SettledCostBasisTotal = 0m;
        }
    }

    private static void ValidateRequest(PostTradeTransactionRequest request)
    {
        if (request.UserId is < 1000000000 or > 9999999999)
        {
            throw new TransactionProcessingValidationException("A valid 10 digit user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new TransactionProcessingValidationException("An idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            throw new TransactionProcessingValidationException("A symbol is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ExternalExecutionId))
        {
            throw new TransactionProcessingValidationException("An external execution ID is required.");
        }

        if (!Enum.IsDefined(request.TradeSide))
        {
            throw new TransactionProcessingValidationException("A valid trade side is required.");
        }

        if (request.Quantity <= 0m || request.ExecutionPrice <= 0m || request.GrossAmount <= 0m || request.NetAmount <= 0m)
        {
            throw new TransactionProcessingValidationException("Trade amounts and quantity must be greater than zero.");
        }

        if (request.Fees < 0m)
        {
            throw new TransactionProcessingValidationException("Trade fees cannot be negative.");
        }
    }
}
