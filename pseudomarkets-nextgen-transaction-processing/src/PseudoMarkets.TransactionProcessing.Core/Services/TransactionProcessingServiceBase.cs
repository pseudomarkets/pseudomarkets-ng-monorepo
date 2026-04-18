using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.TransactionProcessing.Persistence.Database;
using PseudoMarkets.TransactionProcessing.Persistence.Entities;

namespace PseudoMarkets.TransactionProcessing.Core.Services;

public abstract class TransactionProcessingServiceBase
{
    protected TransactionProcessingServiceBase(
        TransactionProcessingDbContext dbContext,
        ITransactionDescriptionService transactionDescriptionService,
        ILogger logger)
    {
        DbContext = dbContext;
        TransactionDescriptionService = transactionDescriptionService;
        Logger = logger;
    }

    protected TransactionProcessingDbContext DbContext { get; }
    protected ITransactionDescriptionService TransactionDescriptionService { get; }
    protected ILogger Logger { get; }

    protected async Task<TransactionCommandResponse?> TryGetExistingResponseAsync(
        string idempotencyKey,
        string expectedRequestType,
        long? expectedUserId,
        CancellationToken cancellationToken)
    {
        var existingBatch = await DbContext.PostingBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existingBatch is null)
        {
            return null;
        }

        if (!string.Equals(existingBatch.RequestType, expectedRequestType, StringComparison.Ordinal))
        {
            throw new TransactionProcessingConflictException(
                $"The idempotency key '{idempotencyKey}' has already been used for a different transaction request.");
        }

        if (expectedUserId.HasValue && existingBatch.UserId != expectedUserId.Value)
        {
            throw new TransactionProcessingConflictException(
                $"The idempotency key '{idempotencyKey}' has already been used for a different user.");
        }

        if (string.Equals(existingBatch.Status, PostingBatchStatus.Pending.ToString(), StringComparison.Ordinal))
        {
            throw new TransactionProcessingConflictException(
                $"The transaction request for idempotency key '{idempotencyKey}' is still pending.");
        }

        if (string.Equals(existingBatch.Status, PostingBatchStatus.Rejected.ToString(), StringComparison.Ordinal))
        {
            throw new TransactionProcessingConflictException(
                existingBatch.ErrorMessage
                ?? $"The transaction request for idempotency key '{idempotencyKey}' was previously rejected.");
        }

