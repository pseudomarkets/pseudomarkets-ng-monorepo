using Microsoft.EntityFrameworkCore;
using PseudoMarkets.Shared.Entities.Entities.Platform;
using PseudoMarkets.Shared.Entities.Entities.ReferenceData;
using PseudoMarkets.Shared.Entities.Entities.TransactionProcessing;

namespace PseudoMarkets.Shared.Entities.Database;

public class PseudoMarketsDbContext : DbContext
{
    public PseudoMarketsDbContext(DbContextOptions<PseudoMarketsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PostingBatchEntity> PostingBatches => Set<PostingBatchEntity>();
    public DbSet<LedgerTransactionEntity> LedgerTransactions => Set<LedgerTransactionEntity>();
    public DbSet<TradeExecutionEntity> TradeExecutions => Set<TradeExecutionEntity>();
    public DbSet<CashMovementEntity> CashMovements => Set<CashMovementEntity>();
    public DbSet<AccountBalanceEntity> AccountBalances => Set<AccountBalanceEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<PositionLotEntity> PositionLots => Set<PositionLotEntity>();
    public DbSet<PositionLotClosureEntity> PositionLotClosures => Set<PositionLotClosureEntity>();
    public DbSet<MarketHolidayEntity> MarketHolidays => Set<MarketHolidayEntity>();
    public DbSet<TradingInstrumentEntity> TradingInstruments => Set<TradingInstrumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePlatformModel(modelBuilder);
        ConfigureReferenceDataModel(modelBuilder);
        ConfigureTransactionProcessingModel(modelBuilder);
    }

