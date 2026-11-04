# Slice 02: the Azure DevOps picker says why

The copy ids F1, F2 and E1 are defined in "Maintainer decisions: simplified copy" at the end of
`feature-delta.md`, which supersedes the earlier copy table.

## Goal

A failing Azure DevOps board read reaches the picker as a sentence carrying what Azure DevOps said, instead of
reading as "no boards".

## In scope

- ADO `GetBoards`, `GetBoardsForProject` (`AzureDevOpsWorkTrackingConnector.cs` lines 637-641, called per project
  from `GetBoardsForProjects`) and `GetBoardInformation` stop swallowing `Exception`; failures throw an
  `AzureDevOpsReadException` carrying F1 or F2. Without the second one a rejected PAT still ends as `200 []`.
- Any project's failed board read fails the whole list, naming the failing project(s) in `{answer}`, with a trailing
  full stop of Azure DevOps' message trimmed. Each failing project keeps its Error-level log line, written inside its
  own task before aggregation. A working project never hides a failing one.
- `GetBoardInformation`'s `Length != 2` guard throws instead of returning an empty `BoardInformation`, which the
  frontend would treat as a read board and enable Confirm.
- A timeout reads `no answer`.
- An empty list stays `200 []` and shows E1.
- Failure-path connector tests: board list refused, board list failing, one project failing among others, board
  information failing, malformed board id.

## Out of scope

Jira (slice 01), Linear, the refresh path, `ValidateConnection` and Test connection for Azure DevOps (unchanged),
any distinction between a 401 and a 403 on the picker, any architecture rule.

## Learning hypothesis

Confirms: ADO fits the same template with no shared helper. Disproved if making the tests pass needs a
cross-connector abstraction (then stop and take it to DESIGN).

## Acceptance criteria

- A refused or failing Azure DevOps shows F1 on the list and F2 on a board, with what it said, and Confirm stays
  disabled.
- With two projects and one failing, the whole list fails, F1 names the failing project, and the other project's
  boards are not shown.
- A malformed board id gives F2, never an empty `BoardInformation`.
- A genuinely empty list shows E1.
- The existing Error-level log for a failed board read is kept.

## Dependencies

Slice 02 builds on slice 01 (E1 in `BoardWizard`), the one allowed direction.

## Dogfood

Open the picker against the demo Azure DevOps org with a revoked PAT, then a valid one.
