using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Planning.Infrastructure.Persistence;

namespace Planning.Api;

// DELETE /api/plans/2026-11-02/entries/{entryId}
internal static class RemoveMealPlanEntry
{
    public static async Task<Results<Ok<MealPlanResponse>, NotFound>> Handle(
        [Description("The Monday the week starts on, as yyyy-MM-dd.")] DateOnly weekStart,
        Guid entryId,
        PlanningDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Through the aggregate rather than ExecuteDelete: the response is the week with its new totals anyway.
        var plan = await dbContext.MealPlans
            .Include(p => p.Entries)
            .SingleOrDefaultAsync(p => p.WeekStart == weekStart, cancellationToken);
        if (plan is null || plan.Entries.All(e => e.Id != entryId))
        {
            return TypedResults.NotFound();
        }

        // The plan stays when its last entry goes: an empty plan reads the same as no plan.
        plan.RemoveEntry(entryId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(plan.ToResponse());
    }
}
