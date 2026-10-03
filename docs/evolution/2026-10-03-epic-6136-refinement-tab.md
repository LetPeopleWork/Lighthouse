# Refinement tab — Epic 6136

**Pushed to `main`, not yet released** · ADO Epic #6136 *"Refinement tab: see the Work Items in refinement"*
(`Community`) · Stories #6139 (slice 01) and #6140 (slice 02), both Resolved · commits `6906eb9df` (DISTILL)
… `c5cb52587` (mutation), on `main` as `fd9df2bca..c5cb52587` · delivered 2026-10-03 in 26 roadmap steps
(01-01 … 01-14, 02-01 … 02-12), every step committed.

Epic 6136 is the first of five Epics (E1–E5) cut from one combined workspace,
`docs/feature/epic-5510-5881-refinement/`. The other four — E2 #5881 (how many more to refine), E3 #5510 (sizing
votes), E4 #6137, E5 #6138 — are not delivered, so the workspace stays where it is and this record covers E1 only.

## What users get

A Team admin opens **Settings → Refinement** and picks which of the Team's To Do and Doing states mean refinement,
as chips with suggestions, the same control as wait states. The Team page gets a **Refinement** tab between Metrics
and Settings:

- **Switched off until a Team has refinement states**, with a tooltip that tells each reader what is missing — an
  admin (or anyone, when roles are not enforced) reads "Choose refinement states in Settings → Refinement", a reader
  reads "A Team admin needs to choose refinement states first". It switches on as soon as the save is accepted.
- **When on, it lists every Work Item in those states** under a counted heading ("63 Work Items in Refinement"), in
  the shared Work Item grid: **Name** (id and name, linked to the work tracking system), **Parent** and **State**.
  The grid opens in the tracker's backlog order; columns can be sorted, filtered and searched like every other grid.
- **An empty refinement is stated, not shown as an error**: "No Work Items in Refinement states right now".
- **Refinement is a configurable term**, so a Team that calls it Replenishment sees that word on the tab, the
  heading, the tooltip and the settings section.
- Demo data: Team Gravity ships with `Backlog`, `Analysing` and `Next` as refinement states; every other demo Team,
  Zenith included, ships without, so both sides of the tab can be shown.

