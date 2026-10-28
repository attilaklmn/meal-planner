using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Recipes.Infrastructure.Persistence;

// Used only by `dotnet ef` to build the model when adding migrations; it never opens a connection.
// This keeps migrations independent of the Api host and of Aspire supplying a connection string.
public sealed class RecipesDbContextFactory : IDesignTimeDbContextFactory<RecipesDbContext>
{
    public RecipesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RecipesDbContext>()
            .UseNpgsql("Host=localhost", RecipesDbContext.ConfigureNpgsql)
            .Options;

        return new RecipesDbContext(options);
    }
}
