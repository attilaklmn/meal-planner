using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planning.Infrastructure.Persistence;

// Used only by `dotnet ef` to build the model when adding migrations; it never opens a connection.
// This keeps migrations independent of the Api host and of Aspire supplying a connection string.
public sealed class PlanningDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    public PlanningDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseNpgsql("Host=localhost", PlanningDbContext.ConfigureNpgsql)
            .Options;

        return new PlanningDbContext(options);
    }
}
