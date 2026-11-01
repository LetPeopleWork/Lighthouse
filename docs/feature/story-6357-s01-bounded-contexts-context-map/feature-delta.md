# Story 6357 — S01 Bounded contexts, context map and layout (ADR)

ADO Story #6357, child of Epic #6264. Epic workspace: [`../epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md).
Brief: [`../epic-6264-modular-monolith/slices/slice-01-s01-bounded-contexts-context-map-layout.md`](../epic-6264-modular-monolith/slices/slice-01-s01-bounded-contexts-context-map-layout.md).

DISCUSS: covered at Epic level (see the Epic's Wave Decisions); waves start at DESIGN.

Picked up 2026-11-01 by maintainer choice, ahead of the test-infrastructure track (G0b). That track runs later
the same day, once the machine is quiet enough for test runs.

## Inherited [REF] (Epic `feature-delta.md`, by heading — linked, not copied)

- `[REF] DDD vocabulary used in this Epic`
- `[REF] Story Map` (pick-up group G1, dependency graph: S01 ==> S02, S03*, FE2; S01 --> FE6 and the backend renames)
- `[REF] How to pick up a Story` (per-Story checklist)
- `[REF] Cross-Cutting Impact Checklist`
- `[REF] Outcome KPIs + guardrails`
- `[REF] Wave Decisions`, especially D-6264-6 (nine modules), D-6264-9 (DDD made explicit), D-6264-13 (logical
  data ownership only), D-6264-15 (Event Storming with the maintainer)
- `[REF] Risks / Open questions for per-Story DESIGN`, items 7 and 8
- `[REF] Improvement log` (S04a/S05a: dependency ratchets read compiled IL via `IlDependencies`; FE1: Fallow)
- ADR-027, ADR-139, ADR-163, ADR-268, ADR-269; ARCHITECTURE.md §4–§8, §12
- Input from Epic 6455 (deeper hierarchies, DISCOVER + DIVERGE done):
  `../epic-6455-deeper-hierarchies/feature-delta.md` (Current-State Hierarchy Inventory) and
  `../epic-6455-deeper-hierarchies/recommendation.md` (levels above the Feature, per Portfolio). The session
  storms the domain *with* those levels in view, so the context map does not have to be redrawn when 6455 lands.

## DoR re-run (Epic Definition of Ready, for this Story)

| # | Item | Answer |
|---|---|---|
| 1 | Problem stated in domain language | Yes: no agreed folder shape, no typed relations between the nine modules, drifting names. |
| 2 | Persona | `lighthouse-maintainer`; CI build is downstream of the ADR via later ratchets. |
| 3 | Examples | Brief's Elevator Pitch ("may Reports call Metrics?", `Portfolio (code drift: Project)`). |
| 4 | UAT / AC | Brief's five AC, developer-observable (ADR, map, glossary, layout, ARCHITECTURE.md §4). |
| 5 | Dependencies | None. Hard precondition for S02, S03*, FE2. |
| 6 | Size | M, docs-only; carpaccio at this DESIGN. |
| 7 | Technical notes | Event Storming opener (D-6264-15), nw-ddd-architect. |
| 8 | Outcome KPI | Epic KPIs are not moved by a docs-only Story; it enables S02. Stated, not silent. |
| 9 | Cross-cutting | RBAC, Clients, Website, usage data, UI sketches: N/A, because S01 changes no code and nothing a user sees. |

## Baseline re-count (2026-11-01, `main` at 2bb97dc6e)

| Number in brief / Epic | Old | New |
|---|---|---|
| Contexts (modules) | 9 (brief said 8) | 9 in ARCHITECTURE.md §4, to be confirmed or changed by the storming |
| Domain event types in `Models/Events` | 15 | 14 (+ the `IDomainEvent` marker; the 15 counted the marker) |
| `IDomainEventHandler<>` implementations | — | 20 handler classes, 26 event → handler registrations (`Program.cs`); a raw grep finds 23 files |
| Background / hosted services | — | 8 hosted services, plus `UpdateQueueService` and the start-up `OrphanedFeatureCleanupService`; `ForecastUpdater` runs only when triggered |
| Controllers | — | 47 `*Controller.cs` under `API/` |
| `Models` files | 242 (KPI 3: 252 after S04a) | 254 |
| Loose `Services/Interfaces` / `Services/Implementation` files | 42 / 58 | 41 / 61 |
| Highest ADR number (local and `origin/main`) | — | 269; this Story's ADR takes the next free number, checked again right before writing |

## Checklist (ticked through DELIVER)

- [ ] Behaviour-free: docs only; no route, DTO, response, pixel or topology changes.
- [ ] Ratchet: S01 adds no rule itself; every edge the context map calls illegal is listed as a ratchet item for a later Story.
- [ ] Moves separate from edits: N/A, nothing moves (state it at close-out).
- [ ] Re-count before designing: done above.
- [ ] Mutation: N/A, no production code (state it at close-out).
- [ ] ARCHITECTURE.md §4 links the ADR and says "nine bounded contexts" (or the number the session decides), same change.
- [ ] ADR number taken from `origin/main` right before writing.
- [ ] Terminology: glossary uses the seeded default terms from `TerminologySeeder.cs`.
- [ ] Comments/docs: no internal references as explanations.
- [ ] Cross-cutting re-check answered with no silent N/A.
- [ ] No user-facing release notes.

## Wave: DESIGN

### Event Storming (Big Picture), run with the maintainer

- Seed board (from the code, status quo): `event-storming/board-seed.md`.
- Session board and decisions: `event-storming/board.md` (written as we walk it).
