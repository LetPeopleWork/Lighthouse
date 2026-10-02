# Wave Decisions — epic-5878-baseline / DISCOVER

**Agent**: Scout (`nw-product-discoverer`) · **Date**: 2026-10-02 · **Mode**: subagent, autonomous. The
maintainer was unavailable, so questions are recorded rather than asked.
**Predecessor**: none. **Successor**: DIVERGE (`nw-diverger`). The full narrative is in `../feature-delta.md`.

## Decisions

- **[D1] One workspace, `epic-5878-baseline`.** Rationale: there is a single Epic and no sibling to merge. Source: maintainer dispatch, 2026-10-02.
- **[D2] Proceed to DIVERGE with G1 to G3 failing, as a maintainer-accepted risk.** Rationale: the solution shape is wide open (11 questions), and DIVERGE costs little compared with DESIGN or DELIVER. The interview protocol runs alongside. Source: maintainer decision, 2026-10-02.
- **[D3] ICE Confidence 1.5 → 2.0; ICE 54 → 72.**
  - The Thrivve demo (2026-08-31) predates the 1.5 score set on 2026-09-20, so it is already counted.
  - The only new evidence is consultant #2 saying he "would really like it", after being pitched. That is future intent, and the Mom Test order (problem first) was not followed.
  - His real, present engagement context earns +0.5. Anything more would be inflation.
  - Ease stays at 6 for computation; it would be 5 if a stored baseline is chosen (D5).
  - Source: maintainer evidence, 2026-10-02; #5878 ICE note.
- **[D4] Consultant #2's case is a shape divergence, not a confirmation.** Rationale: a 4-week assessment has almost no "after" window. With a 90-day rolling comparison, 62 of 90 days (about 69%) predate the intervention. His value is more likely a retroactive snapshot, or a baseline frozen at the end of the engagement. Source: maintainer evidence, 2026-10-02.
- **[D5] The Ease claim is partly verified.**
  - Computation rides existing services: `(start, end)` windows on `TeamMetricsService` and `PortfolioMetricsService`, PBC `asOf`, and the Story 6053 reconstruction.
  - "No migration" holds only if nothing is frozen. Freezing needs storage, which means an expand-only migration.
  - Source: codebase check, 2026-10-02.
- **[D6] The prospect counter-signal (signals and email reports, not baseline) stays on record.** Rationale: it is one evaluating buyer, which is not a refutation, and "email reports" may be the channel to management. Source: #5878 ICE note.
- **[D7] The "2 of 3 would pay" test is replaced by a commitment test.** Rationale: "would you pay" asks for future intent, so the protocol counts actions instead (use on a live engagement, show the artefact, give an introduction). Source: Mom Test, nw-interviewing-techniques.

## Constraints (with evidence)

| # | Constraint | Evidence |
|---|---|---|
| C1 | Community tier | Maintainer 2026-09-20 |
| C2 | Configurable terms only. Never Epic/Story in copy. | `TerminologySeeder.cs` keys: team, portfolio, delivery, feature, workItem, cycleTime, throughput, wip, workItemAge, sle |
| C3 | Name collision with the existing PBC Baseline | `FlowMetricsConfigurationComponent.tsx:666` "Set Baseline for Process Behaviour Chart" |
| C4 | No synthesised values | ADR-109 as narrowed by Story 6053 |
| C5 | Recomputation reflects today's configuration and item set | Story 6053 journey, accepted residual risk |
| C6 | The window must lie inside `DoneItemsCutoffDays` (default 365) | `Team.cs:23`, `Portfolio.cs:35`, `BaselineValidationService.Validate` |
| C7 | Expand-only migration through `CreateMigration` if anything is stored | Project rules |
| C8 | DEVOPS designs the usage event | Project rule |

## Validated assumptions

| # | Assumption | Confidence |
|---|---|---|
| V1 | Consultants need before/after flow evidence to prove value (Thrivve built and use it; second-hand) | Medium for consultants, Low for transfer to Lighthouse users |
| V2 | The whole field set can be computed for any past window from stored Work Items | High |
| V3 | A pinned "before" window already exists as a product concept (PBC Baseline) | High as precedent, none as demand |
| V4 | A frozen baseline is not offered by ActionableAgile or Nave | Medium (absence of evidence) |
| V5 | A retroactive baseline works from day one | High |

## Invalidated assumptions (with evidence)

| # | Assumption | Evidence |
|---|---|---|
| X1 | "No migration" | Freezing requires stored values or a stored definition; the only existing baseline storage is the PBC pair of columns |
| X2 | "Recomputed = frozen" | Story 6053: changes to configuration or the item set silently change past values |
| X3 | "Available for the whole engagement" | A 365-day cutoff means a 90-day pre-start baseline becomes uncomputable about 275 days after the start |
| X4 | "Before/after fits every consultant" (challenged) | The 4-week assessment (D4) |
| X5 | "2 of 3 would pay → Confidence 5" as a test | It asks for future intent (D7) |

## Gate status

G1 FAIL (0 interviews) · G2 FAIL (no opportunity scores) · G3 FAIL (nothing tested) · G4 PARTIAL
(computation feasible, viability chain unvalidated). Proceeding is a maintainer-accepted risk (D2).

## Open for the maintainer

1. Run the interview protocol in `feature-delta.md`, starting with consultant #2 (what he will hand over at the end of his 4 weeks) and Thrivve (what happened after management saw the baseline).
2. Ask whether Thrivve will share how often engagements outlast about 9 months. This bears on X3.
