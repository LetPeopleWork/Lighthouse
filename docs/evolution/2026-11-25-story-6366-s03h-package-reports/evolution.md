# Story 6366 — S03h Package Reports: Reports in its own module, with its own rules
**Finished:** 2026-11-25 · ADO User Story #6366 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Reports was a clean context: it reads the Flow metrics through one port, asks Licensing whether reports may be used, and
nothing but `API` and the composition root names it. But its code still sat in `Services/Interfaces/Reports` and
`Services/Implementation/Reports`, beside code of contexts not yet packaged. The rules that protect it named it by those
folders, so they could not tell it from the rest of `Services`. It had no direction rules of its own: nothing stopped a new
edge from Reports into Access, an adapter, Tracker integration or the update code, and nothing named the edges it takes into
Flow metrics, Licensing, the repositories and the clock.

## What changed

Three nwdes features on `main`, behaviour-free, no schema change, no route or DTO change. Production code changed only in
`namespace` lines, `using` lines and the Reports registrations in `Program.cs`. Design:
`docs/feature/story-6366-s03h-package-reports/`.

- **Feature A, rules first** (`docs/feature/story-6366-s03h-a-rules-first/`, 2 slices). No production code changed.
  - A1: Reports' membership (each type, its layer, its old and new namespace, and the types that stay) was measured from
    the compiled code and pinned in a test. A type goes to `Domain` only if its edges reach the kernel, the .NET base
    library and other Reports `Domain` types: `ReportCapPolicy`, `ReportSeriesReading`, `ReportWindowPolicy`. The metrics
    go to `Application/Metrics`. Every other type goes to `Application`.
  - A2: every rule pattern that sees a Reports type also sees it under `Modules.Reports`, and no rule string names the old
    path outside a fixed list. The shared namespace-move law checked it on the subject each rule reads.
- **Feature B, the move** (`docs/feature/story-6366-s03h-b-move/`, 2 slices).
  - B1: the 31 production files moved to `Modules/Reports/{Domain,Application,Application/Metrics}` with `git mv`; each one
    is identical to its old self apart from its namespace and `using` lines. `Program.cs` changed only in the Reports
    registration names. `Models/Reports` (7 types) and `ReportRepository` stay where they are.
  - B2: the unit tests of the moved types moved to `Tests/Modules/Reports/{Domain,Application,Application/Metrics}`; the
    acceptance, host and controller tests stay, each named with its owner.
- **Feature C, edges and rules** (`docs/feature/story-6366-s03h-c-edges-and-rules/`, 2 slices).
  - C1: every rule names Reports by `Modules.Reports` alone, in `ReportsRulesNameOnlyTheModuleArchUnitTest`. The joint
    Tracker integration pattern drops Reports from its `Services` alternative, and keeps Auth, Authorization and Licensing
    there (see the host decision below). The move-time helpers retired, because their old side no longer exists.
  - C2: `ReportsModuleBoundariesArchUnitTest` is the ninth module rule set on `ModuleRules`, measured from the compiled
    code, with a falsifier per rule and per list position: R-RP1 every type in its layer (36 types: `Domain` 3,
    `Application` 25, `Application.Metrics` 8) and none under `Services`; R-RP2 `Domain` reaches only the kernel; R-RP3 and
    R-RP4 outside code reaches `Domain` only from `API`, the composition root and Reports' `Application`, and `Application`
    only from `API`, the composition root and `ReportRepository`; R-RP5 `Application` reaches no EF Core, `Data`, `API`,
    module adapter, Tracker integration, Access or update code, except 3 listed EF Core edges; R-RP6 it reaches Flow
    metrics' `Domain`, Licensing, Organisation set-up data and the platform only along 12 listed edges.
