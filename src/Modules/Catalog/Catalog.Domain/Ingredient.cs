using System.Runtime.CompilerServices;

namespace Catalog.Domain;

/// <summary>A food from the catalog with its nutrition per 100 g, imported from an external dataset.</summary>
public sealed class Ingredient
{
    public const int NameMaxLength = 200;
    public const int SourceMaxLength = 50;
    public const int SourceIdMaxLength = 100;

    /// <summary>Both names normalized, plus the space between them; normalizing never makes text longer.</summary>
    public const int SearchTextMaxLength = (2 * NameMaxLength) + 1;

    /// <summary>100 g can't hold more than 100 g of macros; the tolerance absorbs rounding in source data (e.g. oils at 100.0 g fat plus 0.1 g protein).</summary>
    internal const decimal MaxMacrosPer100g = 100.5m;

    private Ingredient(Guid id, string name, string hungarianName, NutrientProfile nutrientsPer100g, string source, string sourceId)
    {
        Id = id;
        Name = name;
        HungarianName = hungarianName;
        NutrientsPer100g = nutrientsPer100g;
        Source = source;
        SourceId = sourceId;
        SearchText = BuildSearchText(name, hungarianName);
    }

    // For EF Core: it can't pass a complex property through a constructor, so it materializes through
    // this one and sets the properties afterwards. Domain code always goes through Create.
#pragma warning disable CS8618 // Non-nullable properties are filled in by EF Core right after construction.
    private Ingredient()
    {
    }
#pragma warning restore CS8618

    public Guid Id { get; private set; }

    /// <summary>English name, as in the source dataset.</summary>
    public string Name { get; private set; }

    public string HungarianName { get; private set; }

    public NutrientProfile NutrientsPer100g { get; private set; }

    /// <summary>Dataset the ingredient was imported from; together with <see cref="SourceId"/> it identifies the row there.</summary>
    public string Source { get; private set; }

    public string SourceId { get; private set; }

    /// <summary>Hungarian and English name in <see cref="Domain.SearchText"/> form, kept in sync by every name change. Searches match against this.</summary>
    public string SearchText { get; private set; }

    public static Ingredient Create(string name, string hungarianName, NutrientProfile nutrientsPer100g, string source, string sourceId)
    {
        ArgumentNullException.ThrowIfNull(nutrientsPer100g);

        return new Ingredient(
            // Version 7 GUIDs are time-ordered, so new rows land at the end of the primary key index.
            Guid.CreateVersion7(),
            RequiredText(name, NameMaxLength),
            RequiredText(hungarianName, NameMaxLength),
            ValidPer100g(nutrientsPer100g),
            RequiredText(source, SourceMaxLength),
            RequiredText(sourceId, SourceIdMaxLength));
    }

    /// <summary>Takes over a newer version of the same source row; the identity (id, source, source id) stays.</summary>
    public void UpdateFromSource(string name, string hungarianName, NutrientProfile nutrientsPer100g)
    {
        ArgumentNullException.ThrowIfNull(nutrientsPer100g);

        // Validate everything before changing anything, so a bad row leaves the ingredient untouched.
        var validName = RequiredText(name, NameMaxLength);
        var validHungarianName = RequiredText(hungarianName, NameMaxLength);
        var validNutrients = ValidPer100g(nutrientsPer100g);

        Name = validName;
        HungarianName = validHungarianName;
        NutrientsPer100g = validNutrients;
        SearchText = BuildSearchText(validName, validHungarianName);
    }

    /// <summary>Nutrition of the given amount of this ingredient.</summary>
    public NutrientProfile NutrientsFor(decimal grams)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grams);

        return NutrientsPer100g.Scale(grams / 100);
    }

    // Hungarian first: it's the name people type, and "starts with" ranking looks at the beginning.
    private static string BuildSearchText(string name, string hungarianName) =>
        Domain.SearchText.Normalize($"{hungarianName} {name}");

    private static string RequiredText(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Must be at most {maxLength} characters.", paramName);
        }

        return trimmed;
    }

    private static NutrientProfile ValidPer100g(NutrientProfile nutrientsPer100g)
    {
        if (nutrientsPer100g.Macros > MaxMacrosPer100g)
        {
            throw new ArgumentException(
                $"Protein, carbohydrate and fat add up to {nutrientsPer100g.Macros} g, more than fits in 100 g.",
                nameof(nutrientsPer100g));
        }

        return nutrientsPer100g;
    }
}
