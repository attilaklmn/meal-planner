// Builds the Catalog seed file from the USDA FoodData Central "SR Legacy" download (see docs/adr/0003).
// selection.csv lists which foods to take and their Hungarian names; this script adds the nutrients, and the
// density and piece weight derived from USDA's household portions ("1 tbsp = 13.5 g", "1 large = 50 g").
// unit-overrides.csv corrects or removes derived values where the portions are missing or misleading.
//
// Run from the repository root:
//   dotnet run tools/catalog-data/build-ingredients.cs -- <path to FoodData_Central_sr_legacy_food_csv_2018-04.zip>

using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualBasic.FileIO;

const string SelectionPath = "tools/catalog-data/selection.csv";
const string UnitOverridesPath = "tools/catalog-data/unit-overrides.csv";
const string OutputPath = "src/Modules/Catalog/Catalog.Infrastructure/Seeding/ingredients.csv";

// USDA nutrient ids; amounts are per 100 g of the food.
const string EnergyKcal = "1008";
const string Protein = "1003";
const string Carbohydrate = "1005"; // "Carbohydrate, by difference", includes fiber
const string Fat = "1004";
const string Fiber = "1079";
string[] wantedNutrients = [EnergyKcal, Protein, Carbohydrate, Fat, Fiber];

// USDA's household measures are US ones, so densities are derived with US volumes.
(string Name, decimal Milliliters)[] volumeMeasures =
[
    ("tbsp", 14.787m), ("tablespoon", 14.787m), ("tsp", 4.929m), ("teaspoon", 4.929m), ("cup", 236.588m), ("fl oz", 29.574m),
];

// Portion names that mean "one piece": sizes anywhere in the description ("Potato medium", "croissant, medium"),
// most typical first, since a recipe's "2 apples" means medium ones; then nouns that start it ("clove").
string[] pieceSizes = ["medium", "large", "small"];
string[] pieceMeasures = ["fruit", "piece", "whole", "clove", "head"];

// Portions starting with these are amounts, not pieces, even with a size in them ("cup, large curd").
string[] notPieces = ["cup", "tbsp", "tablespoon", "tsp", "teaspoon", "fl", "oz", "lb", "package", "can", "container", "serving", "nlea", "bag", "box", "packet", "slice", "wedge"];

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: dotnet run tools/catalog-data/build-ingredients.cs -- <SR Legacy CSV zip>");
    return 1;
}

var selection = ReadCsv(File.OpenRead(SelectionPath)).ToList();
var selectedIds = selection.Select(row => row["fdc_id"]).ToHashSet();

using var zip = ZipFile.OpenRead(args[0]);

// Guards against a typo in an id: the description in selection.csv must match USDA's.
var descriptions = ReadCsv(OpenEntry(zip, "food.csv"))
    .Where(row => selectedIds.Contains(row["fdc_id"]))
    .ToDictionary(row => row["fdc_id"], row => row["description"]);

var nutrients = selectedIds.ToDictionary(id => id, _ => new Dictionary<string, decimal>());
foreach (var row in ReadCsv(OpenEntry(zip, "food_nutrient.csv")))
{
    if (nutrients.TryGetValue(row["fdc_id"], out var amounts) && wantedNutrients.Contains(row["nutrient_id"]))
    {
        amounts[row["nutrient_id"]] = decimal.Parse(row["amount"], CultureInfo.InvariantCulture);
    }
}

// Portions as (description, grams for one of it), in USDA's order, which lists the common measure first.
var portions = selectedIds.ToDictionary(id => id, _ => new List<(string Description, decimal Grams)>());
foreach (var row in ReadCsv(OpenEntry(zip, "food_portion.csv")))
{
    if (portions.TryGetValue(row["fdc_id"], out var list))
    {
        var amount = decimal.Parse(row["amount"], CultureInfo.InvariantCulture);
        var grams = decimal.Parse(row["gram_weight"], CultureInfo.InvariantCulture);
        list.Add((row["modifier"].Trim().ToLowerInvariant(), grams / amount));
    }
}

// Empty cell: keep the derived value; a number: use it instead; "none": no value.
var overrides = ReadCsv(File.OpenRead(UnitOverridesPath)).ToDictionary(row => row["fdc_id"]);

var errors = new List<string>();
var output = new StringBuilder("source_id,name,hungarian_name,energy_kcal,protein_g,carbohydrate_g,fat_g,fiber_g,density_g_per_ml,piece_weight_g\n");
var withDensity = 0;
var withPieceWeight = 0;

foreach (var id in overrides.Keys.Where(id => !selectedIds.Contains(id)))
{
    errors.Add($"{id}: listed in unit-overrides.csv but not in selection.csv.");
}

