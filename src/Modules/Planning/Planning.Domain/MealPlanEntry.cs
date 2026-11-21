namespace Planning.Domain;

/// <summary>
/// One recipe on the plan: which day and meal, and how many servings of it are eaten. A meal can have more than one
/// entry (soup and a main course for lunch). Only <see cref="MealPlan"/> creates and changes it.
/// </summary>
public sealed class MealPlanEntry
{
    internal MealPlanEntry(DayOfWeek day, Meal meal, RecipeSnapshot recipe, int servings)
    {
        Id = Guid.CreateVersion7();
        Day = day;
        Meal = meal;
        Recipe = recipe;
        Servings = servings;
    }

    // For EF Core: it can't pass a complex property through a constructor.
#pragma warning disable CS8618 // Non-nullable properties are filled in by EF Core right after construction.
    private MealPlanEntry()
    {
    }
#pragma warning restore CS8618

    /// <summary>Version 7: sorting by id gives the order entries were added in, to the millisecond.</summary>
    public Guid Id { get; private set; }

    public DayOfWeek Day { get; internal set; }

    public Meal Meal { get; internal set; }

    public RecipeSnapshot Recipe { get; internal set; }

    /// <summary>Servings eaten at this meal, independent of how many the recipe is written for.</summary>
    public int Servings { get; internal set; }

    public decimal EnergyKcal => Recipe.EnergyKcalPerServing * Servings;
}
