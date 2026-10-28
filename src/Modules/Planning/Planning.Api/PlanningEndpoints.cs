using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Planning.Api;

public static class PlanningEndpoints
{
    // Entry point the host calls to expose this module's HTTP API under /api/planning.
    public static RouteGroupBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planning").WithTags("Planning");

        return group;
    }
}
