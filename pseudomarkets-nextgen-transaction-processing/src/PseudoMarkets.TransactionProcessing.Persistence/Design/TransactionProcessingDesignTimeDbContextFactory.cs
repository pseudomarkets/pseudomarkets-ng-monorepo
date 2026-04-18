using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PseudoMarkets.TransactionProcessing.Persistence.Database;

namespace PseudoMarkets.TransactionProcessing.Persistence.Design;

public class TransactionProcessingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TransactionProcessingDbContext>
{
    public TransactionProcessingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TransactionProcessingDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=pseudomarkets_transaction_processing;Username=postgres;Password=postgres");

        return new TransactionProcessingDbContext(optionsBuilder.Options);
    }
}
