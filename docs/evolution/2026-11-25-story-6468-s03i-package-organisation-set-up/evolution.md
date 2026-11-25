# Story 6468 — S03i Package Organisation set-up: set-up in its own module, an open host with its own rules
**Finished:** 2026-11-25 · ADO User Story #6468 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Organisation set-up (settings, Terminology, blackout periods and recurring rules, Feature ordering and ranking, optional
features, connection defaults and connection usage) still sat in `Services/Interfaces` and `Services/Implementation`,
beside types of other contexts and of the platform. The rules that protect the other modules named set-up by those loose
folders, so they could not tell it from the rest of `Services`, and set-up had no rules of its own. Every other context
depends on set-up, yet nothing said which of those edges were legal.

## What changed

One nwdes feature on `main`, five slices, behaviour-free, no schema change, no route or DTO change. Production code
changed only in `namespace` lines, `using` lines and four set-up registrations in `Program.cs`. Design:
`docs/feature/story-6468-s03i-package-organisation-set-up/`.

- **S1, the rules see the move.** Set-up's membership (27 types, each with its layer and its old and new namespace) is
  measured from the compiled code and pinned in a test. Every rule pattern that saw a moving type is classified (it stands
  for a context, for a place, or for a folder only) and every context pattern also sees the new `Modules.OrganisationSetup`
  namespace. No test string names an old path outside a fixed rename list.
- **S2, the first move.** Blackouts and recurring rules, `WeeklyRecurrence` (into `Domain`), Feature ordering and ranking,
  and the optional features (18 files, 5 unit tests) moved to `Modules/OrganisationSetup/{Domain,Application,
  Application/OptionalFeatures}` with `git mv`.
- **S3, the second move.** Settings, Terminology, connection defaults and connection usage (9 files, 4 unit tests) moved to
  `Modules/OrganisationSetup/{Application,Application/Connections}`. Tracker integration lists exactly one edge into
  set-up: the connection-health cadence reading the settings port.
- **S4, set-up is an open host.** Every downstream context reads set-up's `Application` and `Domain` by right, each with a
  check that its rule still reads a foreign context that is not set-up. Set-up has its own rules,
  `OrganisationSetupModuleBoundariesArchUnitTest` on `ModuleRules`: every type in its layer and none left under `Services`;
  `Domain` reaches only the kernel; `Application` reaches EF Core and Work Items' `Domain` only along 16 listed edges
  (`ConnectionSaveService` 3, `FeatureRankingService` 9, `FeatureRankSeeder` 4). Set-up's events are built only in its
  `Application`, except `TeamController -> TeamDeleted`, which Epic 6474 empties. Its unit tests sit beside it
  (`OrganisationSetupUnitTestPlacementTest`).
- **S5, rules name set-up by its module only** (`OrganisationSetupRulesNameOnlyTheModuleArchUnitTest`). Five fields that
  still named set-up's old home now name only `Modules.OrganisationSetup`. Where a type that stays in the loose `Services`
  namespaces must still be matched, the rule names it in a stray list by full name (`ModuleRules` can now match by full
  name as well as by namespace), so no rule loosens.
- **Docs:** ADR-270 (the set-up row, the edge rows, the Left-open items on Feature ranking and `ManualRank`, a new block
  "Edges measured by packaging Organisation set-up"); `ARCHITECTURE.md` §4 (row 4 gains its set-up half, a new set-up
  module rules bullet); the brief. No new term enters the glossary.

## Decisions taken in AFK mode that the maintainer may revisit

All are recorded in `feature-delta.md`, "Host decisions during DELIVER".

- **Downstream set-up edges left with the move (S2, S3):** Refinement's, Promises', Access', Licensing's and Reports'
  outbound rules read set-up only through the old folder, so their entries into a moved type left in the slice that moved
  it. They were legal by omission until S4 made them legal by right; the slices were pushed together, so no pushed
  revision carried that window.
