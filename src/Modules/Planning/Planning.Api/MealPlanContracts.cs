using Planning.Domain;

namespace Planning.Api;

/// <summary>Puts a recipe on a day and meal of the week; title and kcal are copied from Recipes.</summary>
public sealed record AddMealPlanEntryRequest(Guid RecipeId, DayOfWeek Day, Meal Meal, int Servings);

/// <summary>Moves an entry to another day or meal and sets its servings.</summary>
public sealed record ChangeMealPlanEntryRequest(DayOfWeek Day, Meal Meal, int Servings);

/// <summary>A week's plan: always seven days, Monday to Sunday, also for a week nothing is planned for yet.</summary>
public sealed record MealPlanResponse(DateOnly WeekStart, IReadOnlyList<MealPlanDayResponse> Days, decimal EnergyKcal);

/// <summary>One day of the week with its entries, ordered by meal and then by when they were added.</summary>
public sealed record MealPlanDayResponse(
    DayOfWeek Day, DateOnly Date, decimal EnergyKcal, IReadOnlyList<MealPlanEntryResponse> Entries);

/// <summary>
/// A planned recipe. Title and energy per serving are Planning's copy (ADR 0005): a recipe deleted since stays listed
/// under its last known title. Energies in kcal, rounded to 2 decimals.
/// </summary>
public sealed record MealPlanEntryResponse(
    Guid Id,
    Meal Meal,
    Guid RecipeId,
    string RecipeTitle,
    int Servings,
    decimal EnergyKcalPerServing,
    decimal EnergyKcal);
