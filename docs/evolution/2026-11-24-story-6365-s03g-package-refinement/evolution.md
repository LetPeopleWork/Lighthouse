# Story 6365 — S03g Package Refinement: Refinement in its own module, with its own rules
**Finished:** 2026-11-24 · ADO User Story #6365 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Refinement was already the cleanest context: it depends down only, and nothing but `API` depends on it. But its code still
sat in `Services/Interfaces/Refinement` and `Services/Implementation/Refinement`, beside code of contexts not yet packaged.
The rules that protect it named it by those folders, so they could not tell it from the rest of `Services`, and it had no
direction rules of its own: nothing stopped a new edge from Refinement into the update code, an adapter or the API, and
nothing named the edges it takes into Access and the platform.

## What changed

Three nwdes features on `main`, behaviour-free, no schema change, no route or DTO change. Production code changed only in
`namespace` lines, `using` lines and the eight full type names of the Refinement registrations in `Program.cs`. Design:
`docs/feature/story-6365-s03g-package-refinement/`.

- **Feature A, rules first** (`docs/feature/story-6365-s03g-a-rules-first/`, 2 slices). No production code changed.
  - A1: Refinement's membership (each type, its layer, its old and new namespace, and the port files) was measured from
    the compiled code and pinned in `RefinementMembership` and its test. The layer is decided per file: a file that
    declares no port and whose types reach only the kernel goes to `Domain` (only `NeedBand.cs`); every other file, the
    three port files whole with their records, goes to `Application`.
  - A2: the namespace-move preservation law was lifted out of the platform lane's test into a shared `NamespaceMoveLaw`
    (a prefactoring, its tests unchanged), and every rule pattern that sees a Refinement type also sees it under
    `Modules.Refinement`, proven with made-up types and checked by the law on the subject each rule reads.
- **Feature B, the move** (`docs/feature/story-6365-s03g-b-move/`, 2 slices).
  - B1: the 16 production files moved to `Modules/Refinement/{Domain,Application}` with `git mv`; every one is identical
    to its old self apart from its namespace and `using` lines. `Program.cs` changed only in the eight registration names.
  - B2: the 11 unit tests of the moved types moved to `Tests/Modules/Refinement/{Domain,Application}`; the tests that stay
    in place (the sizing-log repository, controller and acceptance tests) are named with their owner.
- **Feature C, edges and rules** (`docs/feature/story-6365-s03g-c-edges-and-rules/`, 2 slices).
  - C1: every Refinement rule names Refinement by `Modules.Refinement` alone; no rule constant or string literal in the
    test project still names its former `Services` place (`RefinementRulesNameOnlyTheModuleArchUnitTest`). Reports, which
    has not moved, keeps its `Services` alternative. The Refinement preservation law and its rename list retired, because
    their old side no longer exists; `NamespaceMoveLaw` stays for the platform lane.
  - C2: `RefinementModuleBoundariesArchUnitTest` is the eighth module rule set on `ModuleRules`, measured from the
    compiled code, with a falsifier per rule and per list position: R-R1 every type in its layer and none under
    `Services`; R-R2 `Domain` reaches only the kernel; R-R3 and R-R4 outside code reaches `Domain` and `Application` only
    from `API` and the composition root; R-R5 `Application` reaches no EF Core, `Data`, `API`, module adapter, Tracker
    integration or update code; R-R6 it reaches Access, Organisation set-up, the platform and the repositories only along
    14 listed edges. A look-alike namespace check replaces the one the retired law held.
- **Docs, at finalize:** ADR-270 (header "Moved so far", the Refinement row of Decision 1, the new block "Edges measured
  by packaging Refinement"); `ARCHITECTURE.md` §4 (row 8, the diagram node and its Access edge, the new Refinement module
  rules bullet); the Epic's S03g note and the slice brief's IN and acceptance criteria (amended as DESIGN decided). No new
  term enters the glossary.

## Decisions taken in AFK mode that the maintainer may revisit

- **Rigor (no reply in the 15-minute window, 14:16):** feature A `thorough` (opus producers, sonnet reviewers); features
  B and C `standard` (sonnet producers, haiku reviewers).
- **Layer per file, not per type (A1):** `SizingRefusal` reaches `ILogger`, and a per-type rule would have pulled the port
  records into `Domain`. So only `NeedBand.cs` is `Domain`, and the `Domain` rule is one-way.
- **Whole-parent pattern list (A2):** the platform lane's `ModuleNamespaces` pattern sees all of `Modules`, so it newly saw
  Refinement. It is declared as a whole-parent pattern and checked to match `Modules` itself; the law is untouched.
- **Feature A's membership meta-tests became a B1 target (B1):** five of them hard-coded the old namespaces; their made-up
  cases now build on the namespaces the map gives. Assertions are unchanged.
- **EXAMINE rows reworded (B and C), after the Story 6364 precedent:** what no public probe can see (the order and
  lifetimes in `Program.cs`, the source text, the failure wording, the comparison with the earlier rule files) is owned by
  the whole-candidate review, which checked it. B's E2 became E6; C's E1, E4 and E6 became E7 to E9.
- **C's candidate review rejected once, three oracle findings, fixed in one oracle revision:** R-R5's update target now
  includes the update services' interfaces (it had used the update-queue key only); the look-alike check returned; and the
  two pattern-role declarations in `PlatformLanePatternRoles` are a recorded support of the C2 oracle.

## Knowingly left open

- `NamespaceMoveLaw`'s `RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation` carries a platform name although the law is
  shared. Its users are in the approved platform oracle, so it is renamed with the next Story that uses the law.
- One support reason text in `PlatformLanePatternRoles` (`AccessSetUpPlatformAndRepositories`) does not name every part of
  its pattern. Text only; changing it would void the oracle approval, so it goes with the next edit of that file.
- The C1 sweep skips every namespace ending in `Fixtures`, and it reads one literal at a time, so an old form split across
  concatenated literals would escape it. No real rule constant sits in such a namespace today.
- The sizing-log repository pair and `Models/Refinement` stay where they are (S05, repositories by role; S12, entities).

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no page under `docs/` describes the code layout. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done in this commit: ADR-270 and `ARCHITECTURE.md` §4, as listed above. |
| Usage-data event | N/A, because the Story changes no behaviour a user can use (DEVOPS, 2026-11-21); Refinement's existing events are unchanged. |
| Mutation | Done: Stryker.NET over `Modules/Refinement/**`, **97.84 %** (226 killed, 5 survived, 0 no coverage); the 16 moved files are identical to their pre-move selves apart from `namespace` and `using` lines, so the move added no mutant. `docs/feature/story-6365-s03g-package-refinement/mutation/results.md`. Frontend N/A, no file changed. |
| Release notes | N/A; written at /release. |

## Gates

- Backend, filtered full suite: 14683 passed, 2 skipped, 0 failed (on the accepted candidate of feature C).
- Architecture namespace: 1813 of 1813.
- `dotnet build Lighthouse.Backend`: 0 warnings.
- EXAMINE: feature A 12 of 12 met; feature B E1 and E3 to E6 met; feature C E2, E3, E5 and E7 to E9 met.
