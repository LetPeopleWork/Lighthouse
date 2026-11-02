# Slice 02: the Azure DevOps picker says why

Story: US-6499-02. Estimate: about 1 day. Reference class: slice 01 of this Bug.

## Goal

An expired PAT, a failing or unreachable Azure DevOps and a really empty board list each read differently
in the Azure DevOps picker.

## In scope

- ADO `GetBoards`, `GetBoardsForProject` and `GetBoardInformation` stop swallowing `Exception`; failures throw an
  `AzureDevOpsReadException` with a verdict (board-specific factory).
- The second swallow: `GetBoardsForProject` (`AzureDevOpsWorkTrackingConnector.cs` lines 637-641,
  `catch (Exception) { return []; }`), called per project from `GetBoardsForProjects` (lines 610-619). Without
  this a rejected PAT still ends as `200 []`. A failing project must never read as a project without boards.
- One classifier for a credential refusal (`VssUnauthorizedException` or an HTTP 401), shared by the project list, `GetBoardsForProject` and `GetBoardInformation`: it yields the same sentence `ValidateConnection` returns today (C6, Q3). Wherever a project's board read is refused with a 401, the whole list fails with C6.
- **C10, ADO 403**: a 403 on the picker reads the permission sentence (the account or token lacks permission to read boards, naming the project when one is the cause), never C6. An ADO 403 is not a credential verdict (AFK-2 reversed): `ValidateConnection` and Test connection are unchanged for 403 (`connection_failed`, health `Unreachable`), pinned by a test. DELIVER confirms the SDK exception type a 403 raises with a recorded fake before writing the classifier.
- **C11, malformed board id**: `GetBoardInformation`'s `Length != 2` guard throws a typed `AzureDevOpsReadException` (C11) instead of returning an empty `BoardInformation`, which the frontend would treat as a read board and enable Confirm.
- ADO timeout: a `TaskCanceledException` joins the `HttpRequestException` arm of `ValidateConnection` (`connection_failed`, field Url); the board reads treat it as C7 with what ADO said.
- Multi-project list (decided by the maintainer, fail the whole list): a 401 on any project or on the project list fails the whole list with C6; any other per-project failure fails the whole list with C7 naming the failing project(s) (C10 for a 403), and each failing project keeps its Error-level log line. A working project never hides a failing one.
- Other failures yield C7 with what Azure DevOps said (naming the project when one is the cause); empty list stays `200 []` and shows C5 (the Azure
  DevOps line of the per-connector table).
- Failure-path connector tests: board list refused, board list failing, one project failing among others
  (`GetBoardsForProject`), board information failing.

## Out of scope

Jira (slice 01), Linear, the refresh path, validation wording and the `ValidateConnection` mapping of a 403 (both unchanged), any architecture rule.

## Learning hypothesis

Confirms: ADO maps onto the same verdicts with no shared helper. Disproved if making the tests pass needs a
cross-connector abstraction (then stop and take it to DESIGN).

## Acceptance criteria

Scenarios 1 to 5 of US-6499-02 in `feature-delta.md`, plus the DESIGN additions: a 403 reads C10 and never C6, and Test connection for the same 403 is unchanged; a malformed board id gives C11, never an empty `BoardInformation`; an ADO timeout reads as unreachable; a 401 on any project fails the whole list with C6. Multi-project scenario (decided, fail the whole list): with two projects and one failing for a non-credential reason, the picker fails the whole list and C7 names the failing project, and the other project's boards are not shown.

Copy ids gated by the DISTILL sketch walk (AFK-6): C5 (ADO line), C6, C7 (naming the project), C10, C11.

## Dependencies

**Slice 02 depends on slice 01** (the per-connector `BoardWizard` copy table, where ADO gets its own C5
line). That is the one allowed direction: this later slice builds on the finished earlier one. Which exceptions count as "credential" is settled in DESIGN: `VssUnauthorizedException` or an HTTP 401 only; a 403 is C10. The multi-project behaviour is decided (fail the whole list) and blocks nothing.

## Dogfood

Open the picker against the demo Azure DevOps org with a revoked PAT, then a valid one.
