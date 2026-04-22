using Moq;
using NUnit.Framework;
using PseudoMarkets.ReferenceData.TradingInstruments.Contracts.Instruments;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Interfaces;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Services;
using Shouldly;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Tests.Core;

[TestFixture]
public sealed class TradingInstrumentServiceTests
{
    private Mock<ITradingInstrumentRepository> _repository = null!;
    private TradingInstrumentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ITradingInstrumentRepository>(MockBehavior.Strict);
        _service = new TradingInstrumentService(_repository.Object);
    }

    [Test]
    public async Task CreateAsync_NormalizesSymbolAndDefaultsTradingStatusToTrue()
    {
        TradingInstrument? savedInstrument = null;
        var request = new CreateTradingInstrumentRequest
        {
            Symbol = " aapl ",
            Description = "Apple Inc.",
            PrimaryInstrumentType = "Equity",
            SecondaryInstrumentType = "Common Stock",
            Source = "Manual",
            ClosingPrice = 100.25,
            ClosingPriceDate = new DateOnly(2026, 4, 21)
        };

        _repository
            .Setup(x => x.ExistsAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository
            .Setup(x => x.AddAsync(It.IsAny<TradingInstrument>(), It.IsAny<CancellationToken>()))
            .Callback<TradingInstrument, CancellationToken>((instrument, _) => savedInstrument = instrument)
            .ReturnsAsync((TradingInstrument instrument, CancellationToken _) => instrument);

        var response = await _service.CreateAsync(request, CancellationToken.None);

        savedInstrument.ShouldNotBeNull();
        savedInstrument.Symbol.ShouldBe("AAPL");
        savedInstrument.TradingStatus.ShouldBeTrue();
        response.Symbol.ShouldBe("AAPL");
        response.TradingStatus.ShouldBeTrue();
        response.ClosingPriceDate.ShouldBe(new DateOnly(2026, 4, 21));
    }

    [Test]
    public async Task CreateAsync_WhenSymbolAlreadyExists_ThrowsConflictException()
    {
        var request = new CreateTradingInstrumentRequest
        {
            Symbol = "AAPL",
            Description = "Apple Inc.",
            PrimaryInstrumentType = "Equity",
            SecondaryInstrumentType = "Common Stock",
            Source = "Manual"
        };

        _repository
            .Setup(x => x.ExistsAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Should.ThrowAsync<TradingInstrumentConflictException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        exception.Message.ShouldContain("AAPL");
    }

    [Test]
    public async Task GetBySymbolAsync_WhenInstrumentDoesNotExist_ThrowsNotFoundException()
    {
        _repository
            .Setup(x => x.GetBySymbolAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradingInstrument?)null);

        await Should.ThrowAsync<TradingInstrumentNotFoundException>(
            () => _service.GetBySymbolAsync("msft", CancellationToken.None));
    }

    [Test]
    public async Task UpdateClosingPriceAsync_WhenPriceIsNegative_ThrowsValidationException()
    {
        var request = new UpdateClosingPriceRequest { ClosingPrice = -0.01 };

        await Should.ThrowAsync<TradingInstrumentValidationException>(
            () => _service.UpdateClosingPriceAsync("AAPL", request, CancellationToken.None));
    }
}
