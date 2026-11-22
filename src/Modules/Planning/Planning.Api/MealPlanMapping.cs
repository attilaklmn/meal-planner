using Planning.Domain;

namespace Planning.Api;

internal static class MealPlanMapping
{
    private const int Decimals = 2;

    public static MealPlanResponse ToResponse(this MealPlan plan) => new(
        plan.WeekStart,
        MealPlan.Days
            .Select(day => new MealPlanDayResponse(
                day,
                plan.DateOf(day),
                Math.Round(plan.EnergyKcalOn(day), Decimals),
                plan.Entries
                    .Where(e => e.Day == day)
                    // Guid v7 ids sort by creation time, so a meal's entries keep the order they were added in.
                    .OrderBy(e => e.Meal)
                    .ThenBy(e => e.Id)
                    .Select(e => new MealPlanEntryResponse(
                        e.Id,
                        e.Meal,
                        e.Recipe.RecipeId,
                        e.Recipe.Title,
                        e.Servings,
                        Math.Round(e.Recipe.EnergyKcalPerServing, Decimals),
                        Math.Round(e.EnergyKcal, Decimals)))
                    .ToList()))
            .ToList(),
        Math.Round(plan.WeeklyEnergyKcal, Decimals));
}
