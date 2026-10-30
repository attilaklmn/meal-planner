using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Catalog.Api;

// The module's public surface: the host only calls these two methods.
public static class CatalogModule
{
    public static IHostApplicationBuilder AddCatalogModule(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddCatalogInfrastructure(connectionName);

        return builder;
    }

    // Exposes this module's HTTP API under /api/catalog.
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        // WithName sets the OpenAPI operationId, which becomes the generated hook's name (usePingCatalog).
        group.MapGet("/ping", Ping).WithName("PingCatalog");

        group.MapGet("/ingredients", SearchIngredients.Handle)
            .WithName("SearchIngredients")
            .WithSummary("Search ingredients by English or Hungarian name");

        return group;
    }

    // Placeholder that proves the OpenAPI -> generated client pipeline end to end; real endpoints replace it in M1.
    // Returning a TypedResults type (not IResult) is what lets OpenAPI describe the response body.
    private static Ok<CatalogPingResponse> Ping() =>
        TypedResults.Ok(new CatalogPingResponse("catalog", DateTimeOffset.UtcNow));
}
