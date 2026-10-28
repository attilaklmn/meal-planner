using Planning.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Planning.Api;

// The module's public surface: the host only calls these two methods.
public static class PlanningModule
{
    public static IHostApplicationBuilder AddPlanningModule(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddPlanningInfrastructure(connectionName);

        return builder;
    }

    // Exposes this module's HTTP API under /api/planning.
    public static RouteGroupBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planning").WithTags("Planning");

        return group;
    }
}
