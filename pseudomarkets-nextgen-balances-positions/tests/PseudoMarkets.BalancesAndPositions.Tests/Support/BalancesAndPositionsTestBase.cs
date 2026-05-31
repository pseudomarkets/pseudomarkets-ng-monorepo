using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.BalancesAndPositions.Tests.Support;

public abstract class BalancesAndPositionsTestBase
{
    private SqliteConnection? _connection;

    protected PseudoMarketsDbContext DbContext = null!;

    [SetUp]
    public void BaseSetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PseudoMarketsDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new PseudoMarketsDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    [TearDown]
    public void BaseTearDown()
    {
        DbContext.Dispose();
        _connection?.Dispose();
    }
}
