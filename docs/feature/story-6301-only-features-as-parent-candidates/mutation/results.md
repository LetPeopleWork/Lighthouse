# Mutation testing — 6301 (Only Features as parent candidates)

Run 2026-10-16 on the story branch, before the rebase onto `main`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived / no cov. | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 72.18 % | 266 | 192 | 74 | 45 m 32 s |
| Backend, whole files, after the kill round | 76.32 % | 266 | 203 | 63 | 47 m 08 s |
| **Backend, this story's changed lines, after the kill round** | **96.75 %** | 123 | 119 | 4 | — |
| **Frontend (StrykerJS 10.0.0)** | **89.19 %** | 37 | 33 | 4 | 1 m 15 s |

Configs: `stryker.6301.backend.json` (the full run), `stryker.6301.backend.kill.json` (the narrowed
re-check of one file), `stryker.6301.frontend.json`, `vitest.stryker.6301.config.ts`.

## Backend

Stryker.NET mutates whole files. Two of the files the story changed, `IssueFactory.cs` and
`IssueExtensions.cs`, are mostly older Jira parsing code (rank, labels, blocked-by links, status
transitions), so the whole-file score is dominated by survivors in code this story did not touch. The
gate is judged on the story's changed lines: every mutant on a line added or changed since
`9170b30a9^`, and every mutant of a file the story created.

| file | whole-file score | story-line mutants | story-line survivors |
| --- | --- | --- | --- |
| `Models/ParentResolution.cs` | 93.9 % | 31 | 1 |
| `Models/ParentPlacement.cs` | 100 % | 4 | 0 |
| `Services/Implementation/Parents/ParentPlacementReport.cs` | 91.2 % (88.2 % narrowed) | 34 | 2 |
| `Services/Implementation/Parents/ParentSourceSelector.cs` | 100 % | 4 | 0 |
| `Services/Implementation/Repositories/FeatureRepository.cs` | 91.7 % | 3 | 0 |
| `Services/Implementation/WorkTrackingConnectors/Jira/IssueExtensions.cs` | 56.3 % | 13 | 0 |
| `Factories/IssueFactory.cs` | 74.3 % | 34 | 1 |

The filter includes the unit tests plus the two acceptance suites of this feature
(`API.Integration.OnlyFeaturesAsParentCandidates`, `API.Integration.ParentFromIssueLinks`). Both were
kept in on purpose: `WorkItemService.cs` is not mutated (below), so they are the only tests that drive
the placement end to end through the Jira connector. They are why a run takes 45 minutes.

### Closed by this pass

Commit `7327b3cb7`, plus one test in the report test added with this file:

- Same-instant history entries: an entry without a history id is read after those with one.
- A history id written as a JSON number is taken as no id rather than failing the issue.
- A link entry without a sentence stands with an empty one.
- A key-shaped `to` is taken whatever the sentence says.
- A sentence naming two keys names no counterpart.
- A changelog `total` that is not a number leaves the history read as whole.
- A link end with no phrase takes no date.
- Two links to the same issue are one candidate, dated by the older link.
- A Local-kind link day is still named by its UTC day.
- A created date read back without a kind is named as stored. Confirmed killed by a narrowed run of
  `ParentPlacementReport.cs` against its own tests (2 minutes). That run's two other no-coverage
  results, the owner strings at lines 20 and 22, are killed by the acceptance suites in the full run.

### Accepted survivors (this story's lines)

- `IssueFactory.cs:151`, removing the guard for a history entry whose date cannot be read. It cannot be
  reached through `CreateIssueFromJson`: the older status-transition parser reads the same `created`
  first and throws on it. That throw predates this story and is listed for the maintainer as a
  separate Bug.
- `ParentResolution.cs:21`, `Count == 0 ? default : …` to always building from the empty list. Equivalent:
  both read as no candidates, and equality compares the candidate keys.
- `ParentPlacementReport.cs:64` and `:83`, the messages of exceptions on switch arms that a placed or
  unplaced record never reaches.

Older code in the same files (63 survivors in the full kill-round run, 58 of them outside this story's lines): rank parsing, labels,
blocked-by links, status-transition parsing and `ParentResolution.GetHashCode`, whose `hash.Add`
mutant leaves a hash that is still consistent with `Equals`. These predate the story and are not
triaged here.

### Not mutated

- `WorkItemService.cs` (1262 lines) and `JiraWorkTrackingConnector.cs` (3278 lines). The story changed a
  few dozen lines in each, and whole-file mutation would bury them under hundreds of mutants in
  untouched code, each tested against the slow acceptance suites. The placement call points they hold
  are covered by the two acceptance suites above.
- `SyncOutcome.cs` is in `mutate` but yields no testable mutant (a record of counts).

### `--with-baseline` does not help a kill round

The kill round was first tried with a disk baseline and `--with-baseline:HEAD`. It tested 0 of 323
mutants, because the since-diff re-tests only source files changed against the committish, and a kill
round changes only tests. In this worktree it also diffed the main checkout. The mutation-testing
command now says to narrow `mutate` and the filter for a kill round instead.

## Frontend

`RefreshHistorySection.tsx`, line ranges of the story's change only. `RefreshLog.ts` is type-only and
produces no mutant.

Accepted survivors:

- `:51`, the `console.error` text when loading fails. The failure look itself is pinned.
- `:281`, three mutants of the loading spinner's `sx` object and strings. Layout only; the spinner's
  presence while loading is pinned.

`git status` was clean after the run.
