# Feature: story-6602-refinement-and-report-window-at-the-port

Story #6602 · Move the remaining Refinement and report-window acceptance classes to the service port · tagged
`Continuous Improvement`, **no parent Epic** (a follow-up of T5 #6469, whose Epic #6506 is Closed). Catalogue:
[Epic 6264 workspace](../epic-6264-modular-monolith/feature-delta.md). Plan and measurements:
[T5 workspace](../story-6469-t5-acceptance-at-the-service-port/feature-delta.md), the "Slice E" sections.

DISCUSS: covered by T5. The scope comes from the ADO description and from the maintainer's decision at T5's slice E
checkpoint (2026-11-08). Waves start at DESIGN. Size M.

Order (maintainer, 2026-11-12): **#6602 runs before T10 #6537.** T10's "before" set is taken after this Story's last move
push, so no class moves twice and T10's time limits are measured on the final suite.

## Wave: DISCUSS / [REF] Inherited

Link, do not copy. From the T5 workspace:
- "Slice E note: Refinement (the addendum DESIGN owes)": the design this Story starts from.
- "Slice E checkpoint: measured, Stage 3 re-run, and the question": `c`, `h`, Stage 3 for E and F, the 14.1 s line, `P2`.
- "Maintainer decision at the slice E checkpoint": settings come from the `IAppSettingService` stand-in, not the real
  seeders; E13 checks the stand-in's answer against a seeded database.
- "Slice E review": the guard gaps this Story closes as its classes move (E10, E13, `PortGivensGuardTest`, `ScriptedForecasts`).
From the catalogue: [REF] How to pick up a Story, Outcome KPIs + guardrails, Risks.
Also: `.claude/skills/test-writing/SKILL.md` and `references/backend.md`; ADR-237 (per-tier limits).

## Wave: DISCUSS / [REF] DoR re-run

| Item | Answer |
|---|---|
| Problem stated | Host-based acceptance classes cost about 17 s summed in a full run, where a port test proves the same rules |
| Persona / job | infrastructure-only (D-6264-2) |
| Scope bounded | 3 Refinement classes and `ReportWindowBoundaryScenarios`, plus the wire rows E1, E3–E7 and E11 as their classes move |
| Acceptance criteria | Rows equal before and after (id list); `HostUsersStillToMove` loses the 4 entries; summed time falls by the line |
| Dependencies | T5's harness (`RefinementAtThePortTest`, `RefinementPortGivens`, `TestHostUseGuardTest`) is on `main`. None open |
| Sized | M; carpaccio at DESIGN, at most 5 slices |
| Behaviour-free | Yes, test structure only |
| Cross-cutting | RBAC, Clients, website, usage data, UI sketch, Terminology, context map: N/A, because no production code or user-visible behaviour changes |
| Open for DESIGN | Slice order (E before F); F's `c` measured on its own set-up, not projected from E |

## Wave: DISCUSS / [REF] Baseline re-count (2026-11-12, `e8273cf51`)

| Number | Brief / T5 (2026-11-08) | Today |
|---|---|---|
| Refinement rows to move (`Slice05`, `Slice10`, `Slice12`) | 49 | 41 test methods (11 / 10 / 20) and 15 `[TestCase]` lines; the row count is re-read from a TRX at DESIGN |
| `ReportWindowBoundaryScenarios` rows | 36 (from `TheRowsOfTheBoundaryTable`) | one `TestCaseSource` method; re-read from a TRX at DESIGN |
| `HostUsersStillToMove` entries | not stated | 329 lines; the 4 classes are on it (lines 165, 169, 171, 186) |
| Estimated saving (E + F, no seeders) | about 17.4 s against a 14.1 s line | unchanged until DESIGN re-measures |
| Changes to these files since 2026-11-08 | — | one: `a28c90685` (Flow metrics contract move), no change to the classes' rows |

Class names in the guard list: `Slice05NeedAndVerdictTest`, `Slice10SleYardstickTest`, `Slice12CommentsTest`
(`Refinement/`), `ReportWindowBoundaryScenarios` (`Reports/`).

## Wave: DISCUSS / [REF] Checklist

- [ ] Behaviour-free: no production code changes; existing API, E2E and response tests green unmodified, apart from the moved tests
- [ ] Every removed host assertion has a row at the port or in a wire test; the id list before and after is compared
- [ ] Ratchet in the same change: each moved class leaves `HostUsersStillToMove`; the list is never widened
- [ ] The guard gaps from T5's slice E review are closed for the classes that move
- [ ] Moves separate from edits (`refactor(test-infra): move …` and a separate edit commit)
- [ ] Re-count before designing (done above; DESIGN re-reads rows from a TRX)
- [ ] Saving measured, as T5 measured it: each class alone and in a full run, before and after
- [ ] Mutation: N/A for production code, because none changes; stated at close-out
- [ ] Architecture docs: ADR-237 or `ARCHITECTURE.md` only where a decision changes
- [ ] One oracle file per nwdes slice (lesson from S02 and S05f)
- [ ] No release notes (behaviour-free)
- [ ] T10 #6537 told when the last move push lands, so its before set starts after it
