# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

**meal-planner** is a home-use recipe, nutrition (calorie/macro) and shopping-list app.
It is also a portfolio project for full-stack job interviews, so *how* things are built
matters as much as what they do: clear module boundaries, deliberate EF Core usage,
justified microservice decisions, modern React data handling, tests and observability.

Full plan: [docs/architecture.md](docs/architecture.md). Decisions: [docs/adr/](docs/adr/).

## Current status

M0 – Foundations is done (Aspire, Postgres with a DbContext and schema per module, React + Vite,
generated API client, CI). M1 – Ingredient catalog is done: `Ingredient` / `NutrientProfile` domain
model persisted in `catalog."Ingredients"` (complex type), seeded in Development with 372 USDA SR
Legacy ingredients with Hungarian names (ADR 0003), searchable via
`GET /api/catalog/ingredients?search=` (accent-insensitive, trigram index), picked in the frontend
with `IngredientAutocomplete` (shown on a temporary demo section). **M2 – Recipes** in progress: `Quantity` value
object with unit conversion in `Recipes.Domain` (unit-tested in `tests/Recipes.UnitTests`); ingredients carry
optional density and piece weight derived from USDA household portions; `Recipe` aggregate with
`RecipeIngredient` lines and `CalculateNutrition()`, persisted in `recipes."Recipes"` / `"RecipeIngredients"` (steps jsonb,
tags text[]), with Recipes' own ingredient copy in `recipes."Ingredients"` (ADR 0004); CRUD endpoints under
`/api/recipes` (copies refreshed on save through `Catalog.Contracts`, try them with `src/Api/Api.http`).
Next: recipe list and details pages.
Update this line when a milestone closes.

## Tech stack

- Backend: .NET 10, ASP.NET Core Minimal APIs, EF Core 10 (Npgsql), PostgreSQL
- Orchestration: .NET Aspire (AppHost + ServiceDefaults)
- Auth: ASP.NET Core Identity, JWT access + refresh tokens
- Messaging (v2): RabbitMQ, MassTransit v8 or Wolverine, transactional outbox
- Gateway (v2): YARP
- Real-time: SignalR
- Frontend: React 19, TypeScript, Vite, TanStack Query, React Hook Form + Zod,
  Tailwind + shadcn/ui, API client generated from OpenAPI (Orval), oxlint + Prettier
- Tests: xUnit, Testcontainers, NetArchTest/ArchUnitNET, Vitest, Testing Library, MSW, Playwright
- Observability: OpenTelemetry; CI: GitHub Actions, Docker

## Architecture in one paragraph

v1 is a **modular monolith**: one API host loads three modules (Catalog, Recipes, Planning),
each with its own DbContext and its own PostgreSQL schema. In v2, Catalog and Planning are
extracted into their own services behind a YARP gateway, and modules sync via integration
events (`IngredientNutritionChanged`: Catalog → Recipes, `RecipeUpdated`: Recipes → Planning)
instead of synchronous calls.

## Planned layout

```
src/
  AppHost/            Aspire orchestration
  ServiceDefaults/    OpenTelemetry, health checks, resilience
  Api/                v1 host, loads all modules
  Gateway/            YARP (v2)
  Modules/
    Catalog/          Catalog.Domain, Catalog.Infrastructure, Catalog.Api, Catalog.Contracts
    Recipes/          same split
    Planning/         same split
  Services/           v2 hosts (Catalog.Service, Planning.Service)
  Shared/Contracts/   integration events only
  web/                React + Vite app, src/features/{recipes,catalog,planning,shopping}
tests/                *.UnitTests, *.IntegrationTests, e2e/
docs/                 architecture.md, adr/
```

## Workflow

- `main` is protected: every change goes through a PR. Never commit or push to `main` directly.
- CI (`.github/workflows/ci.yml`) runs on every PR and push to `main`. Its jobs `backend`, `frontend` and
  `api-client` are required status checks; renaming a job means updating branch protection too.
  When bumping the Aspire SDK, bump `ASPIRE_CLI_VERSION` in the workflow as well.
- One GitHub issue → one branch → one PR. Branch name: `<issue-number>-<short-kebab-name>`,
  e.g. `12-recipe-editor-form`.
- PR description contains `Closes #<issue-number>`. PRs are squash-merged.
- Commit messages: Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `chore:`, `refactor:`).
- Architecture-relevant choices get a short ADR in `docs/adr/` (next free number, same format
  as `0001`).

## Backend conventions

- Nullable reference types on, warnings as errors.
- Minimal API endpoints grouped per module (`MapGroup`); no controllers. Handlers return `TypedResults`
  types and set `WithName` (OpenAPI operationId = generated hook name). JSON number handling is strict,
  so OpenAPI (and the generated TypeScript) types numbers as `number`, not `number | string`. Enums are
  serialized by name; request records are deserialized with required constructor parameters and nullable
  annotations respected, so a missing or null field is a 400.
