using System.Globalization;
using Catalog.Domain;
using Catalog.Infrastructure.Seeding;

namespace Catalog.UnitTests;

public class IngredientCsvTests
{
    [Fact]
    public void Reads_quoted_fields_and_nutrients()
    {
        var rows = Read(
            IngredientCsv.Header,
            "171287,\"Egg, whole, raw, fresh\",Tojás,143,12.56,0.72,9.51,0,1.027,50",
            "1,\"Beef, trimmed to 1/8\"\" fat\",Marha,250,20,0,18.5,0,,");

        Assert.Equal(2, rows.Count);
        Assert.Equal(
            new IngredientSeedRow("171287", "Egg, whole, raw, fresh", "Tojás", new NutrientProfile(143m, 12.56m, 0.72m, 9.51m, 0m), 1.027m, 50m),
            rows[0]);
        Assert.Equal("Beef, trimmed to 1/8\" fat", rows[1].Name);
    }

    [Fact]
    public void Empty_unit_cells_mean_no_value()
    {
        var rows = Read(IngredientCsv.Header, "1,\"Oil, olive\",Olívaolaj,884,0,0,100,0,0.913,");

        Assert.Equal(0.913m, rows[0].DensityGramsPerMl);
        Assert.Null(rows[0].PieceWeightGrams);
    }

    [Fact]
    public void Parses_numbers_the_same_way_under_a_Hungarian_culture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // hu-HU uses a decimal comma; the file always uses a dot.
            CultureInfo.CurrentCulture = new CultureInfo("hu-HU");

            var rows = Read(IngredientCsv.Header, "1,Egg,Tojás,143,12.56,0.72,9.51,0,1.027,50");

            Assert.Equal(12.56m, rows[0].NutrientsPer100g.Protein);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Rejects_an_unexpected_header()
    {
        Assert.Throws<FormatException>(() => Read("id,name", "1,Egg"));
    }

    [Theory]
    [InlineData("1,Egg,Tojás,abc,12.56,0.72,9.51,0,,")]
    [InlineData("1,Egg,Tojás,-1,12.56,0.72,9.51,0,,")]
    [InlineData("1,Egg,Tojás,143,12.56,0.72,9.51,0,abc,")]
    [InlineData("1,Egg,Tojás,143,12.56,0.72,9.51,0")]
    [InlineData("1,Egg,Tojás,143,12.56")]
    public void Reports_the_line_of_an_invalid_row(string invalidRow)
    {
        var exception = Assert.Throws<FormatException>(() =>
            Read(IngredientCsv.Header, "1,Egg,Tojás,143,12.56,0.72,9.51,0,,", invalidRow));

        Assert.StartsWith("Invalid ingredient on line 3", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Embedded_seed_file_holds_only_valid_unique_ingredients()
    {
        var rows = IngredientSeeder.ReadEmbeddedRows();

        Assert.True(rows.Count >= 300, $"Expected a few hundred ingredients, found {rows.Count}.");
        Assert.Equal(rows.Count, rows.Select(r => r.SourceId).Distinct().Count());
        Assert.Equal(rows.Count, rows.Select(r => r.HungarianName).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Every row must pass the same validation the seeder applies.
        foreach (var row in rows)
        {
            Ingredient.Create(
                row.Name, row.HungarianName, row.NutrientsPer100g, IngredientSeeder.Source, row.SourceId, row.DensityGramsPerMl, row.PieceWeightGrams);
        }
    }

    [Fact]
    public void Embedded_seed_file_has_unit_data_for_common_ingredients()
    {
        var rows = IngredientSeeder.ReadEmbeddedRows().ToDictionary(r => r.HungarianName);

        Assert.Equal(50m, rows["Tojás"].PieceWeightGrams);
        Assert.InRange(rows["Olívaolaj"].DensityGramsPerMl!.Value, 0.85m, 0.95m);
        Assert.InRange(rows["Búzaliszt (finomliszt)"].DensityGramsPerMl!.Value, 0.5m, 0.6m);
    }

    private static IReadOnlyList<IngredientSeedRow> Read(params string[] lines) =>
        IngredientCsv.Read(new StringReader(string.Join('\n', lines)));
}
