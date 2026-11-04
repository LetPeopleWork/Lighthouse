# ADR draft: an Azure DevOps board list fails whole and never reads a failing project as an empty one

Status: Accepted by the maintainer (Bug 6499, decided 2026-11-02: fail the whole list; sentences simplified
2026-11-04). Applied in DELIVER as the next free ADR number under `docs/product/architecture/`. The file name is the
one the first draft proposed.

## Context

`AzureDevOpsWorkTrackingConnector.GetBoardsForProjects` reads the boards of every project in parallel, and
`GetBoardsForProject` swallows any exception into `[]` (the `catch (Exception)` at lines 637-640). Fixing only
the outer `GetBoards` would leave a rejected PAT, or one project that failed, as a list that silently lacks
boards. The board list endpoint answers a bare array, so it cannot carry a "this project could not be read"
notice.

Two sentences carry every failure, with what Azure DevOps said in place of `{answer}`:

- **F1**, a failed board list: "Lighthouse could not read the boards from Azure DevOps: {answer}. Check the
  connection's URL and sign-in details, then try again."
- **F2**, a failed board read: "Lighthouse could not read this board from Azure DevOps: {answer}. Check the
  connection's URL and sign-in details, then try again."

## Decision

1. A per-project board read returns an **outcome** (the boards, or the failure), never `[]` for a failure. The
   aggregate decides. A project with zero boards is an empty contribution, not a failure.
2. **Any failure of any project, or of the project list, fails the whole list with F1.** There is no credential
   branch: a rejected PAT, a 403, a project with Boards disabled and a timeout all take this path, and the `{answer}`
   tells them apart.
3. `{answer}` names each failing project with what Azure DevOps said about it, or `no answer` if it did not answer. A
   trailing full stop in Azure DevOps' message is trimmed, because the sentence adds its own. Azure DevOps' Test
   connection is unchanged.
4. Each failing project is logged at Error level inside its own task, before aggregation, so two failing projects
   leave two lines. A malformed board id in `GetBoardInformation` is a typed `AzureDevOpsReadException` carrying F2,
   with an answer saying the id is not one Azure DevOps gave, not an empty `BoardInformation`: an empty object is
   truthy on the frontend and would enable Confirm.
5. No wire change beyond the `400` the refusal channel already ships, and no new UI state.

Rejected, one line each:

- **Skip non-credential failures while others answer:** the list silently lacks the project, which the admin cannot
  tell from "no boards there".
- **Partial list plus a notice:** needs a new response envelope, a `wizardService` change, a new picker state and a
  wire break on a UI-only endpoint.
- "Fail only when every project fails, otherwise partial and silent": the skip option without the log line.

## Alternatives considered (for the settled part)

- **Keep the per-project swallow.** Rejected: a rejected PAT would still read as `200 []`.
- **A sentence of its own for a refused credential or a 403.** Rejected: about fifteen sentences said the same thing in
  different words, and a template carrying Azure DevOps' own answer tells the reader as much.

## Consequences

- Positive: a refused PAT is one sentence for the whole list, never an empty one. A failing project never reads as
  having no boards, by construction.
- Positive: a malformed board id can no longer enable Confirm with nothing behind it.
- Negative, accepted by the maintainer: one broken project (Boards disabled, a project the token cannot see) blocks
  the picker for every project; the Team's query can still be typed by hand.
- Negative, accepted by the maintainer: F1 and F2 say to check the URL and sign-in details even when Azure DevOps is
  down, because the status sits in the sentence.
- Test pins: a refused list and a refused board give F1 and F2 exactly, through the controller; one of two projects
  failing fails the list naming only it, and the other project's boards are not returned; two failing are both named;
  a project or project list nobody answered reads `no answer`; a project with no boards and an organisation with no
  projects stay an empty list; a malformed board id gives F2; each failing project keeps its Error line.
- DELIVER confirms on the live check what the Azure DevOps SDK raises for a real 401, 403 or timeout. Method and string
  names are suggested; DELIVER may rename them.
