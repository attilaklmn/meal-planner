using System.Runtime.CompilerServices;

namespace Recipes.Domain;

/// <summary>
/// Recipes' local copy of a Catalog ingredient: the data a recipe needs to show and calculate it (ADR 0004).
/// The id is Catalog's id. Catalog owns the data; this copy is refreshed from it (on save in v1, by events in v2).
/// </summary>
public sealed class CatalogIngredient
{
    public const int NameMaxLength = 200;

    private CatalogIngredient(
        Guid id, string name, string hungarianName, NutrientProfile nutrientsPer100g, UnitConversion unitConversion)
    {
        Id = id;
        Name = name;
        HungarianName = hungarianName;
        NutrientsPer100g = nutrientsPer100g;
        UnitConversion = unitConversion;
    }

    // For EF Core: it can't pass complex properties through a constructor, so it materializes through
    // this one and sets the properties afterwards. Domain code always goes through Create.
#pragma warning disable CS8618 // Non-nullable properties are filled in by EF Core right after construction.
    private CatalogIngredient()
    {
    }
#pragma warning restore CS8618

    public Guid Id { get; private set; }

    /// <summary>English name, as in Catalog.</summary>
    public string Name { get; private set; }

    public string HungarianName { get; private set; }

    public NutrientProfile NutrientsPer100g { get; private set; }

    /// <summary>Density and piece weight: which units a recipe can measure this ingredient in.</summary>
    public UnitConversion UnitConversion { get; private set; }

    public static CatalogIngredient Create(
        Guid id, string name, string hungarianName, NutrientProfile nutrientsPer100g, UnitConversion unitConversion)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A catalog ingredient needs Catalog's id.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(nutrientsPer100g);
        ArgumentNullException.ThrowIfNull(unitConversion);

        return new CatalogIngredient(
            id, RequiredText(name), RequiredText(hungarianName), nutrientsPer100g, unitConversion);
    }

    /// <summary>Takes over Catalog's current data; every recipe using this ingredient calculates with it from then on.</summary>
    public void RefreshFrom(string name, string hungarianName, NutrientProfile nutrientsPer100g, UnitConversion unitConversion)
    {
        ArgumentNullException.ThrowIfNull(nutrientsPer100g);
        ArgumentNullException.ThrowIfNull(unitConversion);

        // Validate everything before changing anything.
        var validName = RequiredText(name);
        var validHungarianName = RequiredText(hungarianName);

        Name = validName;
        HungarianName = validHungarianName;
        NutrientsPer100g = nutrientsPer100g;
        UnitConversion = unitConversion;
    }

    /// <summary>Nutrition of the given quantity; throws when the quantity's unit doesn't convert to grams for this ingredient.</summary>
    public NutrientProfile NutrientsFor(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);

        return NutrientsPer100g.Scale(quantity.ToGrams(UnitConversion) / 100);
    }

    private static string RequiredText(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Must be at most {NameMaxLength} characters.", paramName);
        }

        return trimmed;
    }
}
