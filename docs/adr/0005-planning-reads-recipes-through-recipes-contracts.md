# 5. Planning reads recipes through Recipes.Contracts

- **Status:** Accepted
- **Date:** 2026-11-02

## Context

Planning puts recipes on a weekly plan and turns the planned week into a shopping list. For that it needs, per recipe,
the title, the servings, the energy per serving and the ingredient lines with their quantities. Recipes owns that data.
Planning may not reference Recipes' Domain or Infrastructure (ADR 0001), and in v2 Planning runs as its own service.

The shopping list merges quantities of the same ingredient across recipes. "1 onion" and "150 g onion" only add up
through the onion's piece weight, which Recipes has (its ingredient copy, ADR 0004) and Planning doesn't.

## Decision

Recipes gets a public API like Catalog's: `Recipes.Contracts`, a project with interfaces and DTOs only.
`IRecipeReader.GetByIdsAsync` returns `RecipeData` (id, title, servings, energy per serving) with its ingredient lines
(`RecipeIngredientData`: ingredient id, Hungarian name, amount, unit, grams). The contract has its own `IngredientUnit`
enum, mapped explicitly from the domain's `Unit`, so the domain can change without changing the contract by accident.

Every line carries its weight in grams, calculated by Recipes' domain (`Quantity.ToGrams`). Planning merges quantities
with its own, smaller unit logic (same unit: add up; same dimension: base unit) and falls back to the grams when the
dimensions differ, so it never needs densities or piece weights.

What Planning copies and what it reads:

- **Plan entries** store the recipe id plus a copy of the title and the energy per serving, so the weekly plan reads only
  the `planning` schema. The copy is refreshed whenever the entry is changed.
- **Shopping lists** are generated snapshots: generating reads the planned recipes through `IRecipeReader` once and
  stores the merged items. Editing a recipe afterwards doesn't change an existing list until it is regenerated.

- **v1:** `IRecipeReader` is implemented in `Recipes.Infrastructure` and called in process.
- **v2 (M7):** Recipes publishes `RecipeUpdated` with the same shape as `RecipeData`; Planning keeps its own copy of the
  recipes it has planned and generates lists from that copy, without a synchronous call.

## Consequences

- The weekly plan keeps working without Recipes (in v2); only adding an entry and generating a list need recipe data.
- A plan entry can show an outdated title or kcal value until it is changed (v1) or the event arrives (v2). A deleted
  recipe stays on the plan under its copied title.
- Grams are calculated with today's ingredient data, like a recipe's nutrition (ADR 0004).
- Unit logic exists twice, the full conversion in Recipes and merging in Planning: the price of not sharing a domain
  project between two future services.
