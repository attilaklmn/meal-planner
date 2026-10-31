# 3. Seed the ingredient catalog from a curated USDA SR Legacy subset

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

Recipes calculate nutrition from their ingredients, so the catalog needs reliable values per 100 g
for everyday ingredients, and Hungarian names, because the app is used in Hungarian. The candidates:

- **USDA FoodData Central, SR Legacy**: ~7,800 generic foods with lab-based values, public domain
  (CC0). Frozen since 2018, but nutrition of generic foods barely changes. Every row has energy,
  protein, carbohydrate and fat; fiber is missing for a few.
- **USDA FoodData Central, Foundation Foods**: newer and very precise, but only a few hundred foods,
  many without a plain kcal value.
- **Open Food Facts**: very large and includes Hungarian products, but mostly branded and
  crowd-sourced (uneven quality), and the ODbL share-alike licence applies to derived databases.

Neither USDA dataset has Hungarian names.

## Decision

- Use **SR Legacy**. The FDC id is the ingredient's `SourceId`, with `Source = "usda"`.
- `tools/catalog-data/selection.csv` lists the chosen foods: FDC id, USDA description (checked
  against the dataset to catch typos in ids) and a hand-reviewed Hungarian name.
- `tools/catalog-data/build-ingredients.cs` (a .NET file-based app) reads the SR Legacy CSV zip and
  writes `Catalog.Infrastructure/Seeding/ingredients.csv` with energy (kcal), protein,
  carbohydrate (by difference, includes fiber), fat and fiber per 100 g. The zip is not committed.
- Unit data (added in M2): the script derives density (g/ml) and piece weight (g) from SR Legacy's
  household portions (`food_portion.csv`). Density comes from a bare spoon or cup measure ("tbsp"
  before "cup, chopped"), converted with US volumes; piece weight from a size ("medium" before
  "large" and "small") or a piece-like portion ("clove", "head"). `tools/catalog-data/unit-overrides.csv`
  corrects or removes derived values, with a reason per row.
- The seed file is an embedded resource. In Development the Api applies it on start, after
  migrations: missing rows are added and changed ones updated, matched by (Source, SourceId).
  Rows removed from the file are not deleted, because recipes may reference them.

## Consequences

- The data and how it was produced are both reviewable in the repo; changing the selection is a
  PR that shows the effect on `ingredients.csv`.
- Missing fiber is stored as 0 (8 of 372 foods, listed by the script). Close for most of them;
  a known inaccuracy for the rest.
- USDA descriptions are long ("Chicken, broiler or fryers, breast, skinless, boneless, meat only,
  raw"); the UI shows the Hungarian name first.
- Hungarian-only foods (e.g. specific sausages, túró variants) are approximated by the nearest
  USDA food. User-defined ingredients would need their own `Source` later.
- 272 of 372 foods get a density and 74 a piece weight. The rest can only be measured in grams
  (or in volumes/pieces after an override); the recipe editor has to show that.
- Portion descriptions are free text, so the derivation is a heuristic. Its results were reviewed
  once by hand, and the overrides file records every correction.
- Seeding outside Development (e.g. a production deploy) is still open; it will come with the
  deployment work in M8.
