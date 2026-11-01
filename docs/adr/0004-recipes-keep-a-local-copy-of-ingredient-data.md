# 4. Recipes keep a local copy of ingredient data

- **Status:** Accepted
- **Date:** 2026-10-31

## Context

A recipe's nutrition needs, for every ingredient line, the ingredient's nutrients per 100 g and its density
and piece weight (to turn "2 tbsp" or "3 pieces" into grams). Catalog owns that data. Recipes may not
reference Catalog's Domain or Infrastructure (ADR 0001), and in v2 Catalog runs as its own service, so
Recipes reading Catalog's tables, or calling Catalog on every recipe read, would couple the two at runtime:
a recipe page would fail whenever Catalog is down or slow.

## Decision

Recipes keeps its own copy of the ingredient data it needs, in `recipes."Ingredients"`
(`CatalogIngredient`: Catalog's id, both names, `NutrientProfile` per 100 g, `UnitConversion`).
There is one row per Catalog ingredient that a recipe uses; `RecipeIngredient` lines store only the
ingredient's id, the `Quantity` and a note, with a foreign key to the copy.

- **v1:** when a recipe is saved, Recipes asks Catalog's public API (in process) for the ingredients the
  recipe uses and creates or refreshes their copies in the same transaction as the recipe. That API is
  `ICatalogIngredients` in `Catalog.Contracts`, a project with interfaces and DTOs only: Recipes references it,
  never Catalog's Domain or Infrastructure, and in v2 the same interface gets an HTTP implementation.
- **v2 (M7):** Catalog publishes `IngredientNutritionChanged`; Recipes' consumer refreshes the one copy row.
  Saving a recipe with an ingredient Recipes has never seen still needs one call to Catalog (or the event
  feed has to include new ingredients); this is decided with the extraction.

Recipes has its own `NutrientProfile` instead of sharing Catalog's: five fields with `Scale` and `+` are a
small price for not having a shared kernel that ties two future services together.

`Recipe.AddIngredient` takes the `CatalogIngredient`, not just its id, and rejects a quantity whose unit
can't be converted to grams for it ("2 pieces of flour"), so every saved line has a nutrition value.

## Consequences

- Reading a recipe and calculating its nutrition touches only the `recipes` schema; it keeps working while
  Catalog is down (in v2).
- A copy row is shared by all recipes, so a nutrition fix in Catalog updates every recipe's totals with a
  single row update, instead of rewriting each recipe's lines.
- Nutrition is current, not historical: a recipe shows today's Catalog values, not the ones it was saved with.
  That suits a home app; a calorie log would need its own snapshot.
- In v1 a copy is refreshed only when some recipe using it is saved, so it can lag behind Catalog until then.
  In v2 the lag is the event delivery time.
- If Catalog later removes an ingredient's density or piece weight, a recipe measuring it in that unit can no
  longer be calculated; `CalculateNutrition` throws rather than counting it as 0 g. The copy is never deleted
  while recipes use it (foreign key with `RESTRICT`).
