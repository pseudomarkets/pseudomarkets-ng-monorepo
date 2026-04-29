using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using PseudoMarkets.TransactionProcessing.Core.Exceptions;
using PseudoMarkets.TransactionProcessing.Tests.Support;

namespace PseudoMarkets.TransactionProcessing.Tests.Core;

[TestFixture]
public class TradeTransactionPostingServiceTests : TransactionProcessingTestBase
{
    [Test]
    public async Task PostTradeAsync_BuyShouldCreatePositionAndLotAndDebitCash()
    {
        await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "seed-cash",
            UserId = 1000000001,
            Amount = 500m,
            OccurredAtUtc = DateTime.UtcNow,
            ExternalReferenceId = "dep-seed"
        });

        var response = await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "buy-1",
            UserId = 1000000001,
            Symbol = "aapl",
            TradeSide = TradeSide.Buy,
            Quantity = 2m,
            ExecutionPrice = 100m,
            GrossAmount = 200m,
            Fees = 1m,
            NetAmount = 201m,
            ExecutedAtUtc = DateTime.UtcNow,
            ExternalExecutionId = "exec-buy-1"
        });

        response.TransactionDescription.ShouldBe("TRADE BUY AAPL $201.00");
        var balance = await DbContext.AccountBalances.SingleAsync();
        balance.CashBalance.ShouldBe(299m);
        balance.SettledCashBalance.ShouldBe(299m);
        balance.UnsettledCashBalance.ShouldBe(0m);

        var position = await DbContext.Positions.SingleAsync();
        position.Symbol.ShouldBe("AAPL");
        position.Quantity.ShouldBe(2m);
        position.SettledQuantity.ShouldBe(0m);
        position.UnsettledQuantity.ShouldBe(2m);
        position.CostBasisTotal.ShouldBe(201m);
        position.SettledCostBasisTotal.ShouldBe(0m);
        position.UnsettledCostBasisTotal.ShouldBe(201m);

        var lot = await DbContext.PositionLots.SingleAsync();
        lot.QuantityOpened.ShouldBe(2m);
        lot.QuantityRemaining.ShouldBe(2m);
        lot.SettledQuantityRemaining.ShouldBe(0m);
        lot.UnsettledQuantityRemaining.ShouldBe(2m);
        lot.OpeningTransactionId.ShouldBe(response.TransactionId);
    }

    [Test]
    public async Task PostTradeAsync_ShouldPersistTradeDateAndSettlementDate()
    {
        await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
        {
            IdempotencyKey = "seed-cash-dates",
            UserId = 1000000001,
            Amount = 500m,
            OccurredAtUtc = DateTime.UtcNow,
            ExternalReferenceId = "dep-seed-dates"
        });

        var response = await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "buy-dates-1",
            UserId = 1000000001,
            Symbol = "MSFT",
            TradeSide = TradeSide.Buy,
            Quantity = 1m,
            ExecutionPrice = 100m,
            GrossAmount = 100m,
            Fees = 0m,
            NetAmount = 100m,
            ExecutedAtUtc = new DateTime(2026, 1, 15, 20, 30, 0, DateTimeKind.Utc),
            ExternalExecutionId = "exec-buy-dates-1"
        });

        var tradeExecution = await DbContext.TradeExecutions.SingleAsync(x => x.TransactionId == response.TransactionId);
        tradeExecution.TradeDate.ShouldBe(new DateOnly(2026, 1, 15));
        tradeExecution.SettlementDate.ShouldBe(new DateOnly(2026, 1, 16));
    }

    [Test]
    public async Task PostTradeAsync_SellShouldConsumeLotsFifoAndCreditCash()
    {
        await SeedBuyAsync("buy-a", 2m, 100m, 0m, "exec-buy-a", settle: true);
        await SeedBuyAsync("buy-b", 1m, 120m, 0m, "exec-buy-b", settle: true);

        var response = await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = "sell-1",
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Sell,
            Quantity = 2.5m,
            ExecutionPrice = 130m,
            GrossAmount = 325m,
            Fees = 5m,
            NetAmount = 320m,
            ExecutedAtUtc = DateTime.UtcNow,
            ExternalExecutionId = "exec-sell-1"
        });

        response.TransactionDescription.ShouldBe("TRADE SELL AAPL $320.00");
        var balance = await DbContext.AccountBalances.SingleAsync();
        balance.CashBalance.ShouldBe(500m);
        balance.SettledCashBalance.ShouldBe(180m);
        balance.UnsettledCashBalance.ShouldBe(320m);

        var position = await DbContext.Positions.SingleAsync();
        position.Quantity.ShouldBe(0.5m);
        position.SettledQuantity.ShouldBe(0.5m);
        position.UnsettledQuantity.ShouldBe(0m);
        position.Symbol.ShouldBe("AAPL");

        var lots = await DbContext.PositionLots.OrderBy(x => x.Id).ToListAsync();
        lots[0].QuantityRemaining.ShouldBe(0m);
        lots[0].SettledQuantityRemaining.ShouldBe(0m);
        lots[1].QuantityRemaining.ShouldBe(0.5m);
        lots[1].SettledQuantityRemaining.ShouldBe(0.5m);
        (await DbContext.PositionLotClosures.CountAsync()).ShouldBe(2);
    }

    [Test]
    public async Task PostTradeAsync_ShouldRejectSellWhenSettledQuantityIsUnavailable()
    {
        await SeedBuyAsync("buy-c", 1m, 50m, 0m, "exec-buy-c");

        var exception = await Should.ThrowAsync<TransactionProcessingConflictException>(() =>
            TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
            {
                IdempotencyKey = "sell-too-much",
                UserId = 1000000001,
                Symbol = "AAPL",
                TradeSide = TradeSide.Sell,
                Quantity = 2m,
                ExecutionPrice = 55m,
                GrossAmount = 110m,
                Fees = 0m,
                NetAmount = 110m,
                ExecutedAtUtc = DateTime.UtcNow,
                ExternalExecutionId = "exec-sell-too-much"
            }));

        exception.Message.ShouldContain("settled position quantity");
    }

    [Test]
    public async Task PostTradeAsync_ShouldRejectSellWhenSettledLotQuantityIsUnavailable()
    {
        await SeedBuyAsync("buy-d", 1m, 50m, 0m, "exec-buy-d", settle: true);

        var position = await DbContext.Positions.SingleAsync();
        position.SettledQuantity = 1m;

        var lot = await DbContext.PositionLots.SingleAsync();
        lot.SettledQuantityRemaining = 0m;
        await DbContext.SaveChangesAsync();

        var exception = await Should.ThrowAsync<TransactionProcessingConflictException>(() =>
            TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
            {
                IdempotencyKey = "sell-no-settled-lots",
                UserId = 1000000001,
                Symbol = "AAPL",
                TradeSide = TradeSide.Sell,
                Quantity = 1m,
                ExecutionPrice = 55m,
                GrossAmount = 55m,
                Fees = 0m,
                NetAmount = 55m,
                ExecutedAtUtc = DateTime.UtcNow,
                ExternalExecutionId = "exec-sell-no-settled-lots"
            }));

        exception.Message.ShouldContain("settled lot inventory");
    }

    private async Task SeedBuyAsync(
        string idempotencyKey,
        decimal quantity,
        decimal executionPrice,
        decimal fees,
        string externalExecutionId,
        bool settle = false)
    {
        if (!await DbContext.AccountBalances.AnyAsync())
        {
            await CashMovementPostingService.PostDepositAsync(new PostCashDepositRequest
            {
                IdempotencyKey = "seed-initial-cash",
                UserId = 1000000001,
                Amount = 500m,
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-10),
                ExternalReferenceId = "dep-initial"
            });
        }

        var grossAmount = quantity * executionPrice;
        await TradeTransactionPostingService.PostTradeAsync(new PostTradeTransactionRequest
        {
            IdempotencyKey = idempotencyKey,
            UserId = 1000000001,
            Symbol = "AAPL",
            TradeSide = TradeSide.Buy,
            Quantity = quantity,
            ExecutionPrice = executionPrice,
            GrossAmount = grossAmount,
            Fees = fees,
            NetAmount = grossAmount + fees,
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExternalExecutionId = externalExecutionId
        });

        if (settle)
        {
            var position = await DbContext.Positions.SingleAsync(x => x.UserId == 1000000001 && x.Symbol == "AAPL");
            position.SettledQuantity = position.Quantity;
            position.UnsettledQuantity = 0m;
            position.SettledCostBasisTotal = position.CostBasisTotal;
            position.UnsettledCostBasisTotal = 0m;

            var lots = await DbContext.PositionLots
                .Where(x => x.UserId == 1000000001 && x.Symbol == "AAPL")
                .ToListAsync();
            foreach (var lot in lots)
            {
                lot.SettledQuantityRemaining = lot.QuantityRemaining;
                lot.UnsettledQuantityRemaining = 0m;
            }

            await DbContext.SaveChangesAsync();
        }
    }
}
