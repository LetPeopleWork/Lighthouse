# ADR draft: an Azure DevOps board list fails whole and never reads a failing project as an empty one

Status: Accepted by the maintainer (Bug 6499, HELD-1 decided 2026-11-02: fail the whole list). Applied in DELIVER as
the next free ADR number under `docs/product/architecture/`. The file name is the one the first draft proposed.

## Context

`AzureDevOpsWorkTrackingConnector.GetBoardsForProjects` reads the boards of every project in parallel, and
`GetBoardsForProject` swallows any exception into `[]` (the `catch (Exception)` at lines 637-640). Fixing only
the outer `GetBoards` would leave a rejected PAT, or one project that failed, as a list that silently lacks
boards. The board list endpoint answers a bare array, so it cannot carry a "this project could not be read"
notice.

## Decision

1. A per-project board read returns an **outcome** (the boards, or the failure), never `[]` for a failure. The
   aggregate decides. A project with zero boards is an empty contribution, not a failure.
2. **A credential refusal on any project, or on the project list, fails the whole list with the credential
   sentence (C6).** A credential refusal is a 401 (`VssUnauthorizedException`
   or an HTTP 401), the same classification `ValidateConnection` already uses, so the picker and Test connection
   cannot drift.
3. **An ADO 403 is not a credential verdict anywhere.** Test connection keeps today's mapping for it (health
   `Unreachable`); the picker shows a permission sentence (C10), never C6. This follows ADR-184 ("where the
   connector cannot say it was authentication, never say it was") and ADR-126's ServiceNow precedent.
4. Each failing project is still logged (Error level). A malformed board id in `GetBoardInformation` is a typed
   `AzureDevOpsReadException` (C11), not an empty `BoardInformation`: an empty object is truthy on the frontend
   and would enable Confirm.

5. **Any other per-project failure fails the whole list** (HELD-1, decided by the maintainer 2026-11-02): C10 for a
   403, otherwise C7 naming the first failing project and "and N more". No wire change, no new UI state.

Rejected, one line each:

- **Skip non-credential failures while others answer:** the list silently lacks the project, indistinguishable by
  the admin from "no boards there", against D10, US-02 example 4 and UAT 5.
- **Partial list plus a notice:** needs a new response envelope, a `wizardService` change, a new picker state and a
  wire break on a UI-only endpoint.
- "Fail only when every project fails, otherwise partial and silent": the skip option without the log line and
  without the credential rule.

## Alternatives considered (for the settled part)

- **Keep the per-project swallow.** Rejected: it is the second swallow (D10); a rejected PAT would still read as
  `200 []`.
- **Treat 403 as a credential refusal on ADO (as Jira N3 does).** Rejected for ADO: no maintainer decision
  covers it, the ADR-184 rule and the ServiceNow precedent say otherwise, and it would move an ADO 403 from
  health `Unreachable` to `AuthenticationFailed`.

## Consequences

- Positive: a refused PAT is one sentence for the whole list, never an empty one; the picker's C6
  and Test connection's text come from one classifier. A failing project never reads as having no boards, by
  construction.
- Positive: a malformed board id can no longer enable Confirm with nothing behind it.
- Negative, accepted by the maintainer: one broken project (Boards disabled, a project the token cannot see) blocks
  the picker for every project; the Team's query can still be typed by hand.
- Test pins: project list unauthorized (C6); a credential refusal on one of several projects (C6
  for the whole list); a 403 on the list (C10); a project with no boards (empty contribution, no error); zero
  projects; a malformed board id (typed failure, C11); fail whole: one of two projects failing fails the list with
  C7 naming the failing project (a 403 on it gives C10), and the other project's boards are not returned.
- DELIVER confirms the exact SDK exception type an ADO 403 raises with a recorded fake before the classifier is
  written. Method and string names are suggested; DELIVER may rename them.
