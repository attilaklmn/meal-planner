namespace Planning.Domain;

/// <summary>
/// The meals planned for one week, Monday to Sunday: the aggregate root, so entries change only through it.
/// A week is identified by its Monday, the ISO 8601 first day of the week, as used in Hungary.
/// </summary>
public sealed class MealPlan
{
    public const int MaxServings = 20;
    public const int MaxEntries = 100;

    /// <summary>Monday to Sunday; <see cref="DayOfWeek"/> itself starts on Sunday.</summary>
    public static readonly IReadOnlyList<DayOfWeek> Days =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    ];

    private readonly List<MealPlanEntry> _entries = [];

    private MealPlan(Guid id, DateOnly weekStart)
    {
        Id = id;
        WeekStart = weekStart;
    }

    public Guid Id { get; private set; }

    /// <summary>The Monday the week starts on.</summary>
    public DateOnly WeekStart { get; private set; }

    /// <summary>In the order they were added; a plan loaded from the database is in no particular order, so readers sort by id.</summary>
    public IReadOnlyList<MealPlanEntry> Entries => _entries;

    public decimal WeeklyEnergyKcal => _entries.Sum(e => e.EnergyKcal);

    public static MealPlan Create(DateOnly weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException($"A week starts on a Monday; {weekStart:yyyy-MM-dd} is a {weekStart.DayOfWeek}.", nameof(weekStart));
        }

        return new MealPlan(Guid.CreateVersion7(), weekStart);
    }

    /// <summary>The Monday of the week <paramref name="date"/> falls in.</summary>
    public static DateOnly WeekStartOf(DateOnly date) =>
        date.AddDays(-((7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7));

    /// <summary>The date of a day of this week (Sunday is the last day, not the first).</summary>
    public DateOnly DateOf(DayOfWeek day) => WeekStart.AddDays(((int)ValidDay(day) + 6) % 7);

    public MealPlanEntry AddEntry(DayOfWeek day, Meal meal, RecipeSnapshot recipe, int servings)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var entry = new MealPlanEntry(ValidDay(day), ValidMeal(meal), recipe, ValidServings(servings));
        if (_entries.Count >= MaxEntries)
        {
            throw new ArgumentException($"A week can have at most {MaxEntries} planned recipes.", nameof(recipe));
        }

        _entries.Add(entry);
        return entry;
    }

    /// <summary>Moves an entry to another day or meal and sets its servings; validates everything before changing anything.</summary>
    public MealPlanEntry ChangeEntry(Guid entryId, DayOfWeek day, Meal meal, int servings)
    {
        var entry = FindEntry(entryId);
        var validDay = ValidDay(day);
        var validMeal = ValidMeal(meal);
        var validServings = ValidServings(servings);

        entry.Day = validDay;
        entry.Meal = validMeal;
        entry.Servings = validServings;
        return entry;
    }

    public void RemoveEntry(Guid entryId) => _entries.Remove(FindEntry(entryId));

    /// <summary>Takes over a recipe's current title and energy on every entry that plans it (ADR 0005).</summary>
    public void RefreshRecipe(RecipeSnapshot recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        foreach (var entry in _entries.Where(e => e.Recipe.RecipeId == recipe.RecipeId))
        {
            entry.Recipe = recipe;
        }
    }

    public decimal EnergyKcalOn(DayOfWeek day)
    {
        var validDay = ValidDay(day);
        return _entries.Where(e => e.Day == validDay).Sum(e => e.EnergyKcal);
    }

    private MealPlanEntry FindEntry(Guid entryId) =>
        _entries.SingleOrDefault(e => e.Id == entryId)
            ?? throw new ArgumentException($"The plan has no entry {entryId}.", nameof(entryId));

    private static DayOfWeek ValidDay(DayOfWeek day) =>
        Enum.IsDefined(day) ? day : throw new ArgumentOutOfRangeException(nameof(day), day, "Unknown day.");

    private static Meal ValidMeal(Meal meal) =>
        Enum.IsDefined(meal) ? meal : throw new ArgumentOutOfRangeException(nameof(meal), meal, "Unknown meal.");

    private static int ValidServings(int servings) =>
        servings is >= 1 and <= MaxServings
            ? servings
            : throw new ArgumentOutOfRangeException(nameof(servings), servings, $"Servings must be between 1 and {MaxServings}.");
}
