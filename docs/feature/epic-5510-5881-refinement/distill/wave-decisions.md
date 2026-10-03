# Wave Decisions — DISTILL — epic-5510-5881-refinement (Epic #6136, E1 only)

**Agent**: Quinn (`nw-acceptance-designer`) · **Date**: 2026-10-03 · **Mode**: autonomous subagent, maintainer AFK
**Scope**: E1 — slice 01 (US-01, ADO #6139) and slice 02 (US-02, ADO #6140). E2–E5 are not distilled here.
**Predecessors**: DISCUSS (DD-1..DD-22), DESIGN (DSN-1..DSN-22, ADR-214..218), DEVOPS (DVO-1..DVO-8).
Full reasoning: `feature-delta.md` → `## Wave: DISTILL / …`. RED evidence: `distill/red-classification.md`.

## Phase 0

`[lang-mode] csharp+typescript` (backend `*.csproj`, frontend `package.json` + `tsconfig.json`, E2E Playwright).
`[policy-mode] inherit` — `docs/architecture/atdd-infrastructure-policy.md` read and applied; no port in scope was
missing, so no row was appended (the one fake added, `ILighthouseClock` → `FakeLighthouseClock`, is already a row).
`[port-mode] n/a` — the policy records that the Python state-delta port does not apply to this C#/TS project; the
universe guard is asserted directly (Work Item row count, settings read-back unchanged, a second read identical).

## Reconciliation gate (DISCUSS ↔ DESIGN ↔ DEVOPS)

**Reconciliation passed — 0 contradictions.** Three DISCUSS statements are superseded by DESIGN, and each is a
documented, reasoned correction in `design/upstream-changes.md`, not a conflict; the scenarios apply DESIGN's reading:

| DISCUSS | DESIGN / DEVOPS reading applied | Source |
|---|---|---|
| AC-2.1 "same backlog order the Team's forecasts use" | the tracker's rank via `FeatureComparer.CompareOrderValues`, ties by id | DSN-6 |
| AC-2.2 "… category, age" on every row | Work Item Age on Doing rows only (MQ-4 default) | DSN-6 note |
| Slice 01 OUT "Terminology key (02)" | `refinement`/`refinements` land in slice 01 | DSN-20 |
| DD-16 tab-open reuses `TeamTabOpened` | + route key `TeamDetail_Refinement`; `TeamRefinementConfigured` name-only, slice 01 | DVO-1 |

Settled maintainer calls re-checked: the word is **Refinement** (renameable); everything in slices 01–02 is
**Community** (no licence gate in any scenario — the premium demo scenario is only where Team Gravity lives);
votes, need number and pull-only async belong to later slices and nothing here contradicts them; the usage-data
events DEVOPS assigned to slices 01–02 each have scenarios.

## Decisions taken autonomously (DST-n)

| # | Decision | Why this default |
|---|---|---|
| DST-1 | **Wire shape pinned by the scenarios.** Write: `refinement: { states: [{ state }] }` on `PUT /teams/{id}`; absent or `null` = unchanged; `[]` = none chosen. Read (`GET …/settings`, PUT answer): each entry also carries `isMapped`. `GET /teams/{id}` gains `refinementConfigured`. `GET /teams/{id}/refinement` → `{ refinementConfigured, workItems: [{ referenceId, name, url, state, stateCategory: "ToDo"\|"Doing", workItemAge: number\|null }] }` | DESIGN names the DTOs and fields but not their JSON; these follow the existing camelCase DTO shape, string enums out, and DSN-5/DSN-9 wording |
| DST-2 | **A flagged (no longer mapped) state survives being sent back.** Only states *added* in a save must be To Do or Doing; a state already stored and now unmapped is kept and stays flagged when the autosaving form re-sends it | Reconciles DSN-4's "kept and flagged, never dropped" with "⊆ To Do ∪ Doing at save" under ADR-029 autosave, which re-sends every section: refusing would block every later edit to the Team |
| DST-3 | **Work Items in a flagged state are not listed** | The section's own flag promises "its Work Items cannot appear" (US-01 example 3) |
| DST-4 | **A Team with no refinement states answers its tab's read with `refinementConfigured: false` and no rows (200)**, not 404/409 | Mirrors the "stated, not an error" rule of the empty state; 404 stays reserved for "you may not read this Team" (non-disclosing, TeamRead) and "no such Team" |
| DST-5 | **An address naming the Refinement tab of a Team without refinement states lands on Forecasts** | Same rule the page already applies to the Features tab of a Team without Features (S1 pattern) |
| DST-6 | **`TeamRefinementConfigured` is reported by the Team page**, where the settings form is handed its save, when a save it accepted turns `refinementConfigured` false → true; never on later saves, never on a refused one. The scenarios stand in for the form and call that save | DVO-1 defines *when*; the page is the one place that sees both the save and the Team read that follows it. Consent is the reporter's, pinned once for every event by the existing reporter tests |
| DST-7 | **The terminology keys are not added to `TERMINOLOGY_KEYS` in DISTILL**; tests spell `"refinement"`/`"refinements"` | The existing guard `TerminologyContext.test.tsx` requires the seeder to carry every listed key — adding the key without the seeder row reds the suite. DELIVER adds key, seeder row and fallback together |
| DST-8 | **300-row guardrail split by layer**: the backend read alone answers in < 2 s (second, warm call, 300 rows in rank order); the frontend asserts the heading counts all 300 and the list starts at the top, not a DOM row count | Leaves DELIVER free to virtualise the list; a jsdom render time is not a budget anyone can hold |
| DST-9 | **E2E walking skeleton on demo scenario 12** (premium; it is the scenario that seeds Team Gravity, beside Team Zenith). Gravity configured (Backlog, Analysing, Next); **Zenith is the unconfigured demo Team** (US-01 example 2). First row asserted is **GR-051**, not GR-058 | Under DSN-6 the CSV rank is file order, and GR-051 is Gravity's first Backlog/Analysing/Next row; the "GR-058 first" in US-02 predates DSN-6. Tooltip asserted is the editor copy, because E2E runs without sign-in where everybody edits |
| DST-10 | **Copy pinned by the scenarios** (from DISCUSS where it gives words, else the plainest sentence): tooltips verbatim from DD-15; heading `"{n} {Work Items} in {Refinement}"` (singular for 1); empty state `"No {Work Items} in {Refinement} states right now"`; option labels `"Backlog (To Do)"`; Doing note `"already counts in {WIP} and {Cycle Time}"`; flag `"{state} is no longer mapped; its {Work Items} cannot appear"`; age `"4 days"`; row category `"To Do"` / `"Doing"`. Renamed-term assertions are case-insensitive mid-sentence | DELIVER may reword only by changing the scenario in the same commit |
| DST-11 | **No backend scaffolds**: the backend scenarios are black-box over HTTP/JSON, so nothing they reference is missing at compile time. Frontend scaffolds: `RefinementService` and `RefinementView` throw `Not yet implemented -- RED scaffold`; model types and `ITeamSettings.refinement` / `IApiServiceContext.refinementService` added. **No EF migration** (DELIVER, `CreateMigration`) | Precedent 22e43e1f9 (backend, no scaffolds) and c3820c0d3 (frontend scaffolds that throw) |
| DST-12 | **Tier B not declared** | Config-shaped settings + one read; two short journeys, no domain-rich generated input |
| DST-13 | **MQ-4 default followed** (To Do rows carry no age). Still open with the maintainer; reversing it changes one scenario | DESIGN: "none blocks DISTILL" |

## Mandate-12 (informational)

Step methods live in `*Specifications.cs` partial classes and the shared `RefinementAcceptanceTest` harness; every
scenario body is Given/When/Then calls only. Domain values are typed (`TeamUnderTest`, `TrackerWorkItem`,
`RefinementRowReading`, `RefinementStateReading`, `StateCategories`). **Step-reuse ratio 2.24×** (103 step calls /
46 distinct steps across the two backend scenario files) — the natural ceiling for a settings-plus-list feature.

## Completeness audit (Phase 2.5)

**11 / 15 → ACCEPTABLE_WITH_DOCUMENTED_GAPS.** Passing: C1a (no states, empty list, cleared), C1b (equal and
9-vs-10 ranks, 1 vs N heading, 300 rows), C2a (state documented in the slice docstrings: none → chosen → flagged →
cleared), C2b (illegal event per state: open-by-address unconfigured, newly chosen unmapped, read without a role),
C3 (0/1/many states and rows), C4a (save again flagged, re-save reports nothing, read twice identical), C5a
(admin / reader / RBAC off / no role / system admin), C5b (renamed term changes words only), C6a (Done, Icebox, unknown
Team), C6b (400 naming the state, 403, 404 ×2), C7c (settings write is concurrency-safe by the existing token, pinned by
`TeamConcurrencyTokenIntegrationTest`; nothing new to race). Gaps, all `AT_GAP_IN_DELIVERY_SCOPE`, none blocking:
C4b (clearing on a Team that never had states), C6c (closed error set), C7a (the tab's read failing — the page's
existing error snackbar is assumed), C7b (interruption mid-save — inherited autosave behaviour). Also not pinned
at acceptance level: the "survives a Team refresh" half of AC-1.2 — a refresh reads Work Items through the
connector and never writes Team settings, so DELIVER covers it with a unit test on the refresh path if it touches
the Team row. **0 SPECIFICATION_AMBIGUITY.**

## Upstream notes (for the maintainer, not blockers)

- US-02's example "GR-058 first" and "14 Work Items" are illustrative: on the demo data under DSN-6 Gravity lists
  **63** Work Items with **GR-051** first. If GR-058 first matters for screenshots, DELIVER reorders the demo CSV.
- MQ-4 (age on To Do rows) remains open; the default is what the scenarios pin.

## Handoff

DELIVER slice 01 enables `Slice01RefinementStatesTest`, the slice-01 cases of `TeamDetail.refinementTab.test.tsx`,
`ModifyTeamSettings.refinement.test.tsx` and the slice-01 cases of `TeamRefinementUsageEventsTests`; slice 02 the
rest, then the E2E skeleton (run live before un-fixme'ing it). One scenario at a time.

## Review follow-up

- **DST-14** The E2E skeleton runs on premium demo scenario 12 because it is the only scenario that seeds Team
  Gravity; nothing in E1 is licence-gated. DELIVER slice 01 adds a backend demo-data scenario so AC-2.6 is guarded
  before the E2E is un-fixme'd.
- **Maintainer to confirm** (product-facing defaults, not blockers): DST-9 (GR-051 first, 63 Work Items on demo
  Gravity), DST-10 (the copy pinned by the scenarios) and DST-13 (no age on To Do rows, MQ-4).
