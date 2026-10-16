# Slice 01 — Lighthouse marks stale on its WIP reads

**Goal:** Team and Portfolio WIP reads carry `isStale` and its reasons from one backend rule.

**Stories:** US-01

## IN
- One domain rule with the current frontend semantics (D4): Blocked precedence, time in state `>`, blocked duration `≥`, `0` = off, Doing only.
- Days in the instance's time zone (D8), measured as of the read's date or `asOfDate` (D5).
- `GET …/teams/{id}/metrics/wip` and `GET …/portfolios/{id}/metrics/wip` carry the fields (D1, D2).
- `WorkItemBecameStale` uses the same rule (D6).

## OUT
- Frontend changes (slice 02). Clients and skill (slice 03).
- New settings or semantics.

## Learning hypothesis
Disproves "the browser rule moves to the server unchanged" if the as-of snapshot or zone day count cannot reproduce the frontend's results on demo data.

## Acceptance criteria
US-01 AC 1–6 (`feature-delta.md`).

## Dependencies
None.

## Effort
~1 day.

## Dogfood
`lh metrics team --metrics wip --id <demo team>` against a local instance shows the flag on demo data.
