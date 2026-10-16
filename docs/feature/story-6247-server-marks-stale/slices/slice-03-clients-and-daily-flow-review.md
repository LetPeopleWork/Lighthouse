# Slice 03 — Clients and the Daily Flow Review read it

**Goal:** `lh` / MCP pass the server's staleness through, and the Daily Flow Review lists stale Work Items.

**Stories:** US-03

## IN (lighthouse-clients repo)
- `client` types + `metricsWording` carry `isStale` / reasons; the WIP `summary` names stale Work Items.
- `lighthouse_team_metrics_wip` description and `lh metrics team|portfolio --metrics wip` mention the fields.
- Daily Flow Review: a Stale item in what to discuss (not duplicating Blocked), the "Lighthouse does not say" fallback, calm-day rule, evals.
- Minor version bumps; version gate on the Lighthouse release carrying slice 01.

## OUT
- Any client-side staleness computation.
- Changes to the general `lighthouse` skill beyond naming the field.

## Learning hypothesis
Disproves "pass-through is enough" if the skill still needs to compute anything to place a stale Work Item.

## Acceptance criteria
US-03 AC 1–5 (`feature-delta.md`).

## Dependencies
Slice 01 released (or a local instance running it).

## Effort
~1 day.

## Dogfood
Run the Daily Flow Review against a local instance with demo data that has a stale Work Item.
