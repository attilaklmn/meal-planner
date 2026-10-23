# 1. Start as a modular monolith, extract services later

- **Status:** Accepted
- **Date:** 2026-10-23

## Context

meal-planner is a home-use app with a handful of users, built in evening sessions. It is also meant
to demonstrate microservice skills in job interviews. Starting with microservices from day one would
mean distributed infrastructure (broker, gateway, per-service deployment) before there is a single
working feature, and service boundaries guessed before the domain is understood.

## Decision

Build v1 as a modular monolith: one API host, three modules (Catalog, Recipes, Planning), each with
its own DbContext and its own PostgreSQL schema. Modules may not reference each other's Domain or
Infrastructure projects; an architecture test enforces this.

In v2, extract Catalog and Planning into separate services, because their load profiles differ from
the rest: Catalog does heavy, infrequent import batches; Planning serves many short, real-time
requests. Recipes stays in the main API. Services sync through integration events with a
transactional outbox instead of synchronous calls.

## Consequences

- A usable app exists after about four evenings, before any distributed infrastructure.
- Module boundaries are tested in practice before they become network boundaries.
- Extraction is mostly a new host project plus messaging, not a rewrite, because each module
  already owns its schema.
- Until v2, modules share one process and one database server, so a failure in one can affect all.
- v2 brings eventual consistency (e.g. Recipes' copy of nutrition data can briefly lag behind
  Catalog), which the UI and tests must account for.
