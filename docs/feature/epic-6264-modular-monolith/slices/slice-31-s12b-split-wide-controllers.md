# Slice 31 — S12b Other wide controllers: split by capability or move logic to a query port

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M–L** (per-controller sub-slices)

## Goal

DeliveriesController, FeaturesController, PortfolioController and PortfoliosController lose their S107 suppressions and their non-HTTP logic.

## IN

- Per controller, whichever fits: sibling controllers per capability (same route prefix, same `RbacGuard`), or non-HTTP logic moved into a query port.
- Shrink the S05a controller-on-repository list where these controllers drop repositories.

## OUT

- No `[FromServices]` action injection, no parameter-object bundling to get under the threshold.

## Hypothesis

Each controller hides two or three capabilities. **Disproved if** splitting fragments one cohesive resource. Then prefer moving logic to a query port.

## Acceptance criteria

- [ ] `grep -ln "disable S107" API/*Controller.cs` is empty (after S12a too).
- [ ] Same routes, same `RbacGuard` on every action (RBAC ArchUnit rules green), same responses.
- [ ] Per-behaviour cases moved to unit tests. One HTTP test per endpoint kept.

## Dependencies

- Soft: S12a (pattern), S05c/S05d.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one controller per slice).
