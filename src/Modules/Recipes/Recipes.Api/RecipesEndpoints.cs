using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Recipes.Api;

public static class RecipesEndpoints
{
    // Entry point the host calls to expose this module's HTTP API under /api/recipes.
    public static RouteGroupBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes").WithTags("Recipes");

        return group;
    }
}