foreach (var row in selection)
{
    var id = row["fdc_id"];
    if (!descriptions.TryGetValue(id, out var description) || description != row["usda_description"])
    {
        errors.Add($"{id}: description in selection.csv doesn't match USDA ('{description}').");
        continue;
    }

    var amounts = nutrients[id];
    var missing = wantedNutrients.Where(n => n != Fiber && !amounts.ContainsKey(n)).ToList();
    if (missing.Count > 0)
    {
        errors.Add($"{id} {description}: missing nutrients {string.Join(", ", missing)}.");
        continue;
    }

    // A handful of foods have no fiber value in SR Legacy; 0 is close for them and keeps the model simple.
    if (!amounts.ContainsKey(Fiber))
    {
        Console.WriteLine($"Note: no fiber value for {id} {description}, using 0.");
    }

    var density = DeriveDensity(portions[id], volumeMeasures);
    var pieceWeight = DerivePieceWeight(portions[id], pieceSizes, pieceMeasures, notPieces);
    if (overrides.TryGetValue(id, out var unitOverride))
    {
        density = Override(density, unitOverride["density_g_per_ml"]);
        pieceWeight = Override(pieceWeight, unitOverride["piece_weight_g"]);
    }

    // Same limit as Ingredient.MaxDensityGramsPerMl; failing here is clearer than failing in the seeder.
    if (density is <= 0 or > 3)
    {
        errors.Add($"{id} {description}: implausible density {density} g/ml, fix it in unit-overrides.csv.");
        continue;
    }

    withDensity += density is null ? 0 : 1;
    withPieceWeight += pieceWeight is null ? 0 : 1;

    string[] fields =
    [
        id,
        description,
        row["hungarian_name"],
        Format(amounts[EnergyKcal]),
        Format(amounts[Protein]),
        Format(amounts[Carbohydrate]),
        Format(amounts[Fat]),
        Format(amounts.GetValueOrDefault(Fiber)),
        density is { } d ? Format(d) : "",
        pieceWeight is { } p ? Format(p) : "",
    ];
    output.AppendJoin(',', fields.Select(Quote)).Append('\n');
}

if (errors.Count > 0)
{
    errors.ForEach(Console.Error.WriteLine);
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
File.WriteAllText(OutputPath, output.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine($"Wrote {selection.Count} ingredients to {OutputPath}: {withDensity} with density, {withPieceWeight} with piece weight.");
return 0;

// From the first volume portion, preferring a bare measure ("tbsp") over a described one ("cup, chopped"):
// the bare ones are what recipes mean. Spoons come before cups, which are packed less consistently.
static decimal? DeriveDensity(List<(string Description, decimal Grams)> portions, (string Name, decimal Milliliters)[] measures)
{
    foreach (var bareOnly in new[] { true, false })
    {
        foreach (var (name, milliliters) in measures)
        {
            var match = portions.FirstOrDefault(p => bareOnly ? p.Description == name : StartsWithWord(p.Description, name));
            if (match.Description is not null)
            {
                return Math.Round(match.Grams / milliliters, 3);
            }
        }
    }

    return null;
}

static decimal? DerivePieceWeight(
    List<(string Description, decimal Grams)> portions, string[] sizes, string[] measures, string[] notPieces)
{
    var candidates = portions.Where(p => !notPieces.Any(word => StartsWithWord(p.Description, word))).ToList();

    foreach (var size in sizes)
    {
        var match = candidates.FirstOrDefault(p => ContainsWord(p.Description, size));
        if (match.Description is not null)
        {
            return Math.Round(match.Grams, 1);
        }
    }

    foreach (var name in measures)
    {
        var match = candidates.FirstOrDefault(p => StartsWithWord(p.Description, name));
        if (match.Description is not null)
        {
            return Math.Round(match.Grams, 1);
        }
    }

    return null;
}

static bool ContainsWord(string description, string word) =>
    description.Split([' ', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries).Contains(word);

// "cup, chopped" and "medium (2-3/4" dia)" start with the measure; "cupcake" doesn't.
static bool StartsWithWord(string description, string word) =>
    description.StartsWith(word, StringComparison.Ordinal)
    && (description.Length == word.Length || description[word.Length] is ' ' or ',' or '(');

static decimal? Override(decimal? derived, string cell) => cell.Trim() switch
{
    "" => derived,
    "none" => null,
    var value => decimal.Parse(value, CultureInfo.InvariantCulture),
};

static Stream OpenEntry(ZipArchive zip, string fileName) =>
    zip.Entries.Single(entry => entry.Name == fileName).Open();

// Yields each data row as column name -> value.
static IEnumerable<Dictionary<string, string>> ReadCsv(Stream stream)
{
    using var parser = new TextFieldParser(stream, Encoding.UTF8) { Delimiters = [","], HasFieldsEnclosedInQuotes = true };
    var header = parser.ReadFields() ?? [];

    while (parser.ReadFields() is { } fields)
    {
        yield return header.Zip(fields).ToDictionary(pair => pair.First, pair => pair.Second);
    }
}

static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

static string Quote(string field) =>
    field.Contains(',', StringComparison.Ordinal) || field.Contains('"', StringComparison.Ordinal)
        ? $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
        : field;
