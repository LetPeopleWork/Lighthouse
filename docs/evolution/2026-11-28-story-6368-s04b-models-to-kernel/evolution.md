# Story 6368 — S04b Reduce Models to the kernel: every Models type has an owner and a measured reason to stay

## The problem

`Lighthouse.Backend/Models` was the shared kernel in name only. It held about 330 types, and nobody could say
which of them truly belonged to every context and which had simply not been moved yet. The module rules had to
carry widened patterns ("in Models or in the module") for each type that might move, and nothing showed which
types were free to move.

## What changed

- **A kernel catalogue** (`Lighthouse.Backend.Tests/Architecture/KernelCatalogue.cs`): one row per `Models`
  type. Each row is `Kernel`, or `Owned` by a named context with the blockers that keep it in `Models`. The
  blockers are measured from the compiled code and the EF model, not declared: a row that claims a blocker the
  type is free of fails, and so does a type with no row. Today: 331 rows, 3 `Kernel` (`IDomainEvent`, `IEntity`,
  `IConcurrencyTokenEntity`) and 328 `Owned`.
- **A new measured blocker, "reached from outside its module".** A type that a type outside its owner's module
  reaches (and that does not already reach the owner's Domain), or that any `Contracts` type reaches, would grow
  the owner's boundary allow-lists if it moved. Such a type stays until its owner publishes it in `Contracts`.
  Types that hold only constants get their "held by" blocker from the source text, because the compiler copies
  constants and leaves no IL edge.
- **Types moved to their owner** (behaviour-free; only `namespace` and `using` lines changed):
  - S2: the four usage-data types to `Platform/UsageData`.
  - S3: 13 free files (18 types) and their 5 unit tests to `Modules/<Owner>/Domain` (Access,
    Organisation set-up, Reports, Tracker integration, Work Items).
- **Rules by namespace only** (S4): a sweep fails when any rule pattern of the test project names a moved type by
  its old `Models` name, and the movers and renamed-strings lists are empty. A new law: every `Kernel` row is used
  by two or more contexts.
- **Docs**: `ARCHITECTURE.md` §4 and ADR-270 Decision 7 describe the three-type kernel and point to the catalogue.
  The slice-12 AC 1 and hypothesis are restated as measured. KPI 3 (`Models` `.cs` files): 253 → 236.

Commits: `d54e1d4a8` (feature opened) to `af209fdfe` (final refactor), pushed in push 1 (`50a76c39b..af209fdfe`).
The production moves are `b375e55cf` (S2), `943c0c34c` (S3) and `18d31f5f2` (S4).

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed. No page under `docs/` describes the backend code layout. The architecture docs are under "Context map" below. |
| Screenshots | N/A, because no UI changed. |
| Demo data | N/A, because no behaviour or data a user sees changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no route, DTO or wire shape changed. The moved types keep their public members, which S2 pins by name, and every consumer compiles unchanged apart from its `using` lines. |
| Context map, `ARCHITECTURE.md` | Done in S4 (`18d31f5f2`): `ARCHITECTURE.md` §4 and ADR-270 Decision 7 match the code. The candidate review re-counted the catalogue against the ADR-270 status line, and every number matched. |
| Usage-data event | N/A, because the Story changes nothing a user can use (DEVOPS). |
| Mutation | N/A, by maintainer decision. The production changes are the `namespace` and `using` lines of 17 moved files; no statement, branch or expression changed, so a Stryker run would mutate the same statements that the same unchanged tests faced before the move. Everything else is test code and docs. |
| Release notes | N/A; written at /release. |

## Gates

- `dotnet build Lighthouse.Backend`: 0 warnings, after the rebase onto `origin/main` (`af209fdfe`).
- Test-Fast: 12748 passed, 0 failed, on the rebased tree. Architecture namespace: 2705 passed, 0 failed, on
  `ed3792622` (before the rebase). The full filtered suite was skipped before push 1 by maintainer decision; CI ran it.
- Slice reviews: S1 accepted. S2 accepted after one round: an oracle case now pins the public members, and the
  design wording says moved files may change `using` lines. S3 accepted after the re-scope below. S4 accepted.
  Whole-candidate review: accepted (`f93ea1391`), with two non-blocking notes, listed under "Knowingly left open".
- EXAMINE: accepted (`10d3c9a40`). E1-E5, E7 and E8 met:
  - architecture rules 2708/2708, including the planted falsifiers;
  - EF model unchanged on SQLite and Postgres;
  - platform and access answers identical;
  - full backend suite 15900 passed, 0 failed.

  E6 and E9 were waived by the maintainer as observation gaps (`64faa5959`):
  - E6: the deliveries, metrics, forecast and refresh comparisons are pinned to bases 1269 to 2369 commits old.
  - E9: ARCHITECTURE.md and ADR-270 are outside the examiner's public projection.

  Each waiver names what covers its row instead.

## Decisions taken in AFK mode that the maintainer may revisit

- **S3 moved only the free files (maintainer decision).** The first S3 craft moved all 27 files. About 15 of
  their types are reached from outside their module, and moving them broke the shrink-only boundary rules. The 12
  reached files now stay in `Models` with the measured "reached from outside its module" blocker, and they move
  when their owner publishes them in `Contracts`.
- **How "reached from outside its module" is measured** (engineering choice): a reacher outside `Models` and
  outside the owner's module that does not already reach the owner's Domain, or any `Contracts` type. It is
  computed after the other blockers, and only for rows that have no other blocker. It stands in for "the owner's
  inbound allow-list would have to grow", without reading the allow-lists.
- **`ForecastHorizon` and `ReportRefusalCodes` stay.** Measured: `ForecastHorizon` is held through constants
  named in staying `Models` source, and `ReportRefusalCodes` is reached from outside its module.
- **Push 1 before the candidate review, EXAMINE and finalize** (maintainer decision), to unblock the next work.
  ADO 6368 stayed Active until push 2.
- **Shared oracle files.** `KernelCatalogue.cs` and `ModelsStringsTheMoveRenames.cs` are declared by several
  slices, so each later change went stale against the earlier approvals. S1, S2 and S3 were re-approved before
  the candidate close. Next time, one file per slice's scenarios avoids this.

## Knowingly left open

- **S4 sweep exemption** (candidate note a): a rule pattern that takes in the whole of `Models` is skipped as a
  whole, not alternative by alternative. The source-file sweep still catches a literal old name.
- **The Kernel-rows law lives in the oracle helper** (`ContextsUsing` in `KernelRulesCatchANewOffenderArchUnitTest`),
  not in `KernelRules` (candidate note d). It is enforced on the real assembly and with a planted offender.
- **Design wording facts F386/F183** still say "its catalogue row names the new namespace". The code and the
  acceptance rows say no row names it.
- **The Architecture tests hold about 13 GB.** There are 156 separate Mono.Cecil reads in 91 classes, each kept
  in a static field. Noted on Story 6647 for a cached `ModuleDefinition` per assembly.
- **Next step for the 12 reached files**: their owners publish them in `Contracts` (S12c), which frees their rows.
