using System.Globalization;
using System.Text;

namespace Recipes.Domain;

/// <summary>
/// The form names are searched in: lowercase, no accents, words separated by single spaces
/// ("Rakott krumpli (vega)" → "rakott krumpli vega"). Stored data and search terms go through the same function,
/// so "gulyas", "GULYÁS" and "gulyás" all find "Gulyásleves". A copy of Catalog's: modules don't share domain code.
/// </summary>
public static class SearchText
{
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // FormD splits "á" into "a" + a combining accent, which is then dropped. Covers Hungarian ő and ű too.
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSpace && result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(char.ToLowerInvariant(character));
                pendingSpace = false;
            }
            else
            {
                // Punctuation and whitespace only separate words.
                pendingSpace = true;
            }
        }

        return result.ToString();
    }
}
