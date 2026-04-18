using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Tests.Support;

namespace PseudoMarkets.TransactionProcessing.Tests.Core;

[TestFixture]
public class CashMovementPostingServiceTests : TransactionProcessingTestBase
{
    [Test]
    public async Task PostDepositAsync_ShouldPersistBatchLedgerCashMovementAndBalance()
    {
        var response = await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "deposit-1",
            UserId = 1000000001,
            Amount = 100m,
            OccurredAtUtc = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc),
            ExternalReferenceId = "dep-001"
        });

        response.Status.ShouldBe("Posted");
        response.TransactionDescription.ShouldBe("CASH DEPOSIT $100.00");

        var balance = await DbContext.AccountBalances.SingleAsync();
        balance.UserId.ShouldBe(1000000001);
        balance.CashBalance.ShouldBe(100m);

        (await DbContext.PostingBatches.CountAsync()).ShouldBe(1);
        (await DbContext.LedgerTransactions.CountAsync()).ShouldBe(1);
        (await DbContext.CashMovements.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task PostDepositAsync_ShouldReturnExistingTransactionForIdempotentReplay()
    {
        var request = new PostCashDepositRequest
        {
            IdempotencyKey = "deposit-idempotent",
            UserId = 1000000001,
            Amount = 25m,
            OccurredAtUtc = DateTime.UtcNow,
            ExternalReferenceId = "dep-002"
        };

        var first = await CashMovementPostingService.PostDepositAsync(request);
        var second = await CashMovementPostingService.PostDepositAsync(request);

        second.TransactionId.ShouldBe(first.TransactionId);
        second.PostingBatchId.ShouldBe(first.PostingBatchId);
        second.Message.ShouldNotBeNull();
        second.Message.ShouldContain("idempotency");
        (await DbContext.LedgerTransactions.CountAsync()).ShouldBe(1);
        (await DbContext.CashMovements.CountAsync()).ShouldBe(1);
        (await DbContext.AccountBalances.SingleAsync()).CashBalance.ShouldBe(25m);
    }

    [Test]
    public async Task PostWithdrawalAsync_ShouldThrowWhenBalanceIsInsufficient()
    {
        var exception = await Should.ThrowAsync<TransactionProcessingConflictException>(() =>
            CashMovementPostingService.PostWithdrawalAsync(new PostCashWithdrawalRequest
            {
                IdempotencyKey = "withdraw-1",
                UserId = 1000000001,
                Amount = 10m,
                OccurredAtUtc = DateTime.UtcNow,
                ExternalReferenceId = "wd-001"
            }));

        exception.Message.ShouldContain("enough cash");
    }

    [Test]
    public async Task PostAdjustmentAsync_ShouldAllowDebitAdjustmentsToDriveBalanceNegative()
    {
        var response = await CashMovementPostingService.PostAdjustmentAsync(new PostCashAdjustmentRequest
        {
            IdempotencyKey = "adjust-1",
            UserId = 1000000001,
            Amount = 15m,
            Direction = CashAdjustmentDirection.Debit,
            OccurredAtUtc = DateTime.UtcNow,
            ReasonCode = "ADMIN_CORRECTION"
        });

        response.TransactionDescription.ShouldBe("CASH ADJUSTMENT DEBIT $15.00");
        (await DbContext.AccountBalances.SingleAsync()).CashBalance.ShouldBe(-15m);
    }
}
