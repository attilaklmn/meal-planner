namespace Recipes.Domain;

/// <summary>
/// One line of a recipe's ingredient list ("2 tbsp olive oil, for frying"): the join between a recipe and a
/// <see cref="CatalogIngredient"/>, with a quantity and a note as payload. It has its own id, so the same ingredient
/// can appear twice (eggs for the dough and for the glaze). Only <see cref="Recipe"/> creates and changes it.
/// </summary>
public sealed class RecipeIngredient
{
    public const int NoteMaxLength = 200;

    internal RecipeIngredient(Guid ingredientId, Quantity quantity, string? note, int position)
    {
        Id = Guid.CreateVersion7();
        IngredientId = ingredientId;
        Quantity = quantity;
        Note = note;
        Position = position;
    }

    // For EF Core: it can't pass a complex property through a constructor.
#pragma warning disable CS8618 // Non-nullable properties are filled in by EF Core right after construction.
    private RecipeIngredient()
    {
    }
#pragma warning restore CS8618

    public Guid Id { get; private set; }

    /// <summary>Catalog's id of the ingredient, also the key of its <see cref="CatalogIngredient"/> copy.</summary>
    public Guid IngredientId { get; private set; }

    public Quantity Quantity { get; private set; }

    /// <summary>Free text shown after the ingredient ("finely chopped", "for the glaze"); null when there is none.</summary>
    public string? Note { get; private set; }

    /// <summary>Order in the recipe's list, from 0. A database returns rows in no particular order, so the order is stored.</summary>
    public int Position { get; internal set; }
}
