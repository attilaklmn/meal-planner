namespace Planning.Domain;

/// <summary>
/// What a plan entry keeps of the recipe it plans: Recipes' id plus a copy of the title and the energy per serving,
/// so the weekly plan can be shown from Planning's own data (ADR 0005). Refreshed when the entry changes.
/// </summary>
public sealed record RecipeSnapshot
{
    public const int TitleMaxLength = 200;

    public RecipeSnapshot(Guid recipeId, string title, decimal energyKcalPerServing)
    {
        if (recipeId == Guid.Empty)
        {
            throw new ArgumentException("A planned recipe needs Recipes' id.", nameof(recipeId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new ArgumentException($"The title must be at most {TitleMaxLength} characters.", nameof(title));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(energyKcalPerServing);

        RecipeId = recipeId;
        Title = trimmed;
        EnergyKcalPerServing = energyKcalPerServing;
    }

    // Get-only (no init): a `with` expression would skip the constructor's validation.
    public Guid RecipeId { get; }

    public string Title { get; }

    public decimal EnergyKcalPerServing { get; }
}
