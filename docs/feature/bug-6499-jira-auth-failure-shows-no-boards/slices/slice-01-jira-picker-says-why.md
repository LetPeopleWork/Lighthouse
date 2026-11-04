# Slice 01: the Jira picker says why

The copy ids F1, F2, E1, T1 and D1 are defined in "Maintainer decisions: simplified copy" at the end of
`feature-delta.md`, which supersedes the earlier copy table.

## Goal

A failing Jira board read reaches the picker as a sentence carrying Jira's own answer, instead of reading as
"no boards". A connection whose sign-in method can never list boards does not offer the picker at all.

## In scope

- Jira `GetBoards` and its paging read (`GetBoardsFromJira`), and `GetBoardInformation` (generic `catch`, non-success
  `/configuration`), stop returning empty on failure and throw a `JiraReadException` carrying F1 or F2. No
  classification by status: the answer goes into the sentence. A failure on page 2 fails the whole list.
- Partial board reads: a refused or unusable status list, `board/{id}/issue` or work item type list inside
  `GetBoardInformation` fails the board with F2, instead of returning a board with no states or no types. The existing
  `board_filter_unreadable` verdict stays as it is, the one exception to F2.
- `BoardWizard`: E1 replaces `NO_BOARDS` for every connector; `boards = []` on a failed list; the
  `latestSelection` guard (a superseded board-information answer, success or refusal, changes nothing and does
  not clear the newer selection's spinner). Today's canned fallback lines stay.
- "Select Jira Board" is disabled with tooltip D1 for `jira.scopedtoken` and `jira.oauth` connections, on the
  settings and create screens. The create flow skips the load-data step when no wizard is usable, and Configure
  shows the same disabled button. One frontend place decides, and `JiraOAuth` joins `AuthenticationMethodKeys`.
- Jira Test connection message becomes T1, "Authentication failed for Jira. Check all of this connection's sign-in
  details.", one neutral string for every Jira sign-in. Code, technical detail and
  highlighted field are unchanged.
- Truthful log line for a non-success board status.
- Failure-path connector tests (none exist today), including 401, 403, 503, unreachable, unparseable, page 2 and
  each part of a partial read.
- Docs lines `jira.md:73` and `jira.md:92`, and the ADR-126, ADR-270 and `jobs.yaml` edits (DELIVER finalize).

## Out of scope

Azure DevOps (slice 02), Linear's read path, ServiceNow's failure verdicts, the refresh path, the Jira search
guard, Refresh History, `ValidateConnection`'s status logic (a 503 on Test connection still reads as an
authentication failure: a known defect left out), the field-list text, any architecture rule (Q8), widening who can
open the picker.

## Learning hypothesis

Confirms: the existing channel (controller 400, `refusalOr`, Confirm disabled on null) needs only connector and
copy changes. Disproved if `BoardWizard` or `WizardsController` needs a new path.

## Acceptance criteria

- A refused, failing or unreachable Jira shows F1 on the list and F2 on a board, with Jira's answer in it, and
  Confirm stays disabled.
- A well-formed answer Lighthouse cannot use (a board entry with no id, a `/configuration` with no columns) reads
  `an answer Lighthouse could not read` in F1 or F2, the same as unparseable JSON.
- A refused or unusable status list, `board/{id}/issue` or work item type list gives F2 and Confirm stays disabled.
  Lighthouse never offers a board with no states or no types because a part could not be read. A board that really
  holds no work items yet is still read.
- A board whose saved filter is refused keeps its existing sentence naming the filter.
- A genuinely empty list shows E1 and is not a failure.
- A superseded board-information answer changes nothing.
- Scoped-token and OAuth connections show the button disabled with D1 in its tooltip, on every screen that offers
  it, and make no board call. Other Jira connections are unaffected.
- Test connection on a Jira refusal shows T1 with today's technical detail, for every Jira sign-in.

## Dependencies

None. This slice is the first; slice 02 depends on it.

## Dogfood

Open the picker against a real Jira Cloud site with a deliberately wrong Username, then with the right one. With
a scoped-token connection, check the button is disabled and hover it.