        var existingTransaction = await DbContext.LedgerTransactions
            .AsNoTracking()
            .Where(x => x.PostingBatchId == existingBatch.Id)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTransaction is null)
        {
            throw new TransactionProcessingServiceException(
                $"The transaction request for idempotency key '{idempotencyKey}' has no persisted ledger transaction.");
        }

        return CreateResponse(existingBatch.Id, existingTransaction, "Existing transaction returned for idempotency replay.");
    }

    protected static TransactionCommandResponse CreateResponse(
        Guid postingBatchId,
        LedgerTransactionEntity transaction,
        string? message = null)
    {
        return new TransactionCommandResponse
        {
            PostingBatchId = postingBatchId,
            TransactionId = transaction.TransactionId,
            Status = transaction.Status,
            TransactionDescription = transaction.TransactionDescription,
            Message = message
        };
    }

    protected static PostingBatchEntity CreatePostingBatch(long userId, string idempotencyKey, string requestType, DateTime createdAtUtc)
    {
        return new PostingBatchEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            RequestType = requestType,
            Status = PostingBatchStatus.Pending.ToString(),
            CreatedAtUtc = createdAtUtc
        };
    }

    protected static LedgerTransactionEntity CreateLedgerTransaction(
        Guid postingBatchId,
        long userId,
        TransactionKind transactionKind,
        LedgerDirection direction,
        decimal amount,
        string description,
        DateTime occurredAtUtc,
        DateTime createdAtUtc,
        string? externalReferenceId = null,
        Guid? voidsTransactionId = null)
    {
        return new LedgerTransactionEntity
        {
            TransactionId = Guid.NewGuid(),
            PostingBatchId = postingBatchId,
            UserId = userId,
            TransactionKind = transactionKind.ToString(),
            Direction = direction.ToString(),
            Amount = NormalizeCurrency(amount),
            TransactionDescription = description,
            Status = TransactionStatus.Posted.ToString(),
            OccurredAtUtc = occurredAtUtc,
            ExternalReferenceId = externalReferenceId,
            VoidsTransactionId = voidsTransactionId,
            CreatedAtUtc = createdAtUtc
        };
    }

    protected static void MarkBatchPosted(PostingBatchEntity batch, DateTime completedAtUtc)
    {
        batch.Status = PostingBatchStatus.Posted.ToString();
        batch.CompletedAtUtc = completedAtUtc;
        batch.ErrorMessage = null;
    }

    protected static void MarkBatchRejected(PostingBatchEntity batch, string message, DateTime completedAtUtc)
    {
        batch.Status = PostingBatchStatus.Rejected.ToString();
        batch.CompletedAtUtc = completedAtUtc;
        batch.ErrorMessage = message;
    }

    protected async Task<AccountBalanceEntity> GetOrCreateBalanceAsync(long userId, DateTime timestampUtc, CancellationToken cancellationToken)
    {
        var balance = await DbContext.AccountBalances
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        balance = new AccountBalanceEntity
        {
            UserId = userId,
            CashBalance = 0m,
            UpdatedAtUtc = timestampUtc
        };

        await DbContext.AccountBalances.AddAsync(balance, cancellationToken);
        return balance;
    }

    protected static DateTime NormalizeUtc(DateTime value)
    {
        if (value == default)
        {
            return DateTime.UtcNow;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    protected static string NormalizeSymbol(string symbol)
    {
        return symbol.Trim().ToUpperInvariant();
    }

    protected static decimal NormalizeCurrency(decimal amount)
    {
        return decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
    }

    protected static decimal NormalizeQuantity(decimal amount)
    {
        return decimal.Round(amount, 6, MidpointRounding.AwayFromZero);
    }

    protected static decimal NormalizeUnitPrice(decimal amount)
    {
        return decimal.Round(amount, 6, MidpointRounding.AwayFromZero);
    }

    protected static TransactionKind ParseTransactionKind(string value)
    {
        if (Enum.TryParse<TransactionKind>(value, out var transactionKind))
        {
            return transactionKind;
        }

        throw new TransactionProcessingServiceException($"Unrecognized transaction kind '{value}'.");
    }

    protected static LedgerDirection ParseLedgerDirection(string value)
    {
        if (Enum.TryParse<LedgerDirection>(value, out var direction))
        {
            return direction;
        }

        throw new TransactionProcessingServiceException($"Unrecognized ledger direction '{value}'.");
    }

    protected async Task<TradeExecutionEntity> GetRequiredTradeExecutionAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var tradeExecution = await DbContext.TradeExecutions
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

        if (tradeExecution is null)
        {
            throw new TransactionProcessingServiceException(
                $"No trade execution record was found for transaction '{transactionId:D}'.");
        }

        return tradeExecution;
    }

    protected async Task<CashMovementEntity> GetRequiredCashMovementAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var cashMovement = await DbContext.CashMovements
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

        if (cashMovement is null)
        {
            throw new TransactionProcessingServiceException(
                $"No cash movement record was found for transaction '{transactionId:D}'.");
        }

        return cashMovement;
    }

    protected async Task<bool> HasLaterActiveTradeForSymbolAsync(
        long userId,
        string symbol,
        DateTime originalExecutedAtUtc,
        Guid originalTransactionId,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);

        return await (
                from tradeExecution in DbContext.TradeExecutions
                join ledgerTransaction in DbContext.LedgerTransactions
                    on tradeExecution.TransactionId equals ledgerTransaction.TransactionId
                where tradeExecution.UserId == userId
                      && tradeExecution.Symbol == normalizedSymbol
                      && tradeExecution.TransactionId != originalTransactionId
                      && ledgerTransaction.Status != TransactionStatus.Voided.ToString()
                      && tradeExecution.ExecutedAtUtc >= originalExecutedAtUtc
                select tradeExecution.Id)
            .AnyAsync(cancellationToken);
    }
}
