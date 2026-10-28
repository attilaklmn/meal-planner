using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure.Persistence;

// Used only by `dotnet ef` to build the model when adding migrations; it never opens a connection.
// This keeps migrations independent of the Api host and of Aspire supplying a connection string.
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost", CatalogDbContext.ConfigureNpgsql)
            .Options;

        return new CatalogDbContext(options);
    }
}