- **The platform lane keeps its three settings edges guarded (S3):** its Services-only rule also reads set-up's module.
- **Access and Licensing are open-host consumers (candidate review):** the design said their edges stay unchanged; after
  the move they reach set-up by right, as ADR-270's context map already says they depend on it.
- **The move-preservation laws end for the narrowed fields (S5):** the preservation claim held only while the move ran.
  Two declared lists (5 set-up fields, 3 platform-lane fields) name the only fields that may stop seeing the old place,
  each with checks; the platform list holds only while none of the platform lane's 35 moved types is left in the loose
  namespaces.
- **Rigor:** S1 and S4 `thorough` (designer, crafter and architect Opus; reviewers Sonnet); S2, S3, S5 `standard` (Sonnet;
  reviewers Haiku); candidate review and EXAMINE Sonnet.
- **Rejections and their fixes:** the S2 slice review (downstream edges unguarded until S4; answered by design), the S3
  craft (two conflicts with S1's frozen roles; S1 revised twice), the S5 oracle review (three rounds: tokenless fields,
  then an exemption probe and a self-compared exclusion list), and the S5 craft (three earlier oracles still demanded the
  old namespace; preservation retired for the declared fields).

## Knowingly left open

- Team, Portfolio, the mapping and levels stay in the kernel and the repositories until Epic 6474 and the S05 Stories.
- `TeamController -> TeamDeleted` stays listed until Epic 6474 slice 03.
- No rule guards a platform type reaching a context module other than its Adapters (pre-existing).
- The Tracker integration single edge into set-up is held in two lists (its own rule and set-up's), both compared both
  ways with the compiled code.
- Fixture names `AnAccessServiceAskingTheAppSettings`, `AReportBandAskingTheSettings` and siblings now ask
  `IFeatureListService`; rename them when next touched.
- Mutation survivors are recorded, not killed; `FeatureRankingService` and `FeatureOrderingPolicyProvider` are observed
  only by the host scenarios Stryker leaves out. See `mutation/results.md`.

## Commits

- `987a393f2` docs(organisation-setup): story 6468 S03i — DESIGN and DEVOPS, proposed in AFK mode
- `325007592` docs(organisation-setup): story 6468 S03i — product facts and design recorded in nwdes
- `1c391f40a` docs(organisation-setup): story 6468 S03i — examiner expectations recorded
- `2c3d34982` test(organisation-setup): oracle — the rules see the Organisation set-up move before it happens
- `0153443c4` docs(organisation-setup): story 6468 S03i — S1 oracle approved
- `d420c2b7b` refactor(organisation-setup): the rules that see Organisation set-up also see Modules.OrganisationSetup
- `c12ac7859` docs(organisation-setup): story 6468 S03i — S1 slice review accepted
- `66f7fbb65` test(organisation-setup): oracle — calendar, Feature ordering and optional features live in Modules.OrganisationSetup
- `e9c8c8fbf` docs(organisation-setup): story 6468 S03i — S2 oracle approved
- `0ee5ffe17` refactor(organisation-setup): move calendar, Feature ordering and optional features into Modules/OrganisationSetup
- `36063b1d0` refactor(organisation-setup): move the calendar, ordering and optional-feature unit tests beside the module
- `11f580a0e` refactor(organisation-setup): using lines, Program.cs and the rule lists follow the first move
- `bf404b32b` test(organisation-setup): the rename list lets a fixture's expected offender leave once its rule stops reading set-up as foreign (oracle)
- `6b8d4eb62` docs(organisation-setup): story 6468 S03i — revised S1 oracle approved
- `27b1919d4` docs(organisation-setup): story 6468 S03i — S2 craft GREEN
- `dbea8a734` docs(organisation-setup): story 6468 S03i — S2 slice review rejected (downstream set-up edges unguarded until S4)
- `cf0db8fea` docs(organisation-setup): story 6468 S03i — design: downstream set-up edges leave with the move, S4 makes them legal by namespace
- `eec7f32bb` docs(organisation-setup): story 6468 S03i — S2 slice review accepted (round 2)
- `76592bc78` test(organisation-setup): oracle — settings, terminology and connections live in Modules.OrganisationSetup
- `0151ef7a8` docs(organisation-setup): story 6468 S03i — S3 oracle approved
- `4ac06f4ba` refactor(organisation-setup): move settings, terminology and connections into Modules/OrganisationSetup
- `04595a458` refactor(organisation-setup): move the settings, terminology and connection unit tests beside the module
- `fd08a9269` refactor(organisation-setup): using lines, Program.cs and the rule lists follow the second move
- `29f53073e` test(organisation-setup): Tracker integration's other-contexts pattern carries a set-up role (oracle)
- `6c92e7ad2` docs(organisation-setup): story 6468 S03i — revised S1 oracle approved (round 3)
- `0948db34f` refactor(organisation-setup): Tracker integration sees set-up as another context, and the platform lane keeps its settings edges guarded
- `e53871450` docs(organisation-setup): story 6468 S03i — S3 slice review accepted
- `eb1ce9e97` test(organisation-setup): oracle — set-up is an open host, with its own boundary and placement rules
- `6a07d058a` docs(organisation-setup): story 6468 S03i — S4 oracle approved
- `81396473d` refactor(organisation-setup): Work Items reaches set-up's Application and Domain by right
- `543e31329` docs(organisation-setup): story 6468 S03i — S4 slice review accepted
- `24fd39a67` test(organisation-setup): oracle — rules name set-up by its module only
- `36613f5be` docs(organisation-setup): story 6468 S03i — S5 oracle review rejected (tokenless Context fields unjudged)
- `f6859cf96` test(organisation-setup): the S5 oracle judges every Context field, with or without the module's name
- `987f773ca` docs(organisation-setup): story 6468 S03i — design: the exact S5 narrowing of each widened rule field
- `32640db3f` docs(organisation-setup): story 6468 S03i — S5 oracle review round 2 rejected (exemption by probe; self-compared exclusions)
- `ae96a4b6c` test(organisation-setup): the S5 oracle exempts only named whole-backend fields and compares its exclusions with an independent list
- `519a6a94c` docs(organisation-setup): story 6468 S03i — S5 oracle approved (round 3)
- `93c428cd5` test(organisation-setup): the move-preservation laws end for the five fields S5 narrows (oracle)
- `654044b5b` refactor(organisation-setup): rules name set-up by its module only
- `3a9c4a800` docs(organisation-setup): ARCHITECTURE.md, ADR-270 and the brief record Organisation set-up packaged
- `d594f03ac` docs(organisation-setup): story 6468 S03i — revised S1 and S4 oracles approved after S5's narrowing
- `fdd1ddde4` docs(organisation-setup): story 6468 S03i — S5 craft accepted
- `71bcab9fd` docs(organisation-setup): story 6468 S03i — S5 slice review accepted; host decisions recorded
- `e31859d42` docs(organisation-setup): story 6468 S03i — candidate review accepted; Access and Licensing recorded as open-host consumers
- this finalize commit

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no page under `docs/` describes the code layout. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done in `3a9c4a800`: ADR-270, `ARCHITECTURE.md` §4 and the brief. |
| Usage-data event | N/A, because the Story changes no behaviour a user can use (DEVOPS); set-up's existing events are unchanged. |
| Mutation | Done: Stryker.NET over `Modules/OrganisationSetup/**`, **80.37 %** (262 killed, 22 survived, 42 no coverage); the 27 moved files are identical to their pre-move selves apart from `namespace` and `using` lines, so the move added no survivor. |
| Release notes | N/A; written at /release. |

## Gates

- `dotnet build Lighthouse.Backend`: 0 warnings.
- Architecture namespace: 2025 passed, 0 failed. Test-Fast: 11855 passed, 0 failed.
- Slice reviews: all accepted (S2 at round 2). Whole-candidate review: accepted, 2 non-blocking findings.
- EXAMINE: accepted (first round); its own full filtered backend run: 14994 tests, 14982 passed, 12 skipped, 0 failed; the EF model is unchanged for Sqlite and Postgres.
- CI: see the push of this commit.
