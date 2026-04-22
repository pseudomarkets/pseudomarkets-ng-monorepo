using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Exceptions;
using PseudoMarkets.ReferenceData.TradingInstruments.Core.Models;
using PseudoMarkets.ReferenceData.TradingInstruments.Persistence.Repositories;
using PseudoMarkets.Shared.Entities.Database;
using Shouldly;

namespace PseudoMarkets.ReferenceData.TradingInstruments.Tests.Persistence;

[TestFixture]
public sealed class TradingInstrumentRepositoryTests
{
    private SqliteConnection _connection = null!;
    private PseudoMarketsDbContext _dbContext = null!;
    private TradingInstrumentRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PseudoMarketsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new PseudoMarketsDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new TradingInstrumentRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task AddAsync_PersistsInstrument()
    {
        var instrument = CreateInstrument("AAPL");

        await _repository.AddAsync(instrument, CancellationToken.None);

        var savedInstrument = await _repository.GetBySymbolAsync("AAPL", CancellationToken.None);
        savedInstrument.ShouldNotBeNull();
        savedInstrument.Symbol.ShouldBe("AAPL");
        savedInstrument.TradingStatus.ShouldBeTrue();
    }

    [Test]
    public async Task UpdateClosingPriceAsync_UpdatesPriceAndDate()
    {
        await _repository.AddAsync(CreateInstrument("MSFT"), CancellationToken.None);

        var updatedInstrument = await _repository.UpdateClosingPriceAsync(
            "MSFT",
            456.78,
            new DateOnly(2026, 4, 21),
            CancellationToken.None);

        updatedInstrument.ClosingPrice.ShouldBe(456.78);
        updatedInstrument.ClosingPriceDate.ShouldBe(new DateOnly(2026, 4, 21));
    }

    [Test]
    public async Task UpdateClosingPriceAsync_WhenInstrumentDoesNotExist_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<TradingInstrumentNotFoundException>(
            () => _repository.UpdateClosingPriceAsync(
                "MISSING",
                1,
                new DateOnly(2026, 4, 21),
                CancellationToken.None));
    }

    private static TradingInstrument CreateInstrument(string symbol)
    {
        return new TradingInstrument
        {
            Symbol = symbol,
            Description = $"{symbol} Description",
            TradingStatus = true,
            PrimaryInstrumentType = "Equity",
            SecondaryInstrumentType = "Common Stock",
            ClosingPrice = 123.45,
            ClosingPriceDate = new DateOnly(2026, 4, 21),
            Source = "Unit Test"
        };
    }
}
