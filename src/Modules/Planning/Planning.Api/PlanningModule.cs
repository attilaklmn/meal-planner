using Planning.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Planning.Api;

// The module's public surface: the host only calls these two methods.
public static class PlanningModule
{
    // Needs Recipes' IRecipeReader (Recipes.Contracts), registered by the Recipes module.
    public static IHostApplicationBuilder AddPlanningModule(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddPlanningInfrastructure(connectionName);

        return builder;
    }

    // Exposes this module's HTTP API: weekly plans under /api/plans (shopping lists get a group of their own).
    public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        // A week is addressed by its Monday: /api/plans/2026-11-02.
        var plans = app.MapGroup("/api/plans/{weekStart}").WithTags("Meal plans");

        // WithName sets the OpenAPI operationId, which becomes the generated hook's name (useGetMealPlan).
        plans.MapGet("/", GetMealPlan.Handle)
            .WithName("GetMealPlan")
            .WithSummary("Get a week's plan with daily energy totals; an empty plan when nothing is planned");

        plans.MapPost("/entries", AddMealPlanEntry.Handle)
            .WithName("AddMealPlanEntry")
            .WithSummary("Put a recipe on a day and meal of the week");

        plans.MapPut("/entries/{entryId:guid}", ChangeMealPlanEntry.Handle)
            .WithName("ChangeMealPlanEntry")
            .WithSummary("Move an entry to another day or meal, or change its servings");

        plans.MapDelete("/entries/{entryId:guid}", RemoveMealPlanEntry.Handle)
            .WithName("RemoveMealPlanEntry")
            .WithSummary("Take an entry off the plan");

        return app;
    }
}
