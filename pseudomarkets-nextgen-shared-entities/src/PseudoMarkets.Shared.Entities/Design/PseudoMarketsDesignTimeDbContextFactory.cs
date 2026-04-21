using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PseudoMarkets.Shared.Entities.Database;

namespace PseudoMarkets.Shared.Entities.Design;

public class PseudoMarketsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PseudoMarketsDbContext>
{
    public PseudoMarketsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PseudoMarketsDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=pseudomarkets_db;Username=postgres;Password=postgres");

        return new PseudoMarketsDbContext(optionsBuilder.Options);
    }
}
