using Recipes.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Recipes.Api;

// The module's public surface: the host only calls these two methods.
public static class RecipesModule
{
    public static IHostApplicationBuilder AddRecipesModule(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddRecipesInfrastructure(connectionName);

        return builder;
    }

    // Exposes this module's HTTP API under /api/recipes.
    public static RouteGroupBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes").WithTags("Recipes");

        return group;
    }
}
