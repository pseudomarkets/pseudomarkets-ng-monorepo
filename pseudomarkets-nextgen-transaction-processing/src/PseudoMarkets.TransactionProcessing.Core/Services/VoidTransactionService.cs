using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.TransactionProcessing.Persistence.Database;
using PseudoMarkets.TransactionProcessing.Persistence.Entities;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class VoidTransactionService : TransactionProcessingServiceBase, IVoidTransactionService
{
    public VoidTransactionService(
        TransactionProcessingDbContext dbContext,
        ITransactionDescriptionService transactionDescriptionService,
        ILogger<VoidTransactionService> logger)
        : base(dbContext, transactionDescriptionService, logger)
    {
    }

    public async Task<TransactionCommandResponse> VoidAsync(
        Guid transactionId,
        VoidTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new TransactionProcessingValidationException("An idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            throw new TransactionProcessingValidationException("A reason code is required to void a transaction.");
        }

        var requestType = $"Void:{transactionId:D}";
        var existingResponse = await TryGetExistingResponseAsync(request.IdempotencyKey, requestType, null, cancellationToken);
        if (existingResponse is not null)
        {
            return existingResponse;
        }

        var normalizedVoidedAtUtc = NormalizeUtc(request.VoidedAtUtc);
        var now = DateTime.UtcNow;

        await using var databaseTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var originalTransaction = await DbContext.LedgerTransactions
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

            if (originalTransaction is null)
            {
                throw new TransactionProcessingNotFoundException(
                    $"The transaction '{transactionId:D}' was not found and cannot be voided.");
            }

            if (originalTransaction.Status == TransactionStatus.Voided.ToString()
                || await DbContext.LedgerTransactions.AnyAsync(
                    x => x.VoidsTransactionId == transactionId,
                    cancellationToken))
            {
                throw new TransactionProcessingConflictException(
                    $"The transaction '{transactionId:D}' has already been voided.");
            }

            var originalKind = ParseTransactionKind(originalTransaction.TransactionKind);
            if (originalKind == TransactionKind.Void)
            {
                throw new TransactionProcessingConflictException("Void transactions cannot be voided again.");
            }

            var postingBatch = CreatePostingBatch(originalTransaction.UserId, request.IdempotencyKey, requestType, now);
            await DbContext.PostingBatches.AddAsync(postingBatch, cancellationToken);

            var balance = await GetOrCreateBalanceAsync(originalTransaction.UserId, now, cancellationToken);
            var voidDescription = TransactionDescriptionService.DescribeVoid(originalTransaction.TransactionDescription);

            LedgerTransactionEntity voidTransaction = originalKind switch
            {
                TransactionKind.CashDeposit => await VoidCashDepositAsync(
                    originalTransaction,
                    postingBatch,
                    balance,
                    normalizedVoidedAtUtc,
                    now,
                    request.ReasonCode,
                    voidDescription,
                    cancellationToken),
                TransactionKind.CashWithdrawal => await VoidCashWithdrawalAsync(
                    originalTransaction,
                    postingBatch,
                    balance,
                    normalizedVoidedAtUtc,
                    now,
                    request.ReasonCode,
                    voidDescription,
                    cancellationToken),
                TransactionKind.CashAdjustment => await VoidCashAdjustmentAsync(
                    originalTransaction,
                    postingBatch,
                    balance,
                    normalizedVoidedAtUtc,
                    now,
                    request.ReasonCode,
                    voidDescription,
                    cancellationToken),
                TransactionKind.TradeBuy => await VoidTradeBuyAsync(
                    originalTransaction,
                    postingBatch,
                    balance,
                    normalizedVoidedAtUtc,
                    now,
                    voidDescription,
                    cancellationToken),
                TransactionKind.TradeSell => await VoidTradeSellAsync(
                    originalTransaction,
                    postingBatch,
                    balance,
                    normalizedVoidedAtUtc,
                    now,
                    voidDescription,
                    cancellationToken),
                _ => throw new TransactionProcessingServiceException($"Unsupported transaction kind '{originalKind}' for void processing.")
            };

            originalTransaction.Status = TransactionStatus.Voided.ToString();
            MarkBatchPosted(postingBatch, now);

            await DbContext.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);

            return CreateResponse(postingBatch.Id, voidTransaction);
        }
        catch (TransactionProcessingValidationException)
        {
            throw;
        }
        catch (TransactionProcessingNotFoundException)
        {
            throw;
        }
        catch (TransactionProcessingConflictException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            Logger.LogError(exception, "Transaction void failed for transaction {TransactionId}.", transactionId);
            throw new TransactionProcessingDependencyException("Failed to persist the compensating void transaction.", exception);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Transaction void failed unexpectedly for transaction {TransactionId}.", transactionId);
            throw new TransactionProcessingServiceException("Transaction void failed unexpectedly.", exception);
        }
    }

    private async Task<LedgerTransactionEntity> VoidCashDepositAsync(
        LedgerTransactionEntity originalTransaction,
        PostingBatchEntity postingBatch,
        AccountBalanceEntity balance,
        DateTime voidedAtUtc,
        DateTime now,
        string reasonCode,
        string voidDescription,
        CancellationToken cancellationToken)
    {
        var originalCashMovement = await GetRequiredCashMovementAsync(originalTransaction.TransactionId, cancellationToken);
        EnsureSufficientBalance(balance, originalTransaction.Amount, "The user does not have enough cash available to void the original deposit.");

        balance.CashBalance = NormalizeCurrency(balance.CashBalance - originalTransaction.Amount);
        balance.UpdatedAtUtc = now;

        var voidTransaction = CreateLedgerTransaction(
            postingBatch.Id,
            originalTransaction.UserId,
            TransactionKind.Void,
            LedgerDirection.Debit,
            originalTransaction.Amount,
            voidDescription,
            voidedAtUtc,
            now,
            originalCashMovement.ExternalReferenceId,
            originalTransaction.TransactionId);

        await DbContext.LedgerTransactions.AddAsync(voidTransaction, cancellationToken);
        await DbContext.CashMovements.AddAsync(
            new CashMovementEntity
            {
                TransactionId = voidTransaction.TransactionId,
                UserId = originalTransaction.UserId,
                MovementType = "VoidDeposit",
                ExternalReferenceId = originalCashMovement.ExternalReferenceId,
                ReasonCode = reasonCode,
                OccurredAtUtc = voidedAtUtc,
                CreatedAtUtc = now
            },
            cancellationToken);

        return voidTransaction;
    }

    private async Task<LedgerTransactionEntity> VoidCashWithdrawalAsync(
        LedgerTransactionEntity originalTransaction,
        PostingBatchEntity postingBatch,
        AccountBalanceEntity balance,
        DateTime voidedAtUtc,
        DateTime now,
        string reasonCode,
        string voidDescription,
        CancellationToken cancellationToken)
    {
        var originalCashMovement = await GetRequiredCashMovementAsync(originalTransaction.TransactionId, cancellationToken);

        balance.CashBalance = NormalizeCurrency(balance.CashBalance + originalTransaction.Amount);
        balance.UpdatedAtUtc = now;

        var voidTransaction = CreateLedgerTransaction(
            postingBatch.Id,
            originalTransaction.UserId,
            TransactionKind.Void,
            LedgerDirection.Credit,
            originalTransaction.Amount,
            voidDescription,
            voidedAtUtc,
            now,
            originalCashMovement.ExternalReferenceId,
            originalTransaction.TransactionId);

        await DbContext.LedgerTransactions.AddAsync(voidTransaction, cancellationToken);
        await DbContext.CashMovements.AddAsync(
            new CashMovementEntity
            {
                TransactionId = voidTransaction.TransactionId,
                UserId = originalTransaction.UserId,
                MovementType = "VoidWithdrawal",
                ExternalReferenceId = originalCashMovement.ExternalReferenceId,
                ReasonCode = reasonCode,
                OccurredAtUtc = voidedAtUtc,
                CreatedAtUtc = now
            },
            cancellationToken);

        return voidTransaction;
    }

    private async Task<LedgerTransactionEntity> VoidCashAdjustmentAsync(
        LedgerTransactionEntity originalTransaction,
        PostingBatchEntity postingBatch,
        AccountBalanceEntity balance,
        DateTime voidedAtUtc,
        DateTime now,
        string reasonCode,
        string voidDescription,
        CancellationToken cancellationToken)
    {
        var originalCashMovement = await GetRequiredCashMovementAsync(originalTransaction.TransactionId, cancellationToken);
        var originalDirection = ParseLedgerDirection(originalTransaction.Direction);

        if (originalDirection == LedgerDirection.Credit)
        {
            EnsureSufficientBalance(balance, originalTransaction.Amount, "The user does not have enough cash available to void the original credit adjustment.");
            balance.CashBalance = NormalizeCurrency(balance.CashBalance - originalTransaction.Amount);
        }
        else
        {
            balance.CashBalance = NormalizeCurrency(balance.CashBalance + originalTransaction.Amount);
        }

        balance.UpdatedAtUtc = now;

        var voidDirection = originalDirection == LedgerDirection.Credit
            ? LedgerDirection.Debit
            : LedgerDirection.Credit;

        var voidTransaction = CreateLedgerTransaction(
            postingBatch.Id,
            originalTransaction.UserId,
            TransactionKind.Void,
            voidDirection,
            originalTransaction.Amount,
            voidDescription,
            voidedAtUtc,
            now,
            originalCashMovement.ExternalReferenceId,
            originalTransaction.TransactionId);

        await DbContext.LedgerTransactions.AddAsync(voidTransaction, cancellationToken);
        await DbContext.CashMovements.AddAsync(
            new CashMovementEntity
            {
                TransactionId = voidTransaction.TransactionId,
                UserId = originalTransaction.UserId,
                MovementType = "VoidAdjustment",
                ExternalReferenceId = originalCashMovement.ExternalReferenceId,
                ReasonCode = reasonCode,
                OccurredAtUtc = voidedAtUtc,
                CreatedAtUtc = now
            },
            cancellationToken);

        return voidTransaction;
    }

    private async Task<LedgerTransactionEntity> VoidTradeBuyAsync(
        LedgerTransactionEntity originalTransaction,
        PostingBatchEntity postingBatch,
        AccountBalanceEntity balance,
        DateTime voidedAtUtc,
        DateTime now,
        string voidDescription,
        CancellationToken cancellationToken)
    {
        var originalTrade = await GetRequiredTradeExecutionAsync(originalTransaction.TransactionId, cancellationToken);
        await EnsureTradeCanBeStrictlyVoidedAsync(originalTransaction, originalTrade, cancellationToken);

        var affectedLots = await DbContext.PositionLots
            .Where(x => x.OpeningTransactionId == originalTransaction.TransactionId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (affectedLots.Count == 0)
        {
            throw new TransactionProcessingConflictException("The original buy transaction has no remaining lot inventory to reverse.");
        }

        if (affectedLots.Any(x => x.QuantityRemaining != x.QuantityOpened))
        {
            throw new TransactionProcessingConflictException("The original buy transaction cannot be voided because its lot inventory has already been consumed.");
        }

        var position = await DbContext.Positions
            .FirstOrDefaultAsync(x => x.UserId == originalTransaction.UserId && x.Symbol == originalTrade.Symbol, cancellationToken);

        if (position is null || position.Quantity < originalTrade.Quantity)
        {
            throw new TransactionProcessingConflictException("The original buy transaction cannot be voided because the current position state no longer matches the purchase.");
        }

        balance.CashBalance = NormalizeCurrency(balance.CashBalance + originalTransaction.Amount);
        balance.UpdatedAtUtc = now;

        foreach (var lot in affectedLots)
        {
            DbContext.PositionLots.Remove(lot);
        }

        position.Quantity = NormalizeQuantity(position.Quantity - originalTrade.Quantity);
        position.CostBasisTotal = NormalizeCurrency(position.CostBasisTotal - originalTransaction.Amount);
        position.UpdatedAtUtc = now;

        if (position.Quantity == 0m)
        {
            DbContext.Positions.Remove(position);
        }
        else if (position.CostBasisTotal < 0m)
        {
            position.CostBasisTotal = 0m;
        }

        var voidTransaction = CreateLedgerTransaction(
            postingBatch.Id,
            originalTransaction.UserId,
            TransactionKind.Void,
            LedgerDirection.Credit,
            originalTransaction.Amount,
            voidDescription,
            voidedAtUtc,
            now,
            originalTransaction.ExternalReferenceId,
            originalTransaction.TransactionId);

        await DbContext.LedgerTransactions.AddAsync(voidTransaction, cancellationToken);
        return voidTransaction;
    }

    private async Task<LedgerTransactionEntity> VoidTradeSellAsync(
        LedgerTransactionEntity originalTransaction,
        PostingBatchEntity postingBatch,
        AccountBalanceEntity balance,
        DateTime voidedAtUtc,
        DateTime now,
        string voidDescription,
        CancellationToken cancellationToken)
    {
        var originalTrade = await GetRequiredTradeExecutionAsync(originalTransaction.TransactionId, cancellationToken);
        await EnsureTradeCanBeStrictlyVoidedAsync(originalTransaction, originalTrade, cancellationToken);

        EnsureSufficientBalance(balance, originalTransaction.Amount, "The user does not have enough cash available to void the original sell trade.");

        var lotClosures = await DbContext.PositionLotClosures
            .Include(x => x.PositionLot)
            .Where(x => x.ClosingTransactionId == originalTransaction.TransactionId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (lotClosures.Count == 0)
        {
            throw new TransactionProcessingConflictException("The original sell transaction has no lot closures to reverse.");
        }

        balance.CashBalance = NormalizeCurrency(balance.CashBalance - originalTransaction.Amount);
        balance.UpdatedAtUtc = now;

        var restoredQuantity = 0m;
        var restoredCostBasis = 0m;

        foreach (var lotClosure in lotClosures)
        {
            var lot = lotClosure.PositionLot;
            lot.QuantityRemaining = NormalizeQuantity(lot.QuantityRemaining + lotClosure.QuantityClosed);
            lot.UpdatedAtUtc = now;

            if (lot.QuantityRemaining > lot.QuantityOpened)
            {
                throw new TransactionProcessingServiceException("A sell void attempted to restore more quantity than the original lot opened.");
            }

            if (lot.QuantityRemaining == lot.QuantityOpened)
            {
                lot.ClosingTransactionId = null;
            }

            restoredQuantity = NormalizeQuantity(restoredQuantity + lotClosure.QuantityClosed);
            restoredCostBasis = NormalizeCurrency(restoredCostBasis + lotClosure.CostBasisAmount);
        }

        if (restoredQuantity != originalTrade.Quantity)
        {
            throw new TransactionProcessingServiceException("The sell void did not restore the full original trade quantity.");
        }

        var position = await DbContext.Positions
            .FirstOrDefaultAsync(x => x.UserId == originalTransaction.UserId && x.Symbol == originalTrade.Symbol, cancellationToken);

        if (position is null)
        {
            position = new PositionEntity
            {
                UserId = originalTransaction.UserId,
                Symbol = originalTrade.Symbol,
                PositionSide = PositionSide.Long.ToString(),
                Quantity = 0m,
                CostBasisTotal = 0m,
                UpdatedAtUtc = now
            };

            await DbContext.Positions.AddAsync(position, cancellationToken);
        }

        position.Quantity = NormalizeQuantity(position.Quantity + restoredQuantity);
        position.CostBasisTotal = NormalizeCurrency(position.CostBasisTotal + restoredCostBasis);
        position.UpdatedAtUtc = now;

        var voidTransaction = CreateLedgerTransaction(
            postingBatch.Id,
            originalTransaction.UserId,
            TransactionKind.Void,
            LedgerDirection.Debit,
            originalTransaction.Amount,
            voidDescription,
            voidedAtUtc,
            now,
            originalTransaction.ExternalReferenceId,
            originalTransaction.TransactionId);

        await DbContext.LedgerTransactions.AddAsync(voidTransaction, cancellationToken);
        return voidTransaction;
    }

    private async Task EnsureTradeCanBeStrictlyVoidedAsync(
        LedgerTransactionEntity originalTransaction,
        TradeExecutionEntity originalTrade,
        CancellationToken cancellationToken)
    {
        if (await HasLaterActiveTradeForSymbolAsync(
                originalTransaction.UserId,
                originalTrade.Symbol,
                originalTrade.ExecutedAtUtc,
                originalTransaction.TransactionId,
                cancellationToken))
        {
            throw new TransactionProcessingConflictException(
                $"The trade transaction '{originalTransaction.TransactionId:D}' cannot be voided because later active trade activity exists for {originalTrade.Symbol}.");
        }
    }

    private static void EnsureSufficientBalance(AccountBalanceEntity balance, decimal amount, string message)
    {
        if (balance.CashBalance < amount)
        {
            throw new TransactionProcessingConflictException(message);
        }
    }
}
