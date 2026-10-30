using System.Text.Json.Serialization;
using Catalog.Api;
using Planning.Api;
using Recipes.Api;

// Must match the database resource name in the AppHost.
const string Database = "mealplanner";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// The web defaults also accept numbers sent as strings ("12.5"), so OpenAPI describes every number as
// `number | string` and the generated TypeScript types follow. Strict keeps them plain numbers.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.AddCatalogModule(Database);
builder.AddRecipesModule(Database);
builder.AddPlanningModule(Database);

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // Live document at /openapi/v1.json; the committed Api.json is produced at build time instead.
    app.MapOpenApi();
}

app.MapCatalogEndpoints();
app.MapRecipesEndpoints();
app.MapPlanningEndpoints();

app.Run();
