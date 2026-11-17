# Story 6362 — S03d Package Promises (Deliveries): the fifth context in its own module

**Finished:** 2026-11-17 · ADO User Story #6362 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Promises (today's Deliveries) was spread over `Services/Implementation`, `Services/{Interfaces,Implementation}/DeliverySources`,
`Services/Implementation/DomainEvents` and one file in `Models`. No rule named it. The only "Portfolio/Delivery" rule
pattern matched the update code, not the Deliveries. Two edges were left over from Story 6359: the Delivery services
were listed by name in Tracker integration's inbound list, and the Jira connector reached Promises' forecast block
renderer, the one entry of Tracker integration's leaf rule. Work Items' inbound list also named `DeliveryRuleService`.
The brief still put the update code, the write-back round and Feature ordering and ranking in scope, although ADR-270
gave them to other owners.

## What changed

Three nwdes features on `main`, behaviour-free apart from one dependency inversion, no schema change. Design:
`docs/feature/story-6362-s03d-package-promises-deliveries/`.

- **Feature A, rules first** (`docs/feature/story-6362-s03d-a-rules-first/`, 3 slices). No production code changed.
  - A1: every architecture rule that names a Promises type, namespace or folder also sees `Modules/Promises`, each
    proven with a made-up type (`PromisesRulesSeeTheModuleArchUnitTest`).
  - A2: the membership was measured from the compiled code and fixed in `PromisesMembershipArchUnitTest`: 15 types in
    13 files, and 2 admitted `Models` types (`DeliveryForecastBlock`, `DeliveryForecastBlockPercentile`).
  - A3: the new examine command `deliveries-answers-compare` compares the Delivery answers of two revisions (the
    Delivery list, the rules schema, the sources and each Delivery's metrics history, per demo scenario). Its compare
    stage takes capture folders, and its fixture self-test became the probe `deliveries-answers-selftest`. A3 also fixed
    the shared compare stage, which lost its `VERDICT: DIFFERENT` line under `set -euo pipefail`.
  - A4 (CI selection) was dropped: `test-selection-show` showed the selection already right, so it had no RED.
- **Feature B, the move** (`docs/feature/story-6362-s03d-b-move/`, 2 slices). 14 files moved by `git mv` with
  namespace lines only, then separate edit commits. The unit tests moved with their production code.
  - B1 `Domain` (1): the forecast block. `Application` (11): the rule service, the metric-values projector, the forecast
    block renderer, forecast publishing, the source resolver with its preview, the source sync, and their interfaces.
  - B2 `Application/Policies` (2): the snapshot-recording and forecast-publishing handlers. A new entry in
    `DomainEventHandlerOrderPinTest` fixes their order: the Delivery snapshot is recorded before the forecasts are
    published.
- **Feature C, edges and rules** (`docs/feature/story-6362-s03d-c-edges-and-rules/`, 2 slices).
  - C1: `PromisesModuleBoundariesArchUnitTest`, on the shared `ModuleRules`, with the lists measured from the compiled
    code (R-P1 to R-P7, in ADR-270). Work Items' and Tracker integration's inbound lists lost their 3 Promises entries:
    Promises' `Application` is a legal caller by namespace there (its `Domain` and `Policies` are not).
  - C2: the Jira connector → Promises edge is inverted. Tracker integration owns a new port, `IDescriptionBlockMerger`;
    Promises' renderer implements it and is registered twice, once per interface; the Jira connector takes the port.
    The text written to Jira is unchanged. CI passed with every live suite on that push.
    `TrackerIntegrationReachesNoPromisesArchUnitTest` keeps Tracker integration's leaf rule empty.
  - C3 was dropped as a slice (no RED: feature B had already placed every file). Its check stays as a guard,
    `PromisesUnitTestPlacementTest`, with 22 named exceptions: 5 Delivery repository files (platform persistence), 8
    write-back round files (#6657) and 9 ordering and ranking files (Organisation set-up, S03i).
- **Docs, at finalize:** ADR-270 (the "Moved so far" note, Decision 1 Promises row, "Every edge" rows for Promises ↔
  Tracker integration and Forecasting → Promises, the new block "Edges measured by packaging Promises", a "Done" note on
  the Tracker integration block, ratchet candidates 3 and 4, the Tracker integration glossary term "Description block
  merge", the Promises glossary Delivery row, Decision 6, and "Left open" on Feature ranking, write-back, the update
  queue and `ManualRank`); `ARCHITECTURE.md` §4 (rows 1 and 4, the diagram node and a new Promises → Tracker
  integration arrow, the diagram note, the leaf-rule bullet, the Work Items bullet, a new Promises module-rules bullet,
  and the handler order pin); `docs/product/architecture/brief.md` (new section "story-6362 s03d package promises" and
  the module table row); the Epic catalogue (`docs/feature/epic-6264-modular-monolith/feature-delta.md`: the S03d
  table row, the S03\* notes, KPIs 1 to 3, the Improvement log); the slice-06 brief (two criteria ticked and corrected,
  the IN list corrected, notes for S05d, S03i, #6657 and Epic 6474). `.claude/commands/release.md` needs no change: it
  names no Promises path.

## Key commits

Range: `git log --oneline --grep="6362\|(promises)\|(tracker-integration)" f68f65cf1~1..HEAD`.

| Commit | What |
|---|---|
| `f68f65cf1` | DESIGN and DEVOPS, proposed in AFK mode |
| `29b301115`, `93af00a1c` | start-of-DELIVER confirmations; the write-back placement filed as #6657 |
| `032c5aa9d`, `92452e472` | A1: the architecture rules see `Modules/Promises` |
| `e36f3f5b1` | the old connector namespaces retired from two S03a rule tests |
| `963920687`, `324d0b20d` | A2: the Promises membership, measured from the compiled code |
| `3a0eccc7c`, `cefb7a58f` | A3: the Delivery-answers examine command |
| `2f768fede` | A4 dropped (no RED) |
| `90426020c` | feature A's EXAMINE accepted |
| `c96471c9d`, `e258a49d8`, `2ac54363c`, `098275ee2` | B1: the services and the forecast block into `Modules/Promises` |
| `3fb6387da`, `5bc949a9a`, `8668073a3`, `7a5373e06` | B2: the forecasts-updated handlers into `Application/Policies`; the order pin |
| `4f44420d9` .. `9e2638703` | feature B's EXAMINE, four rounds; E5 and E7 reworded by the maintainer's choice |
| `2cf149907`, `03639b027` | C1: the Promises module rules; Promises legal in the Work Items and Tracker integration lists |
| `9f9689eba`, `2a5ecc5b5` | C2: the Jira connector merges the forecast block through `IDescriptionBlockMerger` |
| `f39c2e0dc` | C3's check kept as a guard: every Promises unit test stays in the Promises test folders |
| `d5b1b8feb`, `dd8d8f94d` | feature C's whole-candidate review and EXAMINE accepted |

## What it achieved

- **Behaviour unchanged.** Delivery answers: 23 compared over 9 demo scenarios, 0 different
  (`deliveries-answers-compare`, against the pre-move base). Forecast answers: 128 compared, 0 different. Refresh
  reactions: the 3 observable reactions identical. No EF model change on SQLite or Postgres (`ef-model-unchanged`). The
  full backend suite: 14,138 run, 14,126 passed, 12 skipped, 0 failed. CI passed with every live suite on the push of C2.
- **Contexts packaged with namespace rules:** 4 → 5 of 11. Files under `Services/Interfaces` + `Services/Implementation`
  332 → 320 (110 + 210). `Models` files 263 → 262.
- **Tracker integration is a true leaf.** Its "reaches no other context" list is empty, and its Application inbound
  list is down from 7 to 5. Work Items' inbound list is down from 7 to 6.
- **Three "Left open" items of ADR-270 answered:** Feature ranking (Organisation set-up, S03i), write-back (not
  Promises; #6657) and the update queue (not Promises).

## Quality

| Gate | Result |
|---|---|
| Feature A | Oracle, slice and candidate reviews recorded under `docs/feature/story-6362-s03d-a-rules-first/runs/`. EXAMINE: round 1 found E2 and E3 not observable; the maintainer chose to fix E3 (the self-test admitted as a probe) and waive E2 at name level; accepted (`90426020c`) |
| Feature B | Oracle and slice reviews per slice, candidate review recorded under `runs/`. EXAMINE accepted in round 4 (`9e2638703`), after observation gaps only, no product violation seen |
| Feature C | Oracle, slice and whole-candidate reviews accepted (`d5b1b8feb`); EXAMINE accepted (`dd8d8f94d`) |
| Mutation | See `docs/feature/story-6362-s03d-package-promises-deliveries/mutation/results.md`: 89.09 %, then 90.80 % after one kill round; D-13 bar met |

No frontend file changed, so no frontend gate ran.

## Decisions taken without the maintainer, open to revisit

- **A4 and C3 dropped as slices** (host, AFK). Each had no RED: the CI selection was already right, and feature B had
  already placed every file. A4's behaviour stays visible through `test-selection-show`; C3's check stays as a guard and
  was judged by feature C's whole-candidate review.
- **A3 fixed the shared compare stage** (host, AFK). Only the DIFFERENT branch gained its missing verdict line; exit
  codes were already right.
- **D-1 to D-13 at DESIGN** (AFK, propose mode), all on the recommended option. Among them: Delivery persistence stays
  with the platform (D-4, S05d); Feature ordering and ranking go to Organisation set-up (D-5, S03i); the write-back round
  stays out of Promises (D-6); the port edge is legal by namespace and the Jira edge is inverted (D-7). The maintainer
  confirmed the start order, the #6657 item, keeping C2 in this Story, per-slice push, size M and the rigor profiles
  (`standard` for feature B, `thorough` for A and C) at the start of DELIVER.
- **The measured lists differ from the design's guesses**, each difference an edge that exists today: the design named
  five platform types for R-P6; the IL adds `IRepository<T>` and `IBlackoutPeriodService`, and the list holds each
  edge per Promises type, 19 in all. R-P8 of the design
  became `TrackerIntegrationReachesNoPromisesArchUnitTest` in C2.
- **The renderer stays in `Application`**, not `Domain`, because it implements a Tracker integration port, and the
  projector stays there because it reads the blackout periods.
- **The maintainer's EXAMINE choices for feature B** (rounds 2 and 3): E5 reworded to what the probes show (nwdes
  refused a probe argument with dots as a path outside the workspace); E7 reworded to every observable reaction (the
  four blocked-period reactions are not observable on any revision and are Work Items code this move did not touch).
  What was promised did not change.

## Knowingly left open

- **Low findings of the candidate review, for the next touch:** the write-back owner in `PromisesUnitTestPlacementTest`
  is worded as a topic, not as who places the files; the Jira connector's constructor parameter is still named
  `forecastBlockRenderer` although its type is now `IDescriptionBlockMerger`.
- **`ef-model-unchanged` is a weak probe:** it printed the dotnet-ef restore message and still exited 0. It should fail
  when it cannot compare.
- **`DeliveryMetricSnapshotRecordingHandlerTest` still starts the app host** for its whole fixture (23 tests seed
  through host-resolved repositories). Its `HostUsersStillToMove` line is renamed to the new namespace, not removed.
  Moving it onto the mock-built handler is a follow-up for the Promises tests.
- **S05d:** the two Delivery repositories and `RecordableDeliveries` stay with the platform (most of R-P6's 19 edges).
- **#6657:** the write-back round (8 files) stays in `Services`; candidate home Forecasting `Application/WriteBack`.
- **S03i:** Feature ordering and ranking (9 files) and `ManualRank`; the Portfolio as a unit.
- **Epic 6474 slice 12:** `PortfolioUpdater` calls the Delivery rule and source sync services inline (the one entry of
  R-P4, ratchet candidate 4).
- **S04b, S12c:** the Delivery entities, kernel Delivery types, source types and wire DTOs stay in `Models` and `API/DTO`.
- **S09a:** no `AddPromises`; `Program.cs` keeps its registrations.
- **`IlDependencies` still misses catch-clause types, events and named attribute arguments**; no current code hits it.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A for user docs, because nothing a user sees changes. Developer docs updated: ADR-270, `ARCHITECTURE.md` §4, `docs/product/architecture/brief.md`, the Epic catalogue and slice-06 brief. `.claude/commands/release.md`: N/A, because it names no Promises path. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because nothing on the marketing surface changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map / ARCHITECTURE.md | Done (this change): ADR-270 Decision 1, "Every edge", the new measured-edges block, ratchet candidates 3 and 4, Decision 5, Decision 6 and "Left open"; `ARCHITECTURE.md` §4 rows 1 and 4, diagram node and arrow, leaf-rule, Work Items and Promises rule bullets. One new term in the Tracker integration glossary, "Description block merge" (`IDescriptionBlockMerger`). No domain event is new; the promise lifecycle events stay target only. |
| Usage-data event | N/A, because nothing a user sees or does changes (DEVOPS, 2026-11-17). |
| Mutation | See `docs/feature/story-6362-s03d-package-promises-deliveries/mutation/results.md`. Backend 89.09 % on the 14 Promises Domain and Application files (163 tested, 145 killed, 16 survived, 2 no coverage); one kill round added 4 tests and 2 rows, 90.80 % after it (148 killed, 15 survived, 0 no coverage). Every remaining survivor is triaged: equivalent, diagnostic text, or argument guard. D-13 bar met: the move added no mutant. Frontend N/A, no file changed. |
| Release notes | N/A; written at /release. |
