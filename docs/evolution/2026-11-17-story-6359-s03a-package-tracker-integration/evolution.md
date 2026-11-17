# Story 6359 — S03a Package Tracker integration: the fourth context in its own module

**Finished:** 2026-11-17 · ADO User Story #6359 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Tracker integration was spread over `Services/{Interfaces,Implementation}/{WorkTrackingConnectors, OAuth,
ConnectionHealth, Parents, Dependencies}`, `Factories`, one hosted service in `BackgroundServices` and a few types in
`Models`. The one rule that protected it as a leaf named only `WorkTrackingConnectors` and `Factories`, and missed the
factory interface's own namespace. Two leaks from the capability Story (S06) were still open: the board port named
implementation types, and the factory interface and its class sat in different namespaces. The Jira connector reaching
Promises' forecast block renderer was an edge no rule saw. Without a module, the next packaging Stories (Promises,
Organisation set-up) had no clean port to write against.

## What changed

Three nwdes features on `main`, behaviour-free, no schema change. Design: `docs/feature/story-6359-s03a-package-tracker-integration/`.

- **Feature A, rules first** (`docs/feature/story-6359-s03a-a-rules-first/`, 3 slices). No production code changed.
  - A1: every architecture rule keyed on the context's namespaces, folders, paths or types also sees
    `Modules/TrackerIntegration`, each proven with a made-up type (`TrackerIntegrationRulesSeeTheModuleArchUnitTest`).
  - A2: the CI test selection, the examine commands, test-time budgets and Stryker configs find connector code under
    the new paths, so a change to a moved connector still selects its live suite.
  - A3: the membership list and the `Models` admissions were measured from the compiled code and fixed in
    `TrackerIntegrationMembershipArchUnitTest`. The A3 oracle review narrowed D-2 (below).
- **Feature B, the move** (`docs/feature/story-6359-s03a-b-move/`, 4 slices). 106 production files in
  `Modules/TrackerIntegration`, moved by `git mv` with namespace lines only, then separate edit commits. The unit tests
  moved with their production code, in the same slice (D-9).
  - B1 `Domain` (3): `WorkItemStateTransitionMapper`, `ParentSourceSelector`, `DependencyFieldReferences`.
  - B2 `Application` (25): the connector port and capability interfaces, the factories, the auth schema, the Board
    types, `WorkItemCategoryCrossing` and `DependencySourceSelector`. Both S06 leaks are closed by placement.
  - B3 `Adapters/Connectors` (52): the five connectors, their helpers, the auth strategies, Jira's issue factory.
  - B4 `Application/OAuth` (19), `Application/ConnectionHealth` (3), `Adapters/OAuth` (3), `Adapters/Background` (1),
    with the four OAuth value types that moved out of `Models`.
- **Feature C, edges and rules** (`docs/feature/story-6359-s03a-c-edges-and-rules/`, 2 slices).
  - C1: `TrackerIntegrationModuleBoundariesArchUnitTest`, on the shared `ModuleRules`, with the lists measured from
    the compiled code (R-T1 to R-T8, in ADR-270). The old leaf pattern and its two rules are retired.
  - C2: `TrackerIntegrationUnitTestPlacementTest` keeps the 99 unit-test files under
    `Lighthouse.Backend.Tests/Modules/TrackerIntegration` (`Domain` 5, `Application` 7, `Application/OAuth` 7,
    `Adapters/Connectors` 78, `Adapters/OAuth` 2), and a check that no file of the context is left in an old folder.
- **Docs, at finalize:** ADR-270 (Decision 1 row, Decision 6 counts, "Every edge", ratchet candidate 3 answered, new
  block "Edges measured by packaging Tracker integration", "Left open" write-back answered, glossary);
  `ARCHITECTURE.md` §4 (row 1, diagram node, leaf-rule bullet); `docs/product/architecture/brief.md` (new section
  "story-6359 s03a package tracker integration" and the module table row); the Epic catalogue
  (`docs/feature/epic-6264-modular-monolith/feature-delta.md`: Improvement log, KPIs, S03* notes); the slice-03 brief
  (the `Models` criterion corrected and the notes for S05e, S03d, S03i, S09a); `.claude/commands/release.md` (the
  connector list now names `Modules/TrackerIntegration/Adapters/Connectors`).

## Key commits

Range: `git log --oneline --grep="6359\|tracker-integration\|connectors" 175daeeec..HEAD`.

| Commit | What |
|---|---|
| `c4c1d86b3` | DESIGN and DEVOPS |
| `c7a532de3`, `2ba64604b` | A1: every rule of the context also sees `Modules/TrackerIntegration` |
| `1515ea02a`, `a9b144677` | A2: CI selection and the throw count find connector code under the module |
| `27045a3e5`, `219adde75`, `70ee9a93d`, `5b197d07d` | A3: the membership measured from the compiled code; D-2 amended |
| `006337875`, `25354b0f1`, `014b48f21`, `84f61a7d6`, `84284ef27` | B1: the translation rules into `Domain` (two into `Application`) |
| `b025a7b96`, `fb0e244c0`, `0eadfeb2f`, `28eed1728`, `1858b3b07` | B2: the connector port, factories, auth schema and board types into `Application`; D-14 |
| `01515e12b` | the host-reach guards remember what they walked (CI per-class time budget) |
| `ba865da0c`, `62e9c579d`, `802f0d1a9` | B3: the five connectors, auth strategies and issue factory into `Adapters/Connectors` |
| `4d781ee22`, `c9a5e8a9f`, `4ca401355` | B4: OAuth and connection health into `Application/OAuth`, `Application/ConnectionHealth`, `Adapters/OAuth`, `Adapters/Background` |
| `814ff9c88`, `6ec283874` | two examine probes: `ci-live-suites-show`, `test-selection-show` |
| `c2d02a41c`, `ec66f3271` | C1: the module's own rules; the old leaf rule and the old CI alternatives retired |
| `61190c830`, `d593b0346` | C2: test placement and no file left; the last old-path alternatives removed |
| `74ae9c6b6` | feature B's EXAMINE waived by the maintainer, evidence recorded |
| `b08c6f99c` | feature C's EXAMINE accepted |

## What it achieved

- **Behaviour unchanged.** Refresh answers: 297 compared, 0 different (`refresh-answers-compare`). No EF model change
  on SQLite or Postgres (`ef-model-unchanged`). Architecture rules 1005 of 1005 passed. The full filtered backend suite
  13,769 passed, 0 failed. CI on the push of feature B: every live suite passed (Jira 72, Azure DevOps 58, Linear 20,
  ServiceNow 20, GitHub 32).
- **Contexts packaged with namespace rules:** 3 → 4 of 11. Files under `Services/Interfaces` + `Services/Implementation`
  332 on disk (115 + 217); the design projected 328. `Models` files 267 → 263.
- **Ratchet candidate 3 answered.** Promises reaches Tracker integration through its port, two listed callers. The one
  edge the other way (Jira connector → Promises' renderer) is the single entry of the leaf rule, owner S03d.
- **Both S06 leaks closed** with no signature change.

## Quality

| Gate | Result |
|---|---|
| Feature A | Oracle, slice and candidate reviews recorded under `docs/feature/story-6359-s03a-a-rules-first/runs/`. The A3 oracle review found D-2 too wide; amended (below) |
| Feature B | Oracle and slice reviews per slice, candidate review recorded under `runs/`. EXAMINE **waived by the maintainer (2026-11-17)** after two attempts that did not reach `accepted` |
| Feature C | Oracle, slice and whole-candidate reviews accepted; EXAMINE accepted (`b08c6f99c`) |
| Mutation | 65.30 % on the moved Domain and Application files; D-13 bar met; 80 % gap recorded as a follow-up (`mutation/results.md`) |

No frontend file changed, so no frontend gate ran.

## Decisions taken without the maintainer, open to revisit

- **D-2 amended (A3 oracle review).** A `Models` type moves only if it is not an entity, not on the wire, not reached
  by a kernel type and not used by code that stays outside the module. So `ConnectionFetchShapeChanged`, the write-back
  vocabulary and `OAuthCompleteResult` stay; only four OAuth value types moved.
- **D-14 (B2 slice review).** `WorkTrackingConnectorFactory`, `WorkTrackingSystemFactory` and
  `AuthenticationMethodSchema` name connector types. They stay in `Application`, and each edge is listed by exact type
  name in C1's Application outbound list, owner S09a. Alternatives rejected: moving the factories to `Adapters`
  (breaks the one-namespace fix and B2's approved oracle), an option-name source of its own (an edit inside a move).
- **D-1 outcome of the B1 Domain rule.** `WorkItemCategoryCrossing` and `DependencySourceSelector` call helpers in
  `Extensions`, so they live in `Application`. Feature C keys its lists by full name, because the moved Azure DevOps
  `WorkItemExtensions` and the unmoved one in `Extensions` share a short name.
- **The measured lists differ from the design's guesses**, each difference an edge that exists today: outside →
  `Domain` is 1 (not 2: `Program` does not reach it); outside → `Application` adds `DemoDataFactory`, and
  `ConnectionSaveService` and the Encryption readers do not appear; outside → `Adapters` is the two connection
  controllers only; the Application outbound list holds 11 entries (5 of them D-14's); R-T8 is derived from R-T6.
- **EXAMINE waiver for feature B.** The examiner's own probes met the live-suite expectation and showed identical
  refresh answers and an unchanged EF model, with observation gaps for auth setup, board listing, OAuth and
  connection health. Two new examine probes came out of it (`ci-live-suites-show` and `refresh-answers-compare` on
  this feature's commit); the examiner recorded no contradicting observation. Before the next move feature, give the
  examiner probes for the behaviours its expectations name, while the expectations are written.
- **Tests moved with the code** (D-9, a deviation from S03b), because the test paths are in the live-suite
  classifier and a second move would have started every live suite again on the shared Linear key.
- **Write-back (D-6).** The translation stays with Tracker integration; the round stays with its publisher. S03d
  files the ADO item.

## Knowingly left open

- **The old alternative** is still in feature B's closed `TrackerIntegrationApplicationLayoutArchUnitTest` (line 25)
  and in feature A's membership oracle. Both are closed oracle files and were left unedited.
- **Empty untracked old folders** remain locally under `Services` and `Factories`; git does not track them.
- **No unit test for `ConnectionHealthService`, `ConnectionHealthCadence` or `ConnectionHealthProber`**, only an
  integration spec. Add unit tests when S05e gives the service ports.
- **`dev-test.sh`'s auto filter** may select JiraIntegration-only tests for a change that touches shared module code.
- **S05e:** the three repositories stay with the platform; `ConnectionHealthService` and `OAuthService` take them
  (4 listed edges).
- **S09a:** the factories and the auth schema name connectors (5 listed edges); no `AddTrackerIntegration`.
- **S03d:** the Jira connector → Promises renderer edge, and the two Promises callers in the Application inbound list.
- **S03i:** the connection save and the connection usage stay for Organisation set-up.
- **S04b, S12c:** the connection entities, option types, write-back vocabulary and wire DTOs stay in `Models` and `API/DTO`.
- **`IlDependencies` still misses catch-clause types, events and named attribute arguments**; no current code hits it.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A for user docs, because nothing a user sees changes. Developer docs updated: ADR-270, `ARCHITECTURE.md` §4, `docs/product/architecture/brief.md`, the Epic catalogue and slice-03 brief, `.claude/commands/release.md`. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because nothing on the marketing surface changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map / ARCHITECTURE.md | Done (this change): ADR-270 Decision 1, Decision 6, "Every edge", the new measured-edges block, ratchet candidate 3, "Left open" and glossary; `ARCHITECTURE.md` §4 row 1, diagram node and leaf-rule bullet. No term, concept or domain event is new. The Tracker integration events stay marked target only, because nothing publishes them yet. |
| Usage-data event | N/A, because nothing a user does changes (DEVOPS, 2026-11-16). |
| Mutation | Done for the D-13 bar (no mutant added; the moved code is byte-identical apart from namespaces). Backend 65.30 % on the 50 moved Domain and Application files (238 killed, 69 survived, 58 no coverage), below the 80 % project gate; the gap (46 no-coverage in connection health, 32 string survivors) is a proposed improvement item. Frontend N/A, no file changed. `docs/feature/story-6359-s03a-package-tracker-integration/mutation/results.md` |
| Release notes | N/A; written at /release. |
