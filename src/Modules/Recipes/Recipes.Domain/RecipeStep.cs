namespace Recipes.Domain;

/// <summary>One step of a recipe's method. A type of its own rather than a string, so a step can grow (a timer, a photo)
/// without changing how steps are stored.</summary>
public sealed record RecipeStep
{
    public const int TextMaxLength = 2000;

    public RecipeStep(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var trimmed = text.Trim();
        if (trimmed.Length > TextMaxLength)
        {
            throw new ArgumentException($"A step must be at most {TextMaxLength} characters.", nameof(text));
        }

        Text = trimmed;
    }

    // Get-only (no init): a `with` expression would skip the constructor's validation.
    public string Text { get; }
}
