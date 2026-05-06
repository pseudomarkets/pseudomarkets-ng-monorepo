using Moq;
using NUnit.Framework;
using PseudoMarkets.MarketData.Contracts.Quotes;
using PseudoMarkets.OrderExecution.Contracts.Enums;
using PseudoMarkets.OrderExecution.Contracts.Orders;
using PseudoMarkets.OrderExecution.Core.Exceptions;
using PseudoMarkets.OrderExecution.Core.Interfaces;
using PseudoMarkets.OrderExecution.Core.Models;
using PseudoMarkets.OrderExecution.Core.Services;
using PseudoMarkets.Shared.Authorization.Constants;
using PseudoMarkets.OrderExecution.Tests.Support;
using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;
using PseudoMarkets.TransactionProcessing.Contracts.Transactions;
using Shouldly;

namespace PseudoMarkets.OrderExecution.Tests.Core;

[TestFixture]
public sealed class OrderSubmissionServiceTests
{
    private Mock<ITradingInstrumentsClient> _tradingInstrumentsClient = null!;
    private Mock<IMarketDataClient> _marketDataClient = null!;
    private Mock<ITransactionProcessingClient> _transactionProcessingClient = null!;
    private FakeOrderExecutionRepository _repository = null!;
    private FixedClock _clock = null!;
    private OrderSubmissionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tradingInstrumentsClient = new Mock<ITradingInstrumentsClient>();
        _marketDataClient = new Mock<IMarketDataClient>();
        _transactionProcessingClient = new Mock<ITransactionProcessingClient>();
        _repository = new FakeOrderExecutionRepository();
        _clock = new FixedClock();
        _sut = new OrderSubmissionService(
            _tradingInstrumentsClient.Object,
            _marketDataClient.Object,
            _transactionProcessingClient.Object,
            _repository,
            _clock);
    }

    [Test]
    public async Task SubmitAsync_ShouldRejectUserTokenForDifferentPayloadUser()
    {
        var exception = await Should.ThrowAsync<OrderExecutionAuthorizationException>(() =>
            _sut.SubmitAsync(
                CreateRequest(userId: 1000000002),
                new OrderCallerContext(1000000001, PlatformTokenTypes.User),
                CancellationToken.None));

        exception.Code.ShouldBe(OrderExecutionErrorCodes.UserOwnershipViolation);
    }

    [Test]
    public async Task SubmitAsync_ShouldRejectInvalidSymbolBeforeDependentCalls()
    {
        var exception = await Should.ThrowAsync<OrderExecutionValidationException>(() =>
            _sut.SubmitAsync(
                CreateRequest(symbol: "BRK.B"),
                new OrderCallerContext(1000000001, PlatformTokenTypes.User),
                CancellationToken.None));

        exception.Code.ShouldBe(OrderExecutionErrorCodes.InvalidSymbolFormat);
        _tradingInstrumentsClient.Verify(x => x.GetBySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _marketDataClient.Verify(x => x.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitAsync_BuyShouldUseSettledCashAndPersistFilledOrder()
    {
        SetupTradableInstrument();
        SetupQuote(100m);
        _repository.AccountBalance = new AccountBalanceEntity
        {
            UserId = 1000000001,
            CashBalance = 500m,
            SettledCashBalance = 200m,
            UnsettledCashBalance = 300m
        };
        _transactionProcessingClient
            .Setup(x => x.PostTradeAsync(It.IsAny<PostTradeTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionCommandResponse
            {
                PostingBatchId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid(),
                Status = "Posted",
                TransactionDescription = "TRADE BUY AAPL $200.00"
            });

        var response = await _sut.SubmitAsync(
            CreateRequest(quantity: 2m),
            new OrderCallerContext(1000000001, PlatformTokenTypes.User),
            CancellationToken.None);

        response.Symbol.ShouldBe("AAPL");
        response.FillPrice.ShouldBe(100m);
        response.GrossAmount.ShouldBe(200m);
        response.Status.ShouldBe(OrderStatus.Filled);
        _repository.OrderExecutions.Single().TransactionId.ShouldNotBeNull();
        _transactionProcessingClient.Verify(x => x.PostTradeAsync(
            It.Is<PostTradeTransactionRequest>(request =>
                request.Symbol == "AAPL" &&
                request.ExecutionPrice == 100m &&
                request.GrossAmount == 200m &&
                request.Fees == 0m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_BuyShouldRejectWhenOnlyUnsettledCashCanCoverTrade()
    {
        SetupTradableInstrument();
        SetupQuote(100m);
        _repository.AccountBalance = new AccountBalanceEntity
        {
            UserId = 1000000001,
            CashBalance = 500m,
            SettledCashBalance = 100m,
            UnsettledCashBalance = 400m
        };

        var exception = await Should.ThrowAsync<OrderExecutionValidationException>(() =>
            _sut.SubmitAsync(
                CreateRequest(quantity: 2m),
                new OrderCallerContext(1000000001, PlatformTokenTypes.User),
                CancellationToken.None));

        exception.Code.ShouldBe(OrderExecutionErrorCodes.InsufficientSettledCash);
        _transactionProcessingClient.Verify(x => x.PostTradeAsync(It.IsAny<PostTradeTransactionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitAsync_SellShouldRejectWhenOnlyUnsettledQuantityCanCoverTrade()
    {
        SetupTradableInstrument();
        SetupQuote(100m);
        _repository.Position = new PositionEntity
        {
            UserId = 1000000001,
            Symbol = "AAPL",
            Quantity = 5m,
            SettledQuantity = 1m,
            UnsettledQuantity = 4m
        };

        var exception = await Should.ThrowAsync<OrderExecutionValidationException>(() =>
            _sut.SubmitAsync(
                CreateRequest(side: OrderSide.Sell, quantity: 2m),
                new OrderCallerContext(1000000001, PlatformTokenTypes.User),
                CancellationToken.None));

        exception.Code.ShouldBe(OrderExecutionErrorCodes.InsufficientSettledPosition);
        _transactionProcessingClient.Verify(x => x.PostTradeAsync(It.IsAny<PostTradeTransactionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitAsync_SystemTokenShouldAllowDifferentPayloadUser()
    {
        SetupTradableInstrument();
        SetupQuote(10m);
        _repository.AccountBalance = new AccountBalanceEntity
        {
            UserId = 1000000002,
            CashBalance = 100m,
            SettledCashBalance = 100m
        };
        _transactionProcessingClient
            .Setup(x => x.PostTradeAsync(It.IsAny<PostTradeTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionCommandResponse
            {
                PostingBatchId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid(),
                Status = "Posted",
                TransactionDescription = "TRADE BUY AAPL $10.00"
            });

        var response = await _sut.SubmitAsync(
            CreateRequest(userId: 1000000002, quantity: 1m),
            new OrderCallerContext(1000000001, PlatformTokenTypes.System),
            CancellationToken.None);

        response.UserId.ShouldBe(1000000002);
        response.Status.ShouldBe(OrderStatus.Filled);
    }

    [Test]
    public async Task SubmitAsync_ShouldRejectWhenTokenTypeMetadataIsMissing()
    {
        var exception = await Should.ThrowAsync<OrderExecutionAuthorizationException>(() =>
            _sut.SubmitAsync(
                CreateRequest(),
                new OrderCallerContext(1000000001, string.Empty),
                CancellationToken.None));

        exception.Code.ShouldBe(OrderExecutionErrorCodes.InvalidUser);
    }

    private void SetupTradableInstrument()
    {
        _tradingInstrumentsClient
            .Setup(x => x.GetBySymbolAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TradingInstrumentResponse
            {
                Symbol = "AAPL",
                TradingStatus = true,
                PrimaryInstrumentType = "Equity",
                SecondaryInstrumentType = "Common Stock"
            });
    }

    private void SetupQuote(decimal price)
    {
        _marketDataClient
            .Setup(x => x.GetQuoteAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteResponse
            {
                Symbol = "AAPL",
                Price = price,
                Source = "Test",
                TimestampUtc = DateTimeOffset.UtcNow
            });
    }

    private static SubmitOrderRequest CreateRequest(
        long userId = 1000000001,
        string symbol = " aapl ",
        OrderSide side = OrderSide.Buy,
        decimal quantity = 1m)
    {
        return new SubmitOrderRequest
        {
            UserId = userId,
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            OrderType = OrderType.Market
        };
    }
}
