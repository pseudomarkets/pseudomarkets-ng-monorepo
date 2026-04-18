using NUnit.Framework;
using Shouldly;
using PseudoMarkets.TransactionProcessing.Contracts.Enums;
using PseudoMarkets.TransactionProcessing.Core.Services;

namespace PseudoMarkets.TransactionProcessing.Tests.Core;

[TestFixture]
public class TransactionDescriptionServiceTests
{
    [Test]
    public void DescribeCashMovement_ShouldDescribeDeposit()
    {
        var sut = new TransactionDescriptionService();

        var description = sut.DescribeCashMovement(TransactionKind.CashDeposit, 100m);

        description.ShouldBe("CASH DEPOSIT $100.00");
    }

    [Test]
    public void DescribeTrade_ShouldDescribeBuyTrade()
    {
        var sut = new TransactionDescriptionService();

        var description = sut.DescribeTrade(TransactionKind.TradeBuy, "aapl", 250.5m);

        description.ShouldBe("TRADE BUY AAPL $250.50");
    }

    [Test]
    public void DescribeVoid_ShouldPrefixOriginalDescription()
    {
        var sut = new TransactionDescriptionService();

        var description = sut.DescribeVoid("CASH WITHDRAWAL $100.00");

        description.ShouldBe("VOID CASH WITHDRAWAL $100.00");
    }
}
