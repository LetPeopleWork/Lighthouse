# Story 6364 — S03f Platform composition root: the platform lane in its own namespaces, with its own rules
**Finished:** 2026-11-24 · ADO User Story #6364 · Epic #6510 "Modules: package the bounded contexts"

## The problem

The platform lane is the infrastructure below the contexts: the encryption key ring, backups and database management,
demo data, seeding, usage-data consent and forwarding, logging, release checks and updates, the domain-event dispatcher,
the host facilities and the update queue's mechanics. It is not a context, because no user tells the story of Lighthouse
through it. But its code still sat in `Services/Interfaces` and `Services/Implementation`, mixed with the code of the
contexts that were not packaged yet, and nothing said where it ended.

Three things followed from that. A rule that protected a context could not tell a platform type from any other
`Services` type. The lane had no direction rule of its own, so nothing stopped a platform type from reaching an `API`
type or a context's adapter. And several rules carried a note that the lane's place would be decided "until S03f".

## What changed

Three nwdes features on `main`, behaviour-free, no schema change, no route or DTO change. Production code changed only in
`namespace` lines and `using` lines. Design: `docs/feature/story-6364-s03f-platform-composition-root/`.

- **Feature A, rules first** (`docs/feature/story-6364-s03f-a-rules-first/`, 4 slices). No production code changed.
  - A1: the platform lane's membership (which type lives in which of the 11 areas) was measured from the compiled code
    and pinned in `PlatformLaneMembership` and its test.
  - A2: every architecture rule that sees a platform type by its old namespace also sees it under
    `Lighthouse.Backend.Platform`, proven with made-up types. The slice was rejected once, because the rules match
    namespaces and the widening had named types; the re-craft widened by whole area. A preservation law checks each rule
    pattern on the subject its rule reads.
  - A3: a change to the release code under `Platform/Releases` still selects the live GitHub suite in CI.
  - A4: the new examine command `platform-answers-compare` compares the platform's answers between two revisions.
- **Feature B, the move** (`docs/feature/story-6364-s03f-b-move/`, 5 slices, each an oracle commit, a refactor commit
  and a slice review). Every platform type moved with `git mv`; its tests moved to `Tests/Platform/<Area>` in the same
  slice.
  - B1: the key ring and secret custody to `Platform/Encryption`.
  - B2: database management, demo data, seeding and the race-tolerant save to `Platform/DatabaseManagement`,
    `DemoData`, `Seeding` and `Persistence`.
  - B3: usage data, logging and releases to `Platform/UsageData`, `Logging` and `Releases`. Its first two reviews were
    rejected (no run evidence, then baselines not proven) and the third accepted.
  - B4: the domain-event dispatcher, the clock and the host facilities to `Platform/DomainEvents` and `Platform/Host`.
  - B5: the update queue's mechanics to `Platform/UpdateQueue`. The Team and Portfolio pipelines stay in `Services`.
- **Feature C, edges and rules** (`docs/feature/story-6364-s03f-c-edges-and-rules/`, 2 slices).
  - C1: `PlatformLaneBoundariesArchUnitTest` holds the lane's rules R-PL1 to R-PL4, measured from the compiled code,
    with a falsifier per rule and per list position: R-PL1 every listed type lives in its area and none is left in
    `Services`; R-PL2 the lane reaches no `API` type; R-PL3 it reaches no context's `Adapters`; R-PL4 it reaches
    `Services` only along 19 listed edges (`TheLanesEdgesIntoServices`).
  - C2: `PlatformLaneUnitTestPlacementTest` keeps every platform unit test under `Tests/Platform/<Area>`, checks that no
    platform file is left in `Services/Interfaces` or `Services/Implementation`, and names the 151 production files that
    stay there, each with its owner. The last two platform tests left in `Tests/Services` (`GithubServiceTest` and
    `PlatformExtensionTest`) moved under `Tests/Platform` (F7-S2).
