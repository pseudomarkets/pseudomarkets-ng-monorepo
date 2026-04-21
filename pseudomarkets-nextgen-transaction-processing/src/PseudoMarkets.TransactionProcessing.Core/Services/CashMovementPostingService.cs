using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.Shared.Entities.Database;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public class CashMovementPostingService : TransactionProcessingServiceBase, ICashMovementPostingService
{
    public CashMovementPostingService(
        PseudoMarketsDbContext dbContext,
        ITransactionDescriptionService transactionDescriptionService,
        ILogger<CashMovementPostingService> logger)
        : base(dbContext, transactionDescriptionService, logger)
    {
    }

    public Task<TransactionCommandResponse> PostDepositAsync(
        PostCashDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostCashMovementAsync(
            userId: request.UserId,
            idempotencyKey: request.IdempotencyKey,
            transactionKind: TransactionKind.CashDeposit,
            direction: LedgerDirection.Credit,
            amount: request.Amount,
            occurredAtUtc: request.OccurredAtUtc,
            externalReferenceId: request.ExternalReferenceId,
            reasonCode: null,
            adjustmentDirection: null,
            cancellationToken);
    }

    public Task<TransactionCommandResponse> PostWithdrawalAsync(
        PostCashWithdrawalRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostCashMovementAsync(
            userId: request.UserId,
            idempotencyKey: request.IdempotencyKey,
            transactionKind: TransactionKind.CashWithdrawal,
            direction: LedgerDirection.Debit,
            amount: request.Amount,
            occurredAtUtc: request.OccurredAtUtc,
            externalReferenceId: request.ExternalReferenceId,
            reasonCode: null,
            adjustmentDirection: null,
            cancellationToken);
    }

    public Task<TransactionCommandResponse> PostAdjustmentAsync(
        PostCashAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Direction))
        {
            throw new TransactionProcessingValidationException("A valid cash adjustment direction is required.");
        }

        return PostCashMovementAsync(
            userId: request.UserId,
            idempotencyKey: request.IdempotencyKey,
            transactionKind: TransactionKind.CashAdjustment,
            direction: request.Direction == CashAdjustmentDirection.Credit
                ? LedgerDirection.Credit
                : LedgerDirection.Debit,
            amount: request.Amount,
            occurredAtUtc: request.OccurredAtUtc,
            externalReferenceId: null,
            reasonCode: request.ReasonCode,
            adjustmentDirection: request.Direction,
            cancellationToken);
    }

    private async Task<TransactionCommandResponse> PostCashMovementAsync(
        long userId,
        string idempotencyKey,
        TransactionKind transactionKind,
        LedgerDirection direction,
        decimal amount,
        DateTime occurredAtUtc,
        string? externalReferenceId,
        string? reasonCode,
        CashAdjustmentDirection? adjustmentDirection,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateIdempotencyKey(idempotencyKey);
        ValidateAmount(amount);

        var requestType = transactionKind.ToString();
        var existingResponse = await TryGetExistingResponseAsync(idempotencyKey, requestType, userId, cancellationToken);
        if (existingResponse is not null)
        {
            return existingResponse;
        }

        var normalizedOccurredAtUtc = NormalizeUtc(occurredAtUtc);
        var normalizedAmount = NormalizeCurrency(amount);
        var now = DateTime.UtcNow;

        await using var databaseTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var postingBatch = CreatePostingBatch(userId, idempotencyKey, requestType, now);
            await DbContext.PostingBatches.AddAsync(postingBatch, cancellationToken);

            var balance = await GetOrCreateBalanceAsync(userId, now, cancellationToken);
            ApplyCashMovement(balance, direction, normalizedAmount, transactionKind);

            var description = TransactionDescriptionService.DescribeCashMovement(transactionKind, normalizedAmount, adjustmentDirection);
            var ledgerTransaction = CreateLedgerTransaction(
                postingBatch.Id,
                userId,
                transactionKind,
                direction,
                normalizedAmount,
                description,
                normalizedOccurredAtUtc,
                now,
                externalReferenceId);

            await DbContext.LedgerTransactions.AddAsync(ledgerTransaction, cancellationToken);
            await DbContext.CashMovements.AddAsync(
                new CashMovementEntity
                {
                    TransactionId = ledgerTransaction.TransactionId,
                    UserId = userId,
                    MovementType = transactionKind switch
                    {
                        TransactionKind.CashDeposit => CashMovementType.Deposit.ToString(),
                        TransactionKind.CashWithdrawal => CashMovementType.Withdrawal.ToString(),
                        TransactionKind.CashAdjustment => CashMovementType.Adjustment.ToString(),
                        _ => throw new ArgumentOutOfRangeException(nameof(transactionKind), transactionKind, null)
                    },
                    ExternalReferenceId = externalReferenceId,
                    ReasonCode = reasonCode,
                    OccurredAtUtc = normalizedOccurredAtUtc,
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
            Logger.LogError(exception, "Cash movement posting failed for user {UserId}.", userId);
            throw new TransactionProcessingDependencyException("Failed to persist the cash movement transaction.", exception);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Cash movement posting failed unexpectedly for user {UserId}.", userId);
            throw new TransactionProcessingServiceException("Cash movement posting failed unexpectedly.", exception);
        }
    }

    private static void ApplyCashMovement(
        AccountBalanceEntity balance,
        LedgerDirection direction,
        decimal amount,
        TransactionKind transactionKind)
    {
        balance.CashBalance = direction switch
        {
            LedgerDirection.Credit => NormalizeCurrency(balance.CashBalance + amount),
            LedgerDirection.Debit when transactionKind == TransactionKind.CashAdjustment => NormalizeCurrency(balance.CashBalance - amount),
            LedgerDirection.Debit when balance.CashBalance >= amount => NormalizeCurrency(balance.CashBalance - amount),
            _ => throw new TransactionProcessingConflictException(
                $"The user does not have enough cash available to post the {transactionKind} transaction.")
        };

        balance.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateUserId(long userId)
    {
        if (userId is < 1000000000 or > 9999999999)
        {
            throw new TransactionProcessingValidationException("A valid 10 digit user ID is required.");
        }
    }

    private static void ValidateIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new TransactionProcessingValidationException("An idempotency key is required.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new TransactionProcessingValidationException("Transaction amounts must be greater than zero.");
        }
    }
}
