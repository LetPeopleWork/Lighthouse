# ADR draft: an Azure DevOps board list never reads a failing project as an empty one (multi-project policy open)

Status: Proposed (Bug 6499, DESIGN). The fixed part below is settled; the per-project policy (section
"Open choice") is **HELD for the maintainer** (HELD-1 in `feature-delta.md`). Applied in DELIVER as the next free
ADR number under `docs/product/architecture/`, with the open choice filled in. The file name is kept from the
first draft, which proposed failing the whole list; that is now only one of three options.

## Context

`AzureDevOpsWorkTrackingConnector.GetBoardsForProjects` reads the boards of every project in parallel, and
`GetBoardsForProject` swallows any exception into `[]` (the `catch (Exception)` at lines 637-640). Fixing only
the outer `GetBoards` would leave a rejected PAT, or one project that failed, as a list that silently lacks
boards. The board list endpoint answers a bare array, so it cannot carry a "this project could not be read"
notice.

## Decision (settled part)

1. A per-project board read returns an **outcome** (the boards, or the failure), never `[]` for a failure. The
   aggregate decides. A project with zero boards is an empty contribution, not a failure.
2. **A credential refusal on any project, or on the project list, fails the whole list with the credential
   sentence (C6), whichever option below is chosen.** A credential refusal is a 401 (`VssUnauthorizedException`
   or an HTTP 401), the same classification `ValidateConnection` already uses, so the picker and Test connection
   cannot drift.
3. **An ADO 403 is not a credential verdict anywhere.** Test connection keeps today's mapping for it (health
   `Unreachable`); the picker shows a permission sentence (C10), never C6. This follows ADR-184 ("where the
   connector cannot say it was authentication, never say it was") and ADR-126's ServiceNow precedent.
4. Each failing project is still logged (Error level). A malformed board id in `GetBoardInformation` is a typed
   `AzureDevOpsReadException` (C11), not an empty `BoardInformation`: an empty object is truthy on the frontend
   and would enable Confirm.

## Open choice: what a non-credential per-project failure does (HELD-1)

| Option | Behaviour | Consequences |
|---|---|---|
| **A. Fail the whole list** | Any failing project fails the list: C10 for a 403, else C7 naming the first failing project and "and N more". | No wire change, no new UI state; "a failing project never reads as empty" holds by construction (D10, US-02 example 4, UAT 5 as written). Regression: a picker that works today (a PAT that cannot see one project, a project with Boards disabled) becomes unusable for every project; the Team's query can still be typed by hand. |
| **AFK-1. Skip non-credential failures while others answer** | The failing project is left out with a Warning naming it, as long as at least one project answered; if none did, the list fails with C7 (C10 when all were 403). | No regression, no wire or UI change. The list silently lacks the project, indistinguishable by the admin from "no boards there"; D10, US-02 example 4 and UAT 5 hold only when every project fails. |
| **B. Partial list plus a notice** | Boards that answered are shown with a notice naming the projects that did not; none answered: fails as in A. | Honest and no regression. Needs a new response envelope (`{boards, notices}`), a `wizardService` change, a new picker state and its loading/failure/partial sketches, and a wire break on a UI-only endpoint. |

No option is recommended in this draft. Rejected outright: "fail only when every project fails, otherwise
partial and silent" (AFK-1 without the log line and without the credential rule).

## Alternatives considered (for the settled part)

- **Keep the per-project swallow.** Rejected: it is the second swallow (D10); a rejected PAT would still read as
  `200 []`.
- **Treat 403 as a credential refusal on ADO (as Jira N3 does).** Rejected for ADO: no maintainer decision
  covers it, the ADR-184 rule and the ServiceNow precedent say otherwise, and it would move an ADO 403 from
  health `Unreachable` to `AuthenticationFailed`.

## Consequences

- Positive (all options): a refused PAT is one sentence for the whole list, never an empty one; the picker's C6
  and Test connection's text come from one classifier.
- Positive: a malformed board id can no longer enable Confirm with nothing behind it.
- Depends on the open choice: whether one broken project blocks the picker (A), is silently omitted (AFK-1) or
  is reported through a new wire shape (B).
- Test pins, all options: project list unauthorized (C6); a credential refusal on one of several projects (C6
  for the whole list); a 403 on the list (C10); a project with no boards (empty contribution, no error); zero
  projects; a malformed board id (typed failure, C11). Option-specific pins: A, one of two projects failing fails
  the list naming it; AFK-1, one of two failing returns the other's boards plus a Warning naming the skipped
  project, both failing gives C7; B, the envelope and the partial picker state.
- DELIVER confirms the exact SDK exception type an ADO 403 raises with a recorded fake before the classifier is
  written. Method and string names are suggested; DELIVER may rename them.
