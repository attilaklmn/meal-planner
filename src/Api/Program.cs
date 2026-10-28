using Catalog.Api;
using Planning.Api;
using Recipes.Api;

// Must match the database resource name in the AppHost.
const string Database = "mealplanner";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCatalogModule(Database);
builder.AddRecipesModule(Database);
builder.AddPlanningModule(Database);

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapCatalogEndpoints();
app.MapRecipesEndpoints();
app.MapPlanningEndpoints();

app.Run();
