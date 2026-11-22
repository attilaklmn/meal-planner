using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Planning.Domain;
using Planning.Infrastructure.Persistence;

namespace Planning.Api;

// GET /api/plans/2026-11-02
internal static class GetMealPlan
{
    public static async Task<Results<Ok<MealPlanResponse>, ValidationProblem>> Handle(
        [Description("The Monday the week starts on, as yyyy-MM-dd.")] DateOnly weekStart,
        PlanningDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        // Validates the Monday; for a week without a saved plan this empty plan is the answer (no 404: every week exists).
        var emptyPlan = errors.Try(() => MealPlan.Create(weekStart), _ => "weekStart");
        if (emptyPlan is null)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        // The aggregate, read-only: daily totals come from the domain. A week has a few dozen entries at most.
        var plan = await dbContext.MealPlans
            .AsNoTracking()
            .Include(p => p.Entries)
            .SingleOrDefaultAsync(p => p.WeekStart == weekStart, cancellationToken);

        return TypedResults.Ok((plan ?? emptyPlan).ToResponse());
    }
}
