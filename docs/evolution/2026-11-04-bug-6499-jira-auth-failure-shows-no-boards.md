# Bug #6499 — A failed board read looked like a connection with no boards

Shipped 2026-11-04 to `main` (`56c36ab54..1aee0a1be`, CI run 41837930274 green). The feature workspace is
`docs/feature/bug-6499-jira-auth-failure-shows-no-boards/`. The design decision is
[ADR-278](../product/architecture/adr-278-azure-devops-board-list-fails-whole.md), and it adopts the refusal channel of
[ADR-126](../product/architecture/adr-126-board-picker-refusal-channel-and-wizard-reach.md).

## What users get

- A Jira or Azure DevOps board list that fails says so, in one sentence with what the tracker answered: *401
  Unauthorized*, *no answer*, or *an answer Lighthouse could not read*. Before, a refused credential read as "No Boards
  available".
- A single board read that fails does the same, including a partial Jira read, and Confirm stays disabled. The previous
  board's information goes away.
- An Azure DevOps project that cannot be read fails the whole list and is named. It never looks like a project with no
  boards.
- A list that really is empty says the account may not have access to any board, or none of its boards can be used.
- **Select Jira Board** is disabled for scoped tokens and Jira Cloud (OAuth), with the reason in a tooltip, because Jira
  does not offer boards to them.
- The Jira Test connection failure names all of the connection's sign-in details.

## How it happened

The connectors caught a failed read and returned an empty list, so a rejected credential, an unreachable tracker and a
real absence of boards all gave the same answer (`rca.md`). ADR-126 had built the channel for a refusal, but only
ServiceNow used it.

## Key decisions

- About fifteen drafted sentences became one template per read, carrying the tracker's own answer. There is no status
  classification on the picker.
- An Azure DevOps list fails whole rather than listing what it could read.
- A partial Jira read fails the board, while a missing status or type id stays a Warning.
- One Error line per failed read.
- ServiceNow's failure verdicts stay as they were; its empty hint gave way to the shared sentence.
- No usage-data event: this reports a failure and adds nothing to be used.

## Work completed

Two slices, eight steps, all with complete DES traces. Slice 01 covers Jira and the picker; slice 02 covers Azure DevOps.
Rigor `standard` for both. Slice 01's review found a 200 list with no `values` read as "no boards". The whole-feature
review found an Azure DevOps board with no columns could be confirmed, malformed ids reached the SDK, and raw exception
text reached the browser in the technical details. All are fixed test-first.

| Gate | Result |
| --- | --- |
| Backend after rebase | 12361 passed, 2 skipped, 2 Licensing failures (environmental in a worktree) |
| Live connectors | Jira 116 of 116 before the final revision; Azure DevOps 100 of 100 after it |
| Mutation | Backend 89.01 % on the changed lines, frontend 86.57 %; see `mutation/results.md` |
| CI | Green, Sonar 0 new issues on both projects |

## Lessons learned

- A crafter twice logged RED without running it. The order had to be spelled out in the prompt.
- The mutation run found real gaps after a clean review: a board with types but no states, the separators in the
  failing-projects sentence, and a stale board staying on screen after a failed read.
- `pnpm build` writes the backend's `wwwroot`, so it cannot run during `dotnet build`.

## Open

- Not checked by hand: wrong email, revoked PAT, the disabled button with a scoped token, a throttled browser.
- The failing-projects sentence has no cap and repeats a message shared by several projects.
- A transport failure shows the HTTP client's text such as "Network Error"; the refresh path can say "Jira answered 200
  (OK)"; a 503 on Test connection is a known defect; the sign-in error still highlights the API token field for OAuth.
- If every status and type id is missing, Jira returns an empty board (a recorded decision).

## Finalize checklist

- Docs prose: `jira.md` and `azuredevops.md` updated.
- Screenshots: N/A, because `jira_wizard.png` and `azuredevops_wizard.png` show the working picker, which did not change;
  the new states are a sentence and a tooltip.
- Demo data: N/A, because the failures need a refused tracker, which demo data does not have.
- Website assets: N/A, because no capability or image changed.
- Clients: N/A, because the wizard endpoints are UI-only.
- ADR-270 glossary gained "Read refusal"; `ARCHITECTURE.md` lists ADR-278; `jobs.yaml` carries the REVISED note.
- Release notes: left for `/release`.
