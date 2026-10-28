using Catalog.Api;
using Planning.Api;
using Recipes.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapCatalogEndpoints();
app.MapRecipesEndpoints();
app.MapPlanningEndpoints();

app.Run();
