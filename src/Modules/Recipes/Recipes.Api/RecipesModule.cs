using Recipes.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Recipes.Api;

// The module's public surface: the host only calls these two methods.
public static class RecipesModule
{
    // Needs Catalog's ICatalogIngredients (Catalog.Contracts), registered by the Catalog module.
    public static IHostApplicationBuilder AddRecipesModule(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddRecipesInfrastructure(connectionName);
        builder.Services.AddScoped<RecipeWriter>();

        return builder;
    }

    // Exposes this module's HTTP API under /api/recipes.
    public static RouteGroupBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes").WithTags("Recipes");

        // WithName sets the OpenAPI operationId, which becomes the generated hook's name (useListRecipes).
        group.MapGet("/", ListRecipes.Handle)
            .WithName("ListRecipes")
            .WithSummary("List recipes with energy per serving, optionally filtered by title");

        group.MapGet("/{id:guid}", GetRecipe.Handle)
            .WithName(GetRecipe.Name)
            .WithSummary("Get a recipe with its ingredients and nutrition");

        group.MapPost("/", CreateRecipe.Handle)
            .WithName("CreateRecipe")
            .WithSummary("Create a recipe");

        group.MapPut("/{id:guid}", UpdateRecipe.Handle)
            .WithName("UpdateRecipe")
            .WithSummary("Replace a recipe, ingredient lines included");

        group.MapDelete("/{id:guid}", DeleteRecipe.Handle)
            .WithName("DeleteRecipe")
            .WithSummary("Delete a recipe");

        return group;
    }
}