- Validation lives in the domain (it throws `ArgumentException`); endpoints translate those errors into a
  `ValidationProblem` keyed by request field (see `Recipes.Api/ValidationErrors.cs`) instead of repeating the rules.
- Text search goes through `Catalog.Domain.SearchText.Normalize` on both sides: stored `SearchText`
  columns and the search term (lowercase, no accents, punctuation → spaces).
- No MediatR, no AutoMapper: plain handler classes and explicit mapping.
- Modules never reference another module's Domain or Infrastructure project. Cross-module
  communication only via `Shared/Contracts` (events) or, in v1, a module's public API surface:
  its `<Module>.Contracts` project (interfaces + DTOs, no dependencies), implemented in its Infrastructure.
  An architecture test enforces this.
- Value objects (`Quantity`, `NutrientProfile`) are immutable records with their own logic,
  mapped as EF Core complex/owned types.
- EF Core: one DbContext per module with its own schema; migrations per module; reads use
  projections + `AsNoTracking`; no lazy loading; concurrency tokens where users can collide
  (shopping list items).
- Domain logic (unit conversion, nutrition calculation, list merging) is unit-tested without
  a database. Endpoints and persistence get integration tests against real PostgreSQL via
  Testcontainers.

## Frontend conventions

- Feature folders under `src/features/`; shared UI in `src/components/` (`components/ui` is generated by
  shadcn/ui: add components with `npx shadcn@latest add <name>`, then `npx prettier --write src/components/ui`;
  don't hand-edit unless needed). Shared hooks in `src/hooks/`.
- Component tests: `renderWithQueryClient` + `stubFetch` (exact URL incl. query string → response);
  `src/test/setup.ts` polyfills the browser APIs jsdom lacks (ResizeObserver, scrollIntoView) for cmdk/Radix.
- The browser only talks to the Vite dev server, which proxies `/api` to the Api (no CORS setup).
- Server state lives in TanStack Query only; no global state library.
- API calls go through the generated client; never hand-write request types that exist in OpenAPI.
  Never edit `src/api/generated` by hand. All generated requests use `src/api/fetcher.ts`, which throws
  `ApiError` on non-2xx so queries reach their error state.
- Forms: React Hook Form + Zod schemas.

## Working with the owner

The owner builds this to learn and to explain it in interviews. When implementing:

- Prefer small, reviewable steps that map to one issue.
- Briefly explain non-obvious decisions and the trade-off behind them.
- Ask before adding a new dependency or changing the architecture described above.
- Keep `docs/architecture.md` and this file up to date when something changes.

## Commands

Fill in as the solution takes shape:

- Prerequisites: .NET 10 SDK, Aspire CLI (`aspire` on PATH, needed to build the AppHost), Docker running
- Build: `dotnet build` (solution: `MealPlanner.slnx`; shared settings in `Directory.Build.props`,
  package versions only in `Directory.Packages.props`)
- Run everything: `dotnet run --project src/AppHost` (Aspire dashboard URL is printed on start).
  Postgres runs as a persistent container; in Development the Api applies pending migrations on start.
- Restore local tools (dotnet-ef): `dotnet tool restore`
- Add a migration (per module, no running database needed):
  `dotnet ef migrations add <Name> --project src/Modules/<Module>/<Module>.Infrastructure --output-dir Persistence/Migrations`
  (`.editorconfig` marks that folder as generated code, so analyzers don't fail the build on it).
  Review the SQL with `dotnet ef migrations script <From> <To> --project ...`.
- Backend tests: `dotnet test` (xUnit v3 on Microsoft.Testing.Platform, opted in via `global.json`;
  in CI `dotnet test --solution MealPlanner.slnx`). Test projects live in `tests/<Module>.UnitTests`;
  `tests/Directory.Build.props` allows `Sentence_style_test_names`.
- Frontend (in `src/web`): `npm run dev` (standalone, proxies `/api` to `localhost:5030`; under Aspire the
  AppHost starts it), `npm test`, `npm run lint` (oxlint), `npm run format` / `format:check` (Prettier),
  `npm run build`
- API client: `dotnet build src/Api` writes `src/Api/Api.json` (OpenAPI), then in `src/web`
  `npm run generate:api` regenerates `src/api/generated`. Both are committed; regenerate after every
  endpoint or DTO change. Live document in Development: `/openapi/v1.json`.
- Ingredient seed data: edit `tools/catalog-data/selection.csv` (foods, Hungarian names) or
  `tools/catalog-data/unit-overrides.csv` (density / piece weight corrections), download the USDA SR Legacy
  CSV zip (FoodData Central downloads, not committed), then
  `dotnet run tools/catalog-data/build-ingredients.cs -- <zip>` rewrites
  `src/Modules/Catalog/Catalog.Infrastructure/Seeding/ingredients.csv`. Commit the edited and generated files.
