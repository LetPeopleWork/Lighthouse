# Bug 6706 — Jira: an item gets no start date when the search returns only part of its history

ADO: Bug #6706 (tagged Release Notes, found while fixing Bug #6705 on a Jira Data Center instance). Delivered
2026-11-26, mostly in AFK mode, rigor profile `standard`.

## What was delivered

A Jira Data Center item moved into Doing on 16 May 2025 and, months later, from one Doing state to another. Lighthouse
stored it in progress with no start date. Jira's search answer can hold only the latest entries of an item's
history and says so in `changelog.total`; Data Center dated every item from that partial answer and never read the
rest, because it serves no paged changelog route. Cloud read the whole history only when it had more than 30 entries.

- **Both deployments** now read the whole history whenever the search answer holds fewer entries than its total, and
  date the item from it. Data Center reads `rest/api/2/issue/<KEY>?fields=status&expand=changelog`; Cloud keeps its
  paged changelog route. Only the changelog is merged; every other field still comes from the search.
- An item whose answer is already whole, or carries no total, is not read again.
- A whole-history read that is still partial keeps the item marked as partial, so it never replaces the stored
  transitions, and is logged at Debug with the counts.
- A whole-history read that Jira refuses fails the team's read loudly, as a failed Cloud changelog page already did.

Still open: whether a real Data Center returns the whole history on the issue endpoint. No Data Center instance was
available; the reporter can confirm by comparing `changelog.total` with the number of `histories` on both the search
and the issue endpoint for the same item. If it is partial there too, the item stays marked partial, as before.

## Commits

- `704e3170e`, `d094817a5`, `a20610a0d`, `59a6c57da`, `f8b1d28a8` — the oracle `JiraPartialHistoryDatingTest` (16
  cases, Data Center and Cloud), revised through its reviews and the mutation kill round. RED on the pre-fix
  production: 8 failed, 2 passed (of the 10 cases before the kill round).
- `9f8d67683` — fix: date an item from its whole history when the search holds only part of it.
- `fa836ed7e` — refactor: one owner for re-reading, merging and logging an item's whole history.
- The review, examine and mutation record commits.

Gates: `dotnet build` 0 warnings; non-live Jira connector tests 642/642 (16 of them the oracle); the whole-candidate reviewer's run of 2,615 adjacent tests passed. The full filtered `dotnet test` was still running after 22 minutes on a machine shared with another session's full run, so the maintainer chose to push and let CI run the whole suite. Frontend unchanged.
Whole-candidate review accepted after one revision pass. EXAMINE: no admitted public probe shows a Jira item's
dates, so E1 and E2 were recorded as observation gaps; the maintainer waived both (coverage: the oracle, the
reporter's discriminator, and the maintainer's look at the real app after the push), and the examination closed
accepted on that waiver.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because `docs/concepts/worktrackingsystems/jira.md` does not describe how start and closed dates are read from an item's history. |
| Screenshots | N/A, because nothing a user sees changed. |
| Demo data | N/A, because there is nothing new to show. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A, because no concept, context or edge changed; the fix stays inside the Jira adapter. |
| Usage-data event | N/A, because a bug fix adds nothing new to count. |
| Mutation | Backend: on the changed lines every mutant that changes behaviour is killed (14 of 25 in all); the 11 left only touch Debug logging or are equivalent. Whole files 69.40 %, the rest in untouched connector code. See `docs/feature/bug-6706-jira-partial-history/mutation/results.md`. Frontend N/A. |
| Release notes | N/A; written at /release. |
