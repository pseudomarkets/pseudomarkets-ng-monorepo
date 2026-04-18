using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Tests.Support;

namespace PseudoMarkets.TransactionProcessing.Tests.Core;

[TestFixture]
public class VoidTransactionServiceTests : TransactionProcessingTestBase
{
    [Test]
    public async Task VoidAsync_ShouldReverseCashDeposit()
    {
        var deposit = await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "dep-void",
            UserId = 1000000001,
            Amount = 100m,
            OccurredAtUtc = DateTime.UtcNow,
            ExternalReferenceId = "dep-void-ref"
        });

        var response = await VoidTransactionService.VoidAsync(deposit.TransactionId, new VoidTransactionRequest
        {
            IdempotencyKey = "void-dep-1",
            VoidedAtUtc = DateTime.UtcNow,
            ReasonCode = "TEST_VOID"
        });

        response.TransactionDescription.ShouldBe("VOID CASH DEPOSIT $100.00");
        response.Status.ShouldBe("Posted");

        var original = await DbContext.LedgerTransactions.SingleAsync(x => x.TransactionId == deposit.TransactionId);
        original.Status.ShouldBe("Voided");

        var voidTransaction = await DbContext.LedgerTransactions.SingleAsync(x => x.TransactionId == response.TransactionId);
        voidTransaction.VoidsTransactionId.ShouldBe(deposit.TransactionId);
        (await DbContext.AccountBalances.SingleAsync()).CashBalance.ShouldBe(0m);
    }

    [Test]
    public async Task VoidAsync_ShouldRestoreLotsAndPositionForSellTrade()
    {
        await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "seed-void-sell-cash",
            UserId = 1000000001,
            Amount = 500m,
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ExternalReferenceId = "seed-void-sell"
        });

        await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "buy-void-sell",
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Buy,
            Quantity = 2m,
            ExecutionPrice = 100m,
            GrossAmount = 200m,
            Fees = 0m,
            NetAmount = 200m,
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            ExternalExecutionId = "exec-buy-void-sell"
        });

        var sell = await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "sell-void-sell",
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Sell,
            Quantity = 1m,
            ExecutionPrice = 110m,
            GrossAmount = 110m,
            Fees = 0m,
            NetAmount = 110m,
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExternalExecutionId = "exec-sell-void-sell"
        });

        var response = await VoidTransactionService.VoidAsync(sell.TransactionId, new VoidTransactionRequest
        {
            IdempotencyKey = "void-sell-1",
            VoidedAtUtc = DateTime.UtcNow,
            ReasonCode = "TEST_VOID"
        });

        response.TransactionDescription.ShouldBe("VOID TRADE SELL AAPL $110.00");
        (await DbContext.AccountBalances.SingleAsync()).CashBalance.ShouldBe(300m);

        var position = await DbContext.Positions.SingleAsync();
        position.Quantity.ShouldBe(2m);
        position.CostBasisTotal.ShouldBe(200m);

        var lot = await DbContext.PositionLots.SingleAsync();
        lot.QuantityRemaining.ShouldBe(2m);
    }

    [Test]
    public async Task VoidAsync_ShouldRejectTradeVoidWhenLaterTradeActivityExists()
    {
        await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "seed-void-buy-cash",
            UserId = 1000000001,
            Amount = 500m,
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ExternalReferenceId = "seed-void-buy"
        });

        var originalBuy = await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "buy-to-void",
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Buy,
            Quantity = 1m,
            ExecutionPrice = 100m,
            GrossAmount = 100m,
            Fees = 0m,
            NetAmount = 100m,
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            ExternalExecutionId = "exec-buy-to-void"
        });

        await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "later-buy",
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Buy,
            Quantity = 1m,
            ExecutionPrice = 105m,
            GrossAmount = 105m,
            Fees = 0m,
            NetAmount = 105m,
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExternalExecutionId = "exec-later-buy"
        });

        var exception = await Should.ThrowAsync<TransactionProcessingConflictException>(() =>
            VoidTransactionService.VoidAsync(originalBuy.TransactionId, new VoidTransactionRequest
            {
                IdempotencyKey = "void-buy-blocked",
                VoidedAtUtc = DateTime.UtcNow,
                ReasonCode = "TEST_VOID"
            }));

        exception.Message.ShouldContain("later active trade activity");
    }
}