Read-only, free, available to anyone who can read the Team. No CLI or MCP surface yet (that is E2's slice 09).

## What shipped

**Backend**

- `Team.RefinementSettings` — one JSON value on the Team (`States[] { State, Stage }`; only `Waiting` exists as a
  stage today), added by one additive migration per provider (`AddRefinementSettingsToTeams`, via
  `CreateMigration`) with its `HistoricalSchemaPatch` row. Saved through the existing Team settings write; a save
  that does not mention refinement leaves the chosen states as they were, bar any that save takes out of To Do and
  Doing. `GET /teams/{id}` gains `refinementConfigured`.
- `RefinementSettingsValidator` — only states added in a save are judged, and they must be one of the Team's To Do
  or Doing states (case-insensitive). Called on update and, since the review, on create as well.
- **Save-time pruning** (`TeamExtensions.SyncRefinement`) — a chosen state that the same save takes out of To Do and
  Doing is removed from the refinement states; a state chosen twice in different case is stored once, under the
  Team's own spelling.
- `GET /teams/{id}/refinement` (Team read, non-disclosing 404) — `RefinementController` →
  `IRefinementViewQuery` → `RefinementList`, the eighth module. Rows carry `referenceId`, `name`, `url`, `state`
  and `parentReferenceId`. Backlog order is the tracker's rank compared through `FeatureComparer.CompareOrderValues`
  (numbers as numbers), ties by id. States are matched in memory, case-insensitively, because the database
  compares text by case.
- `RefinementModuleArchUnitTest` — nothing but the API and the composition root depends on the module; the module
  reaches neither the trackers nor the background updates; reading the tab calls no writing repository member; the
  validator is static and depends on no service. `FeatureOrderingSingleSourceArchUnitTest` names `RefinementList`
  as the one other type allowed to use the rank comparer (ADR-134 amended to match).
- Usage data: `TeamRefinementConfigured = 12` (name only) and route key `TeamDetail_Refinement = 10` on
  `TeamTabOpened`, both listed in `docs/settings/usagedata.md`.
- Terminology: `refinement` / `refinements`, seeded `Refinement` / `Refinements`.
- Demo: `DemoDataFactory` configures Team Gravity, no other Team.

**Frontend**

- `RefinementSettingsSection` in the Team settings form — `ItemListManager` chips, suggestions are the Team's To Do
  and Doing states, free text only counts when it names one of them. When the admin takes a state out of To Do or
  Doing, the chip leaves in the same save. Opening the form saves nothing.
- The Refinement tab in `TeamDetail` with the role-specific tooltip, the address `/teams/:id/refinement` (an
  unconfigured Team's address lands on Forecasts), and a switch-on that follows the accepted save.
- `RefinementView` — heading plus `DataGridBase` with the shared name column (`createNameColumn`, now generic over
  Features and Work Items) and the shared parent lookup (`useParentWorkItems`, widened the same way). A failed read
  goes to the error snackbar; a late answer for a Team the page has already left is dropped.
- `useRefinementSetUpReporter` — shared by the Team page and the edit-Team page. Reports `TeamRefinementConfigured`
  once, when an accepted save gives refinement states to a Team the server last said had none. It remembers what
  the server last said in a ref, so a queued save cannot report twice; a cloned Team does not count as setting
  refinement up.
- E2E: `specs/teams/Refinement.spec.ts` walking skeleton through `TeamDetailPage` / `TeamRefinementPage` on demo
  data — Gravity lists GR-051 first, Zenith's tab is switched off.

Architecture: [ADR-214](../product/architecture/adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md)
(built for the refinement states, amended on pruning),
[ADR-134 addendum](../product/architecture/adr-134-ordering-policy-appsetting-enum-single-selection-point.md),
`brief.md` → "Application Architecture — epic-5510-5881-refinement" → "Built so far", `ARCHITECTURE.md` §4 module 8.

## The maintainer's review — decisions and reversals

The maintainer reviewed the running app after each slice. Every change below was added to the roadmap as its own
step (01-12, 01-13, 01-14, 02-10, 02-11, 02-12) and built test-first.

- **Chips, not checkboxes** (after slice 01). The first build offered a checkbox list labelled "Backlog (To Do)".
  States are now picked like wait states, with the existing chips control. The category labels went with the
  checkboxes.
- **Auto-remove instead of keep-and-flag** (after slice 01). DISCUSS (AC-1.4) and DISTILL (DST-2, DST-3) had a
  chosen state that stops being To Do or Doing *kept and flagged* in the section, its Work Items not listed. The
  maintainer reversed it: the state simply leaves the refinement states, in the save that takes it out. The
  `isMapped` flag left the wire; the "flagged state is not listed" scenario became moot.
- **The Doing note is gone** (after slice 02). "Already counts in WIP and Cycle Time" next to Doing states was
  removed.
- **The shared grid, with Name, Parent and State** (after slice 02). The first list was a plain MUI table with
  state, category and Work Item Age. It is now the shared Work Item grid, and **age and category were removed from
  the API as well as the table** — MQ-4 (age on To Do rows) no longer has anything to decide. Vote columns come
  later, with E3.
- **Sorting is allowed**, with backlog order as the default.
- **A new standing rule in `CLAUDE.md`: sketch any UI before building it, and ask** — an ASCII mock of layout,
  columns, controls and copy, approved before the step starts, in AFK runs too.

## Decisions taken autonomously

- **DISTILL DST-1 … DST-14** (`distill/wave-decisions.md`): the wire shape; a Team without refinement states answers
  its tab with `refinementConfigured: false` and no rows rather than an error (DST-4); an address naming the tab of
  an unconfigured Team lands on Forecasts (DST-5); the set-up event is reported by the page that sees the save
  (DST-6); the 300-row budget split by layer — under 2 s for the backend read, a heading that counts all 300 in the
  browser (DST-8); the E2E on demo scenario 12 with GR-051 first (DST-9); the copy pinned by the scenarios (DST-10).
  DST-2 and DST-3 were later reversed by the maintainer (above).
- **Only a state's mapped name can be chosen.** A tracker state gathered under a state mapping is not offered on its
  own; Work Items held under any of the gathered states are still listed.
- **The list trusts the stored states.** Once a save prunes them, `RefinementList`'s own To Do / Doing filter could
  no longer change an answer and was removed (`1b143699f`), after checking no write path bypasses the pruning.
- **A cloned Team does not count as setting refinement up**, so cloning never reports `TeamRefinementConfigured`.
- **Set-up detection lives in one shared hook**, keyed on what the server last said rather than on a re-read, so
  both settings pages report it and a queued save cannot double-report.
- **A failed read after an accepted save keeps the Team on screen** instead of tearing the page down as "no access".

## Lessons

- **The UI was built three times because nothing was sketched.** Checkboxes became chips; a table with category and
  age became the shared grid with Name, Parent and State; a note was added and removed. Each was a correct build of
  what the scenarios pinned, and none of it was what the maintainer wanted to see. The sketch-first rule now sits in
  `CLAUDE.md`.
- **A DISTILL-written E2E had never run.** The walking skeleton was type-checked in DISTILL but not run, and its
  first live run failed: the spec did not take the `testData` fixture that demo-data specs need. Fixed in 02-09.
- **The database matches text by case; the rest of the product does not.** The adversarial review found the list's
  `IN` filter missing Work Items whose tracker state differed in case from the chosen state. Matching moved into
  memory, with the same case-insensitive set the settings use.
- **An agent commit carried a stale message.** The case-matching fix was committed under a message describing an
  earlier attempt. It was reworded before push (tree identical) — a commit message from an agent is
  worth reading against its diff before it is pushed.
- **Dependencies that "could not matter" did, again.** Mutation testing found the set-up reporter holding the
  usage-data reporter it opened with — which drops the event when consent arrives after the page opened — and the
  tab letting a previous Team's late answer overwrite the current one. Both were real, both are pinned now.

## Quality

| Gate | Result |
|---|---|
| Backend suite (connector categories excluded) | 7780 passed, 0 failed, 1 skipped after the review fixes (`1e4cb29e3`) |
| Frontend suite | 6039 passed, 0 failed after the review fixes; `pnpm build` and Biome clean |
| E2E walking skeleton | green live, three runs (02-12) |
| Adversarial review (Opus) | **needs_revision** — 1 high, 3 medium, 4 low. All 8 fixed in `37b929620..1e4cb29e3`, each with a test shown failing first: case-sensitive state match (high); set-up event could fire twice from a queued save; the edit-Team page never reported; a failed re-read after a save tore down the page; opening the form autosaved a pruned list; a state chosen twice in different case; create skipped the validator; weak waits in the negative usage-data tests |
| Mutation, gate 80 % ([ledger](../feature/epic-5510-5881-refinement/mutation/results.md)) | Backend **90.00 %** on the feature's code (36 / 40; 70.76 % counting two pre-existing files mutated whole). Frontend **91.41 %** (117 / 128). Every remaining survivor is equivalent or presentation, with its reason in the ledger |
| CI | Running on `c5cb52587` at the time of writing; `/clean-ci` is the orchestrator's |

## DELIVER checklist

| Item | Answer |
|---|---|
| User-facing docs prose | **Done** in `9b2602cf0`, alongside this finalize: `docs/teams/edit.md` (picking refinement states, auto-remove), `docs/teams/detail.md` (the tab), `docs/concepts/concepts.md` (a Refinement section), `docs/settings/configuration.md` (the two Terminology terms) |
| Per-feature screenshots | **Deferred by the maintainer** — owed one `@screenshot` per theme for the tab and the settings section, once the next Epic has changed the tab again |
| Demo data | **Done** — Team Gravity configured (`Backlog`, `Analysing`, `Next`), Zenith and every other demo Team not; guarded by `DemoDataFactoryTest` |
| Website assets | **Deferred by the maintainer, and N/A for E1 on its own**: DISCUSS put the website change (letpeople.work/sizing-poker) at the finalize of E2 and E3; E1 changes no asset the website hot-links |
| Usage-data event in `docs/settings/usagedata.md` | **Done** — `TeamRefinementConfigured` row and `/teams/:id/refinement` in the address list |
| Lighthouse-Clients CLI / MCP | **N/A for E1** — no client surface; the first one is E2's slice 09 |

## Still open, knowingly

- **Screenshots** — owed, deferred by the maintainer until the next Epic has changed the tab (above).
- **The quick-settings save path** (`updateTeamSettings`) still re-reads the Team with the destructive post-save
  `fetchTeam`, which a failed read turns into a torn-down page. Pre-existing, outside this Epic; the main settings
  paths no longer do it.
- **`TeamDetail` tab-gating lines were not mutated** — they sit in a large pre-existing page; the 15
  `TeamDetail.refinementTab` cases cover them by behaviour.
- **Traceability tag comments in the scenario files cite internal ids** (`// @us-01 @slice-01
  @kpi-OUT-5510-K1-…` above each scenario in the backend `Slice01`/`Slice02` scenario files and the frontend Refinement
  specs). They follow the DISTILL tagging convention but read as pointers to archived documents, which the comment
  rule forbids. Left as they are; decide project-wide whether tag comments are exempt.
- **DST-9: demo order puts GR-051 first**, with 63 Work Items on Team Gravity, where US-02's illustration said GR-058
  first and 14. If GR-058 first matters for the screenshots, reorder the demo CSV then.
- **KPI baselines** (`OUT-5510-K1`, `OUT-5510-K2`) are pending the first release carrying E1.

Delivery history: `docs/feature/epic-5510-5881-refinement/` (shared with E2–E5).
