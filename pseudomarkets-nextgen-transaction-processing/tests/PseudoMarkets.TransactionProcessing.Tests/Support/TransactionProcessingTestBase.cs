using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PseudoMarkets.TransactionProcessing.Core.Interfaces;
using PseudoMarkets.TransactionProcessing.Core.Services;
using PseudoMarkets.TransactionProcessing.Persistence.Database;

namespace PseudoMarkets.TransactionProcessing.Tests.Support;

public abstract class TransactionProcessingTestBase
{
    private SqliteConnection? _connection;
    protected TransactionProcessingDbContext DbContext = null!;
    protected ITransactionDescriptionService DescriptionService = null!;
    protected CashMovementPostingService CashMovementPostingService = null!;
    protected TradeTransactionPostingService TradeTransactionPostingService = null!;
    protected VoidTransactionService VoidTransactionService = null!;

    [SetUp]
    public void BaseSetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TransactionProcessingDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new TransactionProcessingDbContext(options);
        DbContext.Database.EnsureCreated();

        DescriptionService = new TransactionDescriptionService();
        CashMovementPostingService = new CashMovementPostingService(
            DbContext,
            DescriptionService,
            NullLogger<CashMovementPostingService>.Instance);
        TradeTransactionPostingService = new TradeTransactionPostingService(
            DbContext,
            DescriptionService,
            NullLogger<TradeTransactionPostingService>.Instance);
        VoidTransactionService = new VoidTransactionService(
            DbContext,
            DescriptionService,
            NullLogger<VoidTransactionService>.Instance);
    }

    [TearDown]
    public void BaseTearDown()
    {
        DbContext.Dispose();
        _connection?.Dispose();
    }
}