- **Docs, at finalize:** ADR-270 (header "Moved so far", the *(Platform lane)* row of Decision 1, the "Flow metrics →
  Platform lane" row, the R4 baseline's type name, the "Adapters into the shared update code" row, the new block "Edges
  measured by packaging the platform lane", the update-queue note in "Left open", Decision 6's folder names and the answer
  on Platform/Persistence); `ARCHITECTURE.md` §4 (rows 4 and 7, the diagram node, the shared-update-code edge count and
  the new platform lane rules bullet). No new term enters the glossary.

## Decisions taken in AFK mode that the maintainer may revisit

Feature B, the five answers of the maintainer on 2026-11-22 and the four engineering choices taken in AFK mode:

- **F1 (maintainer, 2026-11-22):** the rule that nothing which resolves or keeps a key may write to a log
  (`SecretCustodySeamArchUnitTest`) names exactly `CryptoService` and `KeyRingFileWatcher` as exempt, with the reason beside
  them. Without it, moving them into `Platform.Encryption` would have changed which types the rule examines.
- **F2 (maintainer, 2026-11-22):** two of feature A's membership tests build their failing example from the map's own
  rows, so they hold before and after the move. Only the choice of example changed; every assertion stays.
- **F3 (maintainer, 2026-11-22):** two `ICryptoService` edges of Tracker integration (`ConnectionHealthService`,
  `OAuthService`) became visible and are listed, with the reason that they existed before the move.
- **F4 (maintainer, 2026-11-22, "the pattern does not widen"):** Tracker integration's `PromisesAndTheUpdateCode` pattern
  gets no `Platform.Encryption` alternative, because the key-ring file watcher had matched it only through its old folder.
  A per-type exception in `PlatformLanePatternRoles` carries this.
- **F3-S3 (AFK engineering choice):** the map row of a later move names a test file at its old path, and an earlier slice
  moves that file. The membership test looks the file up at its old path, then by name under `Tests/Platform`. The map is
  unedited and every assertion stays.
- **F5-S4 (AFK engineering choice):** two feature A tests took a type that moves in B4 as their example. They now take
  their example from the map, and the "Platform alternative" self-test asks a made-up outside name, because after B4 no
  production name outside `Platform` contains the text `Platform`. Assertions are unchanged.
- **F6-S5 (AFK engineering choice):** the update queue's interfaces already matched the shared kernel rule, so moving them
  made two `ForecastUpdater` edges (`IUpdateQueueService`, `IUpdateStatusStore`) visible. They are listed with the reason
  that they existed before the move; no shared rule was narrowed.

Feature C:

- **F7-S2 (AFK engineering choice):** `GithubServiceTest` and `PlatformExtensionTest` moved with `git mv` to
  `Tests/Platform/Releases` and `Tests/Platform/Host`, instead of being named as staying tests. Bodies, class names,
  categories and fixtures are unchanged, and the test selection needed no script change.

Other choices, at DESIGN: eleven areas and no layers; the update pipelines, the repositories, `Data` and the context seeders
stay; the lane's edges into modules stay under those modules' inbound rules and are not listed twice. The move pushes
together, so the live suites run once.

## Key commits

| Commit | What |
|---|---|
| `fcb1ff878` | DESIGN and DEVOPS, proposed in AFK mode |
| `7767a1959`, `dee6784ea`, `62aefb5a5`, `c85bfec1d`, `e4beeee9b` | A1 and A2: membership pinned; rules see the move (first widening, revised oracle, widening by whole area) |
| `4a3c27a6b`, `d9eef9f55` | A3 (test selection for `Platform/Releases`) and A4 (`platform-answers-compare`) |
| `b4ee5849e` | feature A's whole-candidate review accepted |
| `7dcec2d7c` | B1: key ring and secret custody in `Platform/Encryption` |
| `9ab2718d7` | B2: database management, demo data, seeding, the race-tolerant save |
| `8d9e959d7`, `290bfb9d0` | B3: usage data, logging, releases (and two unused usings dropped) |
| `248b77544`, `3b3456399` | B4: dispatcher, clock, host facilities (and a self-import dropped) |
| `62dba989d`, `4f7789a5c` | B5: the update queue's mechanics (and unread service usings dropped) |
| `baa34bb0f` | feature B's whole-candidate review accepted |
| `6dec903de` | C1: R-PL1 to R-PL4 |
| `7d4883cfe`, `12217d132`, `b7a87bed5` | C2: placement rule, the last two tests follow their types |
| `74946da80` | fix: new files in the platform release folders are no longer ignored by `.gitignore` |
| `5af5d6736` | feature C's whole-candidate review accepted |

