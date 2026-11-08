# Story 6469 — Backend acceptance scenarios drive the application-service port

**Finished:** 2026-11-08 · ADO User Story #6469 · Epic #6506 "Fast, reliable test suites"

## The problem

Backend acceptance tests started a whole web host to check rules that an application service answers on its own. Each
hosted fixture paid a host build, and each row paid about 100 ms of set-up before its body ran. Nothing stopped a new
scenario from doing the same, so the slow tier grew with every feature.

## What changed

- **`ServicePortHarness`**, a shared test helper: a database of its own per scenario, built from the schema template, and
  the real services behind the port. Up to three collaborators may still be built with `new`.
- **Three kinds of backend acceptance test.** A port scenario under `Tests/Acceptance/<Feature>/`; a skeleton, one or two
  per user flow, on a named list; a wire contract on the new `WireContractTest` base in `Tests/Contracts/`, which pins
  route, status, JSON names, headers and 401/403.
- **`TestHostUseGuardTest`** fails the build for a class that starts a host without a reason. The 330 classes that did on
  its first run sit on `HostUsersStillToMove`, a list that only shrinks.
- **Two families moved.** In the Reality Check family, `Slice04` and 51 rows of `Slice01` run at the port, 15 rows became
  wire contracts, and `Slice06` stays at the host. In Refinement, `NeedOverOneCycleTest` (22 rows) runs at the port, with
  `PortGivensGuardTest` and two checks that the port's set-up matches what the host stores and seeds.
- **The rule is written down:** the `CLAUDE.md` bullet "New backend acceptance scenarios drive the application-service
  port", and a row in `atdd-infrastructure-policy.md`.

## What it achieved

- **Set-up per row:** 94 to 110 ms at the host, 0.4 to 1.0 ms at the port (`Slice04`, each class alone, Release, 3
  rounds, slice A).
- **The Reality Check family:** 24.409 s to 23.001 s, 1.41 s less (each class alone, 3 rounds, TRX plus one-time set-up,
  "After the fold" of step 02-07). The first reading, before the wire classes were folded onto one host, was 2.58 s
  slower; three host builds cost more than the moved rows saved.
- **`NeedOverOneCycleTest`:** in a full run its ids fell from 6.845 s (`P0`) to 3.100 s (`P2`), 3.75 s less, wire rows
  included. Run alone with its new wire class it takes 0.14 s more (6.866 s to 7.009 s, step 05-05), because each class
  run alone pays its own first host build and schema template.
- **No test lost:** every full-run id list (`P0`, `PA`, `PB`, `P1`, `P2`) held all ids of the one before. `P2`: 13,011
  passed, and the host build count stayed at 624.

## Key decisions

- A moved class keeps its namespace, name and test names, so its ids prove nothing was lost.
- A row stays at the host if it pins the wire, has no port, or its body alone is over 2 s.
- No count constant on the guard's lists: a stale entry fails, and an addition shows in the diff.
- Maintainer, 2026-11-07: slices E and F take the smaller option, only the near-all-port classes.
- Maintainer, 2026-11-08: run the slice E box (steps 05-01 to 05-05) as part of T5.
- Maintainer, 2026-11-08: close T5 at the slice E checkpoint. `Slice05`, `Slice10`, `Slice12` and the report-window
  class go to Story 6602 (Continuous Improvement), with the plan and the measurements.
- Maintainer, 2026-11-08: port scenarios take their settings from the `IAppSettingService` stand-in, not the real
  seeders. A check holds the stand-in's answer to a seeded database.
- Maintainer, 2026-11-08: guarantee 6 not met when the class runs alone (+0.14 s) is accepted as a recorded finding.

## Quality

- Reviews: DESIGN rejected once, approved on the second pass; DISTILL the same. Slices A, B and C each came back with
  findings, all fixed or accepted with a reason; the slice C review added 11 planted guard cases. The slice E re-cut and
  the slice E code review were both approved, and the slice E review's guard gaps go to Story 6602.
- Mutation: N/A, because only test files changed and Stryker mutates the product assembly. A deliberate break of one
  product line stood in, in slices B and E: each turned a moved row red at the port, and was reverted.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | `CLAUDE.md` and `atdd-infrastructure-policy.md` updated. Product docs N/A, because nothing a user sees changed. |
| Screenshots, demo data, website | N/A, because no screen, data shape or website asset changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A; release notes are written at /release, not at finalize. |
| Context map, `ARCHITECTURE.md` | Context map: no change, as test structure adds no context, concept, edge or domain event. `ARCHITECTURE.md` §15's test stack line gains the service-port rule, in this finalize commit. |
| Usage-data event | N/A, because nobody using the product reaches a test. |

## Follow-ups

- Story 6602: move `Slice05`, `Slice10`, `Slice12` and `ReportWindowBoundaryScenarios`, and close the slice E review's
  guard gaps (single-input settings check, unstubbed setting reads, nested setters and collection edits the givens guard
  misses).
- The next feature's DISTILL is the first real test of "a new scenario lands at the port".
- 329 classes remain on `HostUsersStillToMove`.
