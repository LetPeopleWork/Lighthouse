# Slice 01: the Jira picker, Test connection and field list say why

Story: US-6499-01. Estimate: about 1.5 days (the status branch in `ValidateConnection` and the scoped-token/OAuth sentences are included). Reference class: Story 5610 (ServiceNow adopted the same channel).

## Goal

A refused Jira credential, a failing Jira and a really empty board list each read differently in the board
picker, and Test connection and the field list say the same method-neutral credential sentence.

## In scope

- Jira `GetBoards` and its paging read, `GetBoardInformation` (generic `catch`, non-success `/configuration`)
  stop returning empty on failure and throw a `JiraReadException` carrying a verdict.
- One method-neutral credential sentence (C1 as reworded in DESIGN Decision 1: it names the user name or email, the token, and that the account may use Jira, and claims nothing about the token's life) for 401 and 403, used by
  `ValidateConnection` (code `authentication_failed`, no highlighted field), the board list, board
  information and `FieldListRefused`. It replaces today's Test connection text.
- `ValidateConnection` status branch (`JiraWorkTrackingConnector.cs` lines 438-453): today every non-2xx is
  `authentication_failed`, so a 503 reads as an auth failure. Only 401 and 403 keep that code; other statuses
  give C2v with a different code. Consequence: `ConnectionHealthService` stores `AuthenticationFailed` only
  for real refusals; ADR-184's single verdict for credential refusals is unchanged.
- Scoped API token and OAuth connections: board list and board information refusals give C1m; Test
  connection and field-list refusals give C1o. They never get C1 (its email wording would be wrong).
- Jira `ValidateConnection` timeout: a `TaskCanceledException` joins the unreachable arm (`connection_failed`, field Url, C3-style text) via the existing `JiraCouldNotBeAsked`; it no longer reads `validation_failed`. Board reads treat a timeout as C3 too.
- Sentences for other statuses (C2), unreachable/timeout (C3), board configuration (C4, written out in full), and C9 for an unparseable board answer (`JsonException`). Any other exception on a board read is not caught (500, rendered as the frontend fallback).
- `BoardWizard` frontend: `boards = []` on a failed list; the `latestSelection` guard (a superseded board-information answer, success or refusal, changes nothing, and does not clear the newer selection's spinner); the two fallback sentences replacing the canned lines (failure that never came from the backend).
- `BoardWizard` empty-list copy per connector (C5 per connector: Jira, ADO and Linear each name their own
  tracker; ServiceNow unchanged). Slice 02 builds on this table.
- Truthful log line for a non-success board status.
- Failure-path connector tests (none exist today), including 403 on the board list and 503 on `/myself`, and
  frontend copy-table tests.
- Docs lines `jira.md:73` and `jira.md:92` and proposed ADR-126 / ADR-270 / `jobs.yaml` edits (DELIVER finalize).

## Out of scope

Azure DevOps (slice 02), Linear's read path, the refresh path, the Jira search guard, Refresh History,
any architecture rule (Q8), widening who can open the picker.

## Learning hypothesis

Confirms: the existing channel (controller 400, `refusalOr`, Confirm disabled on null) needs only connector
and copy changes. Disproved if `BoardWizard` or `WizardsController` needs a new path.

## Acceptance criteria

Scenarios 1 to 7 of US-6499-01 in `feature-delta.md` (N1 to N3 are answered and folded in), plus the DESIGN additions: C1 as reworded; a timeout on Test connection and on the board reads reads C3, never `validation_failed`; a superseded board-information answer is ignored (`latestSelection`); the two fallback sentences render when the failure never came from the backend; a failure on page 2 of the board list fails the whole list.

Copy ids gated by the DISTILL sketch walk (AFK-6): C1 (reworded), C1m, C1o, C2, C2v, C3, C4, C5, C9 and the two frontend fallbacks.

## Dependencies

None. This slice is the first; slice 02 depends on it.

## Dogfood

Open the picker against a real Jira Cloud site with a deliberately wrong Username, then with the right one; and, if a scoped-token site is at hand, once
with a scoped token to see C1m.
