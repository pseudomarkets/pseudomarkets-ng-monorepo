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
        var balance = await DbContext.AccountBalances.SingleAsync();
        balance.CashBalance.ShouldBe(0m);
        balance.SettledCashBalance.ShouldBe(0m);
        balance.UnsettledCashBalance.ShouldBe(0m);
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

        await MarkAllPositionsAndLotsSettledAsync();

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
        var balance = await DbContext.AccountBalances.SingleAsync();
        balance.CashBalance.ShouldBe(300m);
        balance.SettledCashBalance.ShouldBe(300m);
        balance.UnsettledCashBalance.ShouldBe(0m);

        var position = await DbContext.Positions.SingleAsync();
        position.Quantity.ShouldBe(2m);
        position.SettledQuantity.ShouldBe(2m);
        position.UnsettledQuantity.ShouldBe(0m);
        position.CostBasisTotal.ShouldBe(200m);
        position.SettledCostBasisTotal.ShouldBe(200m);
        position.UnsettledCostBasisTotal.ShouldBe(0m);

        var lot = await DbContext.PositionLots.SingleAsync();
        lot.QuantityRemaining.ShouldBe(2m);
        lot.SettledQuantityRemaining.ShouldBe(2m);
        lot.UnsettledQuantityRemaining.ShouldBe(0m);
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

    private async Task MarkAllPositionsAndLotsSettledAsync()
    {
        var positions = await DbContext.Positions.ToListAsync();
        foreach (var position in positions)
        {
            position.SettledQuantity = position.Quantity;
            position.UnsettledQuantity = 0m;
            position.SettledCostBasisTotal = position.CostBasisTotal;
            position.UnsettledCostBasisTotal = 0m;
        }

        var lots = await DbContext.PositionLots.ToListAsync();
        foreach (var lot in lots)
        {
            lot.SettledQuantityRemaining = lot.QuantityRemaining;
            lot.UnsettledQuantityRemaining = 0m;
        }

        await DbContext.SaveChangesAsync();
    }
}
