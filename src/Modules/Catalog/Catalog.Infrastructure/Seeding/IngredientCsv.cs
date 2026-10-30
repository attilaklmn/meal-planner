using System.Globalization;
using Catalog.Domain;
using Microsoft.VisualBasic.FileIO;

namespace Catalog.Infrastructure.Seeding;

internal sealed record IngredientSeedRow(string SourceId, string Name, string HungarianName, NutrientProfile NutrientsPer100g);

/// <summary>Reads the seed file written by tools/catalog-data/build-ingredients.cs.</summary>
internal static class IngredientCsv
{
    public const string Header = "source_id,name,hungarian_name,energy_kcal,protein_g,carbohydrate_g,fat_g,fiber_g";

    public static IReadOnlyList<IngredientSeedRow> Read(TextReader reader)
    {
        // TextFieldParser ships with .NET and handles quoted fields ("Egg, whole, raw"), so no CSV package is needed.
        using var parser = new TextFieldParser(reader) { Delimiters = [","], HasFieldsEnclosedInQuotes = true };

        var header = parser.ReadFields();
        if (header is null || string.Join(',', header) != Header)
        {
            throw new FormatException($"Expected the header '{Header}'.");
        }

        var rows = new List<IngredientSeedRow>();
        // Counted by hand: TextFieldParser.LineNumber turns -1 once the last line is read. Fields never contain line breaks.
        var line = 1;
        while (parser.ReadFields() is { } fields)
        {
            line++;
            try
            {
                rows.Add(ToRow(fields));
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw new FormatException($"Invalid ingredient on line {line}: {exception.Message}", exception);
            }
        }

        return rows;
    }

    private static IngredientSeedRow ToRow(string[] fields)
    {
        if (fields.Length != 8)
        {
            throw new FormatException($"Expected 8 fields, found {fields.Length}.");
        }

        var nutrients = new NutrientProfile(
            energyKcal: Number(fields[3]),
            protein: Number(fields[4]),
            carbohydrate: Number(fields[5]),
            fat: Number(fields[6]),
            fiber: Number(fields[7]));

        return new IngredientSeedRow(fields[0], fields[1], fields[2], nutrients);
    }

    // Always "12.56", whatever the server's culture is (a Hungarian one would expect "12,56").
    private static decimal Number(string field) => decimal.Parse(field, NumberStyles.Number, CultureInfo.InvariantCulture);
}
