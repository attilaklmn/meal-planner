// Builds the Catalog seed file from the USDA FoodData Central "SR Legacy" download (see docs/adr/0003).
// selection.csv lists which foods to take and their Hungarian names; this script adds the nutrients.
//
// Run from the repository root:
//   dotnet run tools/catalog-data/build-ingredients.cs -- <path to FoodData_Central_sr_legacy_food_csv_2018-04.zip>

using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualBasic.FileIO;

const string SelectionPath = "tools/catalog-data/selection.csv";
const string OutputPath = "src/Modules/Catalog/Catalog.Infrastructure/Seeding/ingredients.csv";

// USDA nutrient ids; amounts are per 100 g of the food.
const string EnergyKcal = "1008";
const string Protein = "1003";
const string Carbohydrate = "1005"; // "Carbohydrate, by difference", includes fiber
const string Fat = "1004";
const string Fiber = "1079";
string[] wantedNutrients = [EnergyKcal, Protein, Carbohydrate, Fat, Fiber];

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

var errors = new List<string>();
var output = new StringBuilder("source_id,name,hungarian_name,energy_kcal,protein_g,carbohydrate_g,fat_g,fiber_g\n");

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
Console.WriteLine($"Wrote {selection.Count} ingredients to {OutputPath}.");
return 0;

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
