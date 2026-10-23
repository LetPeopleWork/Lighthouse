# Slice 25 — S08 Aggregates and invariants

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** (catalogue step); follow-ups sized at its DESIGN · Natural DESIGN owner: nw-ddd-architect

## Goal

For each bounded context, name the aggregate roots and their consistency boundaries: what must be valid within one transaction. Then move the service-enforced invariants that belong to an aggregate onto it, so invalid states cannot be built.

## IN

- **Catalogue first.** For each context:
  - Its aggregate roots. Starting points: the concurrency-tokened config roots Team, Portfolio, WorkTrackingSystemConnection, Delivery, UserProfile / RbacGroupMapping / ApiKey; also Feature (rich), WorkItem (anemic), Report.
  - Each root's consistency boundary.
  - The invariants services enforce today: file, rule, proposed home (aggregate / value object / stays a policy).
- Cross-aggregate references go **by ID**. List today's object references that cross an aggregate boundary.
- Per-aggregate follow-up Stories are decided at this Story's DESIGN, each releasable alone. Move invariants into constructors and mutators, prove them with entity unit tests, and delete the service checks.

## OUT

- Pure calculators and policies stay as they are (brief non-goal). No calculation logic is forced onto entities.
- Table ownership (S10, which consumes this catalogue).

## Hypothesis

Most service-enforced invariants belong to one aggregate. **Disproved if** most span aggregates. Then they are domain services or policies by nature, and the catalogue says so.

## Acceptance criteria

- [ ] Catalogue per context: roots, boundaries, invariants with proposed homes, and cross-aggregate object references.
- [ ] Follow-up Stories listed, each with its own Elevator Pitch.
- [ ] Any follow-up done within this Story is behaviour-free, entity-unit-tested, mutation score held.

## Dependencies

- Soft: S01 (contexts and glossary).
- Feeds S13 (hard: persistence follows the aggregates), S10 (hard) and S07c (soft).

## Notes for DESIGN

Open with a design-level Event Storming session per context, run together with the maintainer and building on S01's Big Picture board. The commands, events and business rules on that board show which data has to stay consistent within one transaction; that is where the aggregate boundaries go. Its output feeds S13 (e.g. whether a forecast belongs to the Feature at all).

ADR-164 (archived-Delivery write refusal in the aggregate) and ADR-169 (remote-owned fields refuse hand mutation) are in-repo precedents for "invariant in the aggregate".

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one context's catalogue per slice).
