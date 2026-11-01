namespace Recipes.Domain;

/// <summary>
/// A recipe and its ingredient lines: the aggregate root, so ingredient lines change only through it.
/// Ingredients are referenced by id; their data comes from Recipes' <see cref="CatalogIngredient"/> copies.
/// </summary>
public sealed class Recipe
{
    public const int TitleMaxLength = 200;
    public const int MaxServings = 100;
    public const int MaxSteps = 100;
    public const int TagMaxLength = 50;
    public const int MaxTags = 20;

    private readonly List<RecipeIngredient> _ingredients = [];

    // A List field behind the read-only property: EF Core maps a complex collection only to an IList<T>.
    private List<RecipeStep> _steps = [];

    private Recipe(Guid id, string title, int servings, List<RecipeStep> steps, IReadOnlyList<string> tags)
    {
        Id = id;
        Title = title;
        SearchText = Domain.SearchText.Normalize(title);
        Servings = servings;
        _steps = steps;
        Tags = tags;
    }

    // For EF Core: it can't pass collections through a constructor. Domain code always goes through Create.
#pragma warning disable CS8618 // Non-nullable properties are filled in by EF Core right after construction.
    private Recipe()
    {
    }
#pragma warning restore CS8618

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    /// <summary>The title in <see cref="Domain.SearchText"/> form, kept in sync by every title change. Searches match against this.</summary>
    public string SearchText { get; private set; }

    /// <summary>How many servings the ingredient quantities are for.</summary>
    public int Servings { get; private set; }

    public IReadOnlyList<RecipeStep> Steps => _steps;

    /// <summary>Lowercase, without duplicates, in the order given ("leves", "vegetáriánus").</summary>
    public IReadOnlyList<string> Tags { get; private set; }

    /// <summary>In the order of <see cref="RecipeIngredient.Position"/>.</summary>
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;

    public static Recipe Create(string title, int servings, IEnumerable<string> steps, IEnumerable<string> tags) =>
        // Version 7 GUIDs are time-ordered, so new rows land at the end of the primary key index.
        new(Guid.CreateVersion7(), ValidTitle(title), ValidServings(servings), ValidSteps(steps), ValidTags(tags));

    public void Update(string title, int servings, IEnumerable<string> steps, IEnumerable<string> tags)
    {
        // Validate everything before changing anything, so a bad request leaves the recipe untouched.
        var validTitle = ValidTitle(title);
        var validServings = ValidServings(servings);
        var validSteps = ValidSteps(steps);
        var validTags = ValidTags(tags);

        Title = validTitle;
        SearchText = Domain.SearchText.Normalize(validTitle);
        Servings = validServings;
        _steps = validSteps;
        Tags = validTags;
    }

    /// <summary>
    /// Adds an ingredient line at the end. Takes the ingredient itself, not just its id, to check that the quantity
    /// converts to grams for it ("2 pieces of flour" doesn't): every line of a saved recipe has a nutrition value.
    /// </summary>
    public RecipeIngredient AddIngredient(CatalogIngredient ingredient, Quantity quantity, string? note)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        ArgumentNullException.ThrowIfNull(quantity);

        if (!quantity.CanConvertToGrams(ingredient.UnitConversion))
        {
            throw new ArgumentException(
                $"{ingredient.Name} can't be measured in {quantity.Unit}: it has no " +
                (quantity.Dimension == UnitDimension.Volume ? "density." : "piece weight."),
                nameof(quantity));
        }

        var line = new RecipeIngredient(ingredient.Id, quantity, ValidNote(note), _ingredients.Count);
        _ingredients.Add(line);
        return line;
    }

    public void RemoveIngredient(Guid recipeIngredientId)
    {
        var line = _ingredients.SingleOrDefault(i => i.Id == recipeIngredientId)
            ?? throw new ArgumentException($"The recipe has no ingredient line {recipeIngredientId}.", nameof(recipeIngredientId));

        _ingredients.Remove(line);

        // Close the gap, so positions stay 0..n-1.
        var position = 0;
        foreach (var remaining in _ingredients.OrderBy(i => i.Position))
        {
            remaining.Position = position++;
        }

        _ingredients.Sort((a, b) => a.Position.CompareTo(b.Position));
    }

    /// <summary>Removes every ingredient line, for replacing the whole list.</summary>
    public void ClearIngredients() => _ingredients.Clear();

    /// <summary>
    /// Total and per-serving nutrition, from each line's quantity in grams and its ingredient's nutrients per 100 g.
    /// <paramref name="ingredients"/> must hold the <see cref="CatalogIngredient"/> of every line, keyed by id.
    /// </summary>
    public RecipeNutrition CalculateNutrition(IReadOnlyDictionary<Guid, CatalogIngredient> ingredients) =>
        RecipeNutrition.Calculate(_ingredients.Select(i => (i.IngredientId, i.Quantity)), Servings, ingredients);

    private static string ValidTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var trimmed = title.Trim();
        return trimmed.Length <= TitleMaxLength
            ? trimmed
            : throw new ArgumentException($"The title must be at most {TitleMaxLength} characters.", nameof(title));
    }

    private static int ValidServings(int servings)
    {
        return servings is >= 1 and <= MaxServings
            ? servings
            : throw new ArgumentOutOfRangeException(nameof(servings), servings, $"Servings must be between 1 and {MaxServings}.");
    }

    private static List<RecipeStep> ValidSteps(IEnumerable<string> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var validSteps = steps.Select(text => new RecipeStep(text)).ToList();
        return validSteps.Count <= MaxSteps
            ? validSteps
            : throw new ArgumentException($"A recipe can have at most {MaxSteps} steps.", nameof(steps));
    }

    // Lowercase, so "Leves" and "leves" are one tag and filtering by tag needs no case-insensitive comparison.
    private static List<string> ValidTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var validTags = new List<string>();
        foreach (var tag in tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tag, nameof(tags));

            var normalized = tag.Trim().ToLowerInvariant();
            if (normalized.Length > TagMaxLength)
            {
                throw new ArgumentException($"A tag must be at most {TagMaxLength} characters.", nameof(tags));
            }

            if (!validTags.Contains(normalized))
            {
                validTags.Add(normalized);
            }
        }

        return validTags.Count <= MaxTags
            ? validTags
            : throw new ArgumentException($"A recipe can have at most {MaxTags} tags.", nameof(tags));
    }

    private static string? ValidNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length <= RecipeIngredient.NoteMaxLength
            ? trimmed
            : throw new ArgumentException($"A note must be at most {RecipeIngredient.NoteMaxLength} characters.", nameof(note));
    }
}