## What it achieved

- **Behaviour unchanged by construction.** No route, DTO, configuration key, entity or schema changed. Production files
  changed only in `namespace` and `using` lines. Existing tests changed only by moves, `using` lines and rule-file strings.
- **The platform lane is a place.** Every platform type is under `Lighthouse.Backend.Platform.<Area>`, in one of 11 areas.
  It has its own direction rules, so a platform type that reaches an `API` type, a context's adapter or an unlisted
  `Services` type fails the build of the tests.
- **19 measured edges into `Services`**, all of them repository ports, `IAppSettingService` or the write-back types; the
  list only shrinks. The lane's edges into contexts stay under those contexts' inbound rules.
- **151 production files stay in `Services/Interfaces` and `Services/Implementation`**, each named with its owner, so
  nothing platform-like can be left or added there unseen.

## Knowingly left open

- **EXAMINE was not run** for features A, B and C. nwdes refuses it on Windows (it needs no-follow file opens). The
  maintainer runs it later on Linux. The whole-candidate reviews, the filtered backend suite and the layout pins stand in
  until then.
- **Non-blocking oracle findings of feature B**, carried to the next touch. They are in the review records
  (`docs/feature/story-6364-s03f-b-move/runs/review.*.json`) and in the craft records beside them.
- **Timing-sensitive tests.** `PortfolioUpdaterTest.ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanOneProject_Refreshes`
  depends on timing, and three tests flaked under load: `Save_and_update…`, `An_old_client…` and `Refreshing_everything…`.
  None was changed by this Story.
- **Does `Platform.UsageData` count as "update code"?** Tracker integration's `PromisesAndTheUpdateCode` pattern is
  open to the same narrowing as F4 for the usage-data types, if a later move makes them visible to it. Not decided.
- **The wording of `UsageDataConsentRepository`'s owner text** in the placement rule ("Platform usage data, its persistence
  adapter kept with the repositories") says the repository stays for the repositories-by-role work; the wording may be
  sharpened.
- **The `.gitignore` fix `74946da80`.** The rule `[Rr]eleases/` hid the new `Platform/Releases` folder, so its files would
  not have been committed. The fix un-ignores the platform release folders; other release-named folders stay ignored.
- **Repositories by role (Epic 6507), S03g, S09a and Epic 6474:** the repositories, the context seeders and the update
  pipelines keep their named owners.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A for user docs, because nothing a user sees changes. Developer docs updated: ADR-270 and `ARCHITECTURE.md` §4 |
| Screenshots | N/A, because no screen changed |
| Demo data | N/A, because no data shape or seeded value changed |
| Website assets | N/A, because nothing on the marketing surface changed |
| Clients (CLI, MCP) | N/A, because no route or DTO changed |
| Context map / ARCHITECTURE.md | Done (this change): ADR-270 header, the *(Platform lane)* row, the "Flow metrics → Platform lane" row (decided: the edge stays in Flow metrics' R4 list, not listed twice), the "Adapters into the shared update code" row, the new measured-edges block with R-PL1 to R-PL4 and the placement rule, the update-queue note, Decision 6 and the answer on Platform/Persistence; `ARCHITECTURE.md` §4 rows 4 and 7, the diagram node, the shared-update-code edge count and the platform lane rules bullet. No new term and no new domain event. UI surface: none |
| Usage-data event | N/A, because nothing a user sees changes (DEVOPS) |
| Mutation | N/A, because no production logic line changed; production changed only in namespace lines and usings |
| Release notes | N/A; written at /release |