    private static void ConfigurePlatformModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketHolidayEntity>(entity =>
        {
            entity.ToTable("market_holidays");
            entity.HasKey(x => x.HolidayDate);
            entity.Property(x => x.HolidayDate).HasColumnName("holiday_date").IsRequired();
            entity.Property(x => x.HolidayName).HasColumnName("holiday_name").HasMaxLength(100).IsRequired();
            entity.HasData(
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 1, 1), HolidayName = "New Year's Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 1, 19), HolidayName = "Martin Luther King, Jr. Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 2, 16), HolidayName = "Washington's Birthday" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 4, 3), HolidayName = "Good Friday" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 5, 25), HolidayName = "Memorial Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 6, 19), HolidayName = "Juneteenth National Independence Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 7, 3), HolidayName = "Independence Day observed" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 9, 7), HolidayName = "Labor Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 11, 26), HolidayName = "Thanksgiving Day" },
                new MarketHolidayEntity { HolidayDate = new DateOnly(2026, 12, 25), HolidayName = "Christmas Day" });
        });
    }

    private static void ConfigureReferenceDataModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradingInstrumentEntity>(entity =>
        {
            entity.ToTable("trading_instruments");
            entity.HasKey(x => x.Symbol);
            entity.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).ValueGeneratedNever();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(512).IsRequired();
            entity.Property(x => x.TradingStatus).HasColumnName("trading_status").HasDefaultValue(true).IsRequired();
            entity.Property(x => x.PrimaryInstrumentType).HasColumnName("primary_instrument_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.SecondaryInstrumentType).HasColumnName("secondary_instrument_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.ClosingPrice).HasColumnName("closing_price").IsRequired();
            entity.Property(x => x.ClosingPriceDate).HasColumnName("closing_price_date").IsRequired();
            entity.Property(x => x.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.TradingStatus);
            entity.HasIndex(x => x.SecondaryInstrumentType);
        });
    }

    private static void ConfigureTransactionProcessingModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostingBatchEntity>(entity =>
        {
            entity.ToTable("posting_batches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.RequestType).HasColumnName("request_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<LedgerTransactionEntity>(entity =>
        {
            entity.ToTable("ledger_transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TransactionId).HasColumnName("transaction_id").IsRequired();
            entity.Property(x => x.PostingBatchId).HasColumnName("posting_batch_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.TransactionKind).HasColumnName("transaction_kind").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Direction).HasColumnName("direction").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.TransactionDescription).HasColumnName("transaction_description").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(x => x.VoidsTransactionId).HasColumnName("voids_transaction_id");
            entity.Property(x => x.ExternalReferenceId).HasColumnName("external_reference_id").HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(x => x.TransactionId).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.PostingBatchId);
            entity.HasIndex(x => x.VoidsTransactionId);
            entity.HasOne(x => x.PostingBatch)
                .WithMany(x => x.LedgerTransactions)
                .HasForeignKey(x => x.PostingBatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TradeExecutionEntity>(entity =>
        {
            entity.ToTable("trade_executions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TransactionId).HasColumnName("transaction_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.ExternalExecutionId).HasColumnName("external_execution_id").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
            entity.Property(x => x.TradeSide).HasColumnName("trade_side").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.ExecutionPrice).HasColumnName("execution_price").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.GrossAmount).HasColumnName("gross_amount").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.Fees).HasColumnName("fees").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.NetAmount).HasColumnName("net_amount").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.ExecutedAtUtc).HasColumnName("executed_at_utc");
            entity.Property(x => x.TradeDate).HasColumnName("trade_date").IsRequired();
            entity.Property(x => x.SettlementDate).HasColumnName("settlement_date").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(x => x.TransactionId).IsUnique();
            entity.HasIndex(x => x.ExternalExecutionId).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Symbol);
            entity.HasIndex(x => x.TradeDate);
            entity.HasIndex(x => x.SettlementDate);
        });

        modelBuilder.Entity<CashMovementEntity>(entity =>
        {
            entity.ToTable("cash_movements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TransactionId).HasColumnName("transaction_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.MovementType).HasColumnName("movement_type").HasMaxLength(30).IsRequired();
            entity.Property(x => x.ExternalReferenceId).HasColumnName("external_reference_id").HasMaxLength(100);
            entity.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(50);
            entity.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(x => x.TransactionId).IsUnique();
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<AccountBalanceEntity>(entity =>
        {
            entity.ToTable("account_balances");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(x => x.CashBalance).HasColumnName("cash_balance").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<PositionEntity>(entity =>
        {
            entity.ToTable("positions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
            entity.Property(x => x.PositionSide).HasColumnName("position_side").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.CostBasisTotal).HasColumnName("cost_basis_total").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(x => new { x.UserId, x.Symbol }).IsUnique();
        });

        modelBuilder.Entity<PositionLotEntity>(entity =>
        {
            entity.ToTable("position_lots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
            entity.Property(x => x.OpeningTransactionId).HasColumnName("opening_transaction_id").IsRequired();
            entity.Property(x => x.ClosingTransactionId).HasColumnName("closing_transaction_id");
            entity.Property(x => x.LotEntryType).HasColumnName("lot_entry_type").HasMaxLength(20).IsRequired();
            entity.Property(x => x.QuantityOpened).HasColumnName("quantity_opened").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.QuantityRemaining).HasColumnName("quantity_remaining").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.OpenedAtUtc).HasColumnName("opened_at_utc");
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(x => new { x.UserId, x.Symbol });
            entity.HasIndex(x => x.OpeningTransactionId);
            entity.HasIndex(x => x.ClosingTransactionId);
        });

        modelBuilder.Entity<PositionLotClosureEntity>(entity =>
        {
            entity.ToTable("position_lot_closures");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.PositionLotId).HasColumnName("position_lot_id").IsRequired();
            entity.Property(x => x.OpeningTransactionId).HasColumnName("opening_transaction_id").IsRequired();
            entity.Property(x => x.ClosingTransactionId).HasColumnName("closing_transaction_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
            entity.Property(x => x.QuantityClosed).HasColumnName("quantity_closed").HasPrecision(18, 6).IsRequired();
            entity.Property(x => x.CostBasisAmount).HasColumnName("cost_basis_amount").HasPrecision(18, 4).IsRequired();
            entity.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasIndex(x => x.PositionLotId);
            entity.HasIndex(x => x.ClosingTransactionId);
            entity.HasIndex(x => new { x.UserId, x.Symbol });
            entity.HasOne(x => x.PositionLot)
                .WithMany(x => x.Closures)
                .HasForeignKey(x => x.PositionLotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
