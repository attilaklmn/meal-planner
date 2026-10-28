using Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        return group;
    }
}