- **Docs, at finalize:** ADR-270 (header "Moved so far", the Reports row of Decision 1, the new block "Edges measured by
  packaging Reports"); `ARCHITECTURE.md` §4 (row 9, the diagram node label, the new Reports module rules bullet); the
  Feature's S03h note and the slice brief's IN and acceptance criteria (amended as DESIGN decided). No new term enters the
  glossary.
- **The IL reader reads catch clauses.** The candidate review of feature C found that `ReportBulkRemover` reaches
  `DbUpdateConcurrencyException` only in a `catch` clause, which the IL reader skipped. The reader now reads catch types
  too, so R-RP5 lists all three EF Core edges the design asked for. Every other rule that reads the IL benefits.

## Decisions taken in AFK mode that the maintainer may revisit

- **Joint Tracker integration pattern keeps Auth, Authorization and Licensing (2026-11-25):** feature A's design dropped
  them as dead. Dropping them made two rows of `AccessAndLicensingRulesSeeTheModulesArchUnitTest` fail, because those rows
  demand that a Tracker integration type reaching the old Access and Licensing places is still caught. Keeping them
  loosens nothing. Revisit if the old places are confirmed empty for good.
- **`Models/Reports` stays in the kernel; `ReportRepository` stays with the platform repositories:** both wait for a later
  Story (S04b and S12, S05). `ReportOwnerGoneException` passes all four conditions for a move, but stays with the other
  `Models/Reports` types.
- **EF Core exception edges are listed, not moved:** moving the catch into an adapter would be an edit, and it would
  disprove the "namespace changes only" hypothesis.
- **Rigor:** feature A `thorough` (crafter Opus, reviewers Sonnet); features B and C `standard` (designer, crafter and
  architect Sonnet; reviewers and EXAMINE Haiku), as DESIGN proposed.
- **Rejections and their fixes:** feature A's EXAMINE could not observe two expectations through an admitted probe; they
  were reworded and the second round accepted. Feature B's first oracle wrote an old Reports path that feature A's sweep
  forbids; it now builds old names at run time. Feature C's first candidate review found that the IL reader skipped
  `catch` clauses, so R-RP5 missed `ReportBulkRemover -> DbUpdateConcurrencyException`; the reader now reads catch types
  for every module rule, the edge is listed, and no other module's list moved.

## Knowingly left open

- `ReportRepository` stays in `Services/Implementation/Repositories` until S05 (repositories by role).
- `Models/Reports` stays in the kernel until S04b and S12 (entities and DTOs).
- Survivors of the mutation run are recorded, not killed. Three are real unit-level gaps: `ReportCommands.cs:131` (name
  length boundary), `:138` (Now-length fallback) and `ReportQueries.cs:44` (per-template cap count). See
  `docs/feature/story-6366-s03h-package-reports/mutation/results.md`.
- The C1 sweep finds old names in constants and literals, not in strings joined at run time from non-constant parts.
- The IL reader still reads no event declarations and no named attribute arguments.

## Commits

- `7180e2959` docs(reports): story 6366 S03h — DESIGN and DEVOPS draft, proposed in AFK mode
- `b9fb0ae41` docs(reports): story 6366 S03h — design review approved, notes folded in
- `314c83d8c` docs(reports): story 6366 S03h feature A — product facts, design and expectations recorded
- `e4d5d28e8` test(reports): oracle — the Reports membership is measured from the IL and fixed in a test
- `2934ea33b` docs(reports): story 6366 S03h feature A — A1 oracle approved
- `ea0f58803` test(reports): oracle — every rule keeps its verdict for the moving Reports types, and no rule string names the old path outside a fixed list
- `67de7662c` docs(reports): story 6366 S03h feature A — A2 oracle approved
- `d942531d7` test(reports): the rules that see Reports also see Modules.Reports
- `480b92574` docs(reports): story 6366 S03h feature A — candidate review accepted
- `019a3bf36` docs(reports): story 6366 S03h feature A — E6 and E7 reworded to what public probes observe
- `a4118a6d2` docs(reports): story 6366 S03h feature A — EXAMINE accepted
- `75807027a` docs(reports): story 6366 S03h feature B — product facts, design and expectations recorded
- `3846b94c5` test(reports): oracle — every moving Reports type lives in its module namespace and folder, and the rename list is empty
- `e4fa3bd20` docs(reports): story 6366 S03h feature B — B1 oracle approved
- `997cedf12` refactor(reports): move Reports into Modules/Reports
- `7da60809d` refactor(reports): using lines, Program.cs and the rename-list strings follow the move
- `00e7ad03c` test(reports): the module-placement oracle names no old path in a literal
- `9d3b3cb93` docs(reports): story 6366 S03h feature B — revised B1 oracle approved
- `351002cb1` docs(reports): story 6366 S03h feature B — B1 GREEN
- `bdadafdfd` docs(reports): story 6366 S03h feature B — B1 slice review accepted
- `fd4a97499` test(reports): oracle — the Reports unit tests sit beside the module they test
- `fc1f4c37d` docs(reports): story 6366 S03h feature B — B2 oracle approved
- `f9bc48517` refactor(reports): move the Reports unit tests beside the module
- `33a98c70f` docs(reports): story 6366 S03h feature B — B2 GREEN
- `fd5a4a4b5` docs(reports): story 6366 S03h feature B — B2 slice review accepted
- `d7a9bf077` docs(reports): story 6366 S03h feature B — candidate review rejected on missing evidence
- `cb65c20ec` docs(reports): story 6366 S03h feature B — whole-candidate review accepted
- `236a6d8e5` docs(reports): story 6366 S03h feature B — EXAMINE accepted
- `a5832cabd` docs(reports): story 6366 S03h feature C — product facts, design and expectations recorded
- `48637339b` test(reports): oracle — the Reports rules name only Modules.Reports
- `59fa8fe53` docs(reports): story 6366 S03h feature C — C1 oracle approved
- `3418effcc` refactor(reports): the Reports rules name Modules.Reports only; the move-time helpers retire
- `79ad634cd` docs(reports): story 6366 S03h feature C — the joint Tracker integration pattern keeps its old Access and Licensing guard
- `017be2a66` docs(reports): story 6366 S03h feature C — design keeps the old Access and Licensing guard in the joint pattern
- `a1f65e064` docs(reports): story 6366 S03h feature C — C1 public oracle keeps its promise links
- `08eac6457` test(reports): the joint Tracker integration pattern names Reports only by the module and keeps the old Access and Licensing guard (oracle)
- `a02d24c01` docs(reports): story 6366 S03h feature C — revised C1 oracle approved
- `ffb8befca` refactor(reports): the joint Tracker integration pattern keeps its old Access and Licensing guard
- `7c11c9733` docs(reports): story 6366 S03h feature C — C1 slice review accepted
- `ce6d79266` test(reports): Reports has its own module rules R-RP1 to R-RP6, read from the IL (oracle)
- `1f863e53c` docs(reports): story 6366 S03h feature C — C2 oracle approved
- `df0f67bc9` docs(reports): story 6366 S03h feature C — candidate review rejected (catch-only EF edge unseen)
- `5ca3f4ccb` test(reports): the IL reader sees catch-clause types, so R-RP5 lists all three EF edges (oracle)
- `af70eba71` docs(reports): story 6366 S03h feature C — revised C2 oracle approved
- `7fbfa31d0` docs(reports): story 6366 S03h feature C — candidate re-review accepted
- `88f02b09e` docs(reports): story 6366 S03h feature C — EXAMINE accepted
- this finalize commit

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no page under `docs/` describes the code layout. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done in this commit: ADR-270 and `ARCHITECTURE.md` §4, as listed above. |
| Usage-data event | N/A, because the Story changes no behaviour a user can use (DEVOPS, 2026-11-21); Reports' existing events (report created, opened, PDF export, bulk) are unchanged. |
| Mutation | Done: Stryker.NET over `Modules/Reports/**`, **90.58 %** (202 killed, 10 survived, 11 no coverage); the 31 moved files are identical to their pre-move selves apart from `namespace` and `using` lines (31 of 31), so the move added no mutant. `docs/feature/story-6366-s03h-package-reports/mutation/results.md`. Frontend N/A, no file changed. |
| Release notes | N/A; written at /release. |

## Gates

- Backend filtered suite (live connector categories left out), after rebasing onto origin: FULL_SUITE.
- Architecture namespace: 1902 passed, 0 failed. `dotnet build Lighthouse.Backend`: 0 warnings.
- Whole-candidate review: features A and B accepted; feature C accepted at the second round.
- EXAMINE: feature A accepted (second round), feature B accepted (second round), feature C accepted (first round; 14818 passed, 0 failed in its own whole run).
