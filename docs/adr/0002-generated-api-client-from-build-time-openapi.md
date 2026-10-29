# 2. Generate the frontend API client from a build-time OpenAPI document and commit both

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

The React app must call the backend with types that match it exactly; hand-written request and
response types drift silently. Orval can generate TanStack Query hooks and types from an OpenAPI
document, but that document has to come from somewhere: a running Api (`/openapi/v1.json`) or the
build. The generated code can also either be committed or produced on every build.

## Decision

- `dotnet build` of `src/Api` writes the OpenAPI document to `src/Api/Api.json`
  (`Microsoft.Extensions.ApiDescription.Server`). No running backend or database is needed.
- `npm run generate:api` in `src/web` runs Orval on that file and writes `src/api/generated`.
- Both `Api.json` and the generated client are committed.
- Every generated request goes through `src/api/fetcher.ts`, which throws `ApiError` on non-2xx
  responses so TanStack Query reports them as errors.

## Consequences

- An API change shows up in the PR diff twice: in `Api.json` (the contract) and in the generated
  client (its effect on the frontend), which makes breaking changes easy to spot in review.
- A changed response type breaks `npm run build` where the frontend uses it, instead of failing at runtime.
- The frontend builds and its tests run without the .NET toolchain.
- Generated files can go stale if someone forgets to regenerate. CI should rebuild both and fail
  when the working tree changes (planned with the CI workflow).
- Startup code must not require a database or other infrastructure at registration time, because
  build-time document generation runs it without any configured.
