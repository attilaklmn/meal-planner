using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Planning.Domain;
using Planning.Infrastructure.Persistence;
using Recipes.Contracts;

namespace Planning.Api;

// POST /api/plans/2026-11-02/entries: the week's plan is created with its first entry.
internal static class AddMealPlanEntry
{
    public static async Task<Results<Ok<MealPlanResponse>, ValidationProblem>> Handle(
        [Description("The Monday the week starts on, as yyyy-MM-dd.")] DateOnly weekStart,
        AddMealPlanEntryRequest request,
        IRecipeReader recipes,
        PlanningDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        errors.Try(() => MealPlan.Create(weekStart), _ => "weekStart");

        // Title and kcal are copied now (ADR 0005), so showing the plan never needs Recipes.
        var recipe = (await recipes.GetByIdsAsync([request.RecipeId], cancellationToken)).SingleOrDefault();
        if (recipe is null)
        {
            errors.Add("recipeId", "No recipe with this id.");
        }

        if (!errors.IsEmpty || recipe is null)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var snapshot = new RecipeSnapshot(recipe.Id, recipe.Title, recipe.EnergyKcalPerServing);

        // Two requests adding the first entries of a week at once both try to create its plan; the unique week index
        // rejects the second one, which then starts over and adds its entry to the plan the first one created.
        for (var attempt = 1; ; attempt++)
        {
            var plan = await dbContext.MealPlans
                .Include(p => p.Entries)
                .SingleOrDefaultAsync(p => p.WeekStart == weekStart, cancellationToken);
            if (plan is null)
            {
                plan = MealPlan.Create(weekStart);
                dbContext.MealPlans.Add(plan);
            }

            var entry = errors.Try(
                () => plan.AddEntry(request.Day, request.Meal, snapshot, request.Servings),
                parameter => parameter == "recipe" ? "recipeId" : parameter ?? "entry");
            if (entry is null)
            {
                return TypedResults.ValidationProblem(errors.ToDictionary());
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                // The whole week, so the client can replace its copy (the day's total changed too).
                return TypedResults.Ok(plan.ToResponse());
            }
            catch (DbUpdateException exception) when (attempt == 1 && PlanningDbContext.IsDuplicateWeek(exception))
            {
                dbContext.ChangeTracker.Clear();
            }
        }
    }
}
