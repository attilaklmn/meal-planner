using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

public static class CatalogEndpoints
{
    // Entry point the host calls to expose this module's HTTP API under /api/catalog.
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        return group;
    }
}
