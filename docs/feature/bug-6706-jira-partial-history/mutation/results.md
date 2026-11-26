# Mutation testing — 6706 (Jira: an in-progress item gets no start date when the search returns only part of its history)

Run 2026-11-26 on the worktree at `fa836ed7e` (fix and refactor), kill round on the oracle that adds three cases. Gate
is 80 % kill rate on each stack touched.

| stack | scope | tested | killed + timeout | survived / no coverage | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend, full run (`stryker.6706.backend.json`) | 3 whole files | 1,024 (+97 no coverage) | 778 | 246 | 11 m 33 s |
| Backend, changed lines only (full run) | 25 | 25 | 12 | 13 | |
| Backend, changed lines after the kill round | 25 | 25 | 14 | 11 (all accepted below) | 4 m 51 s |

Frontend: N/A — the fix touches no frontend file.

Stryker.NET's `mutate` takes whole files only, so the full-run score (69.40 %) is dominated by the untouched parts of
`JiraWorkTrackingConnector.cs` (about 4,000 lines). The figures that describe this change are the changed-line rows.
On the changed lines, every mutant that changes behaviour a user or a caller can see is killed; the 11 left are listed
below with the reason each is acceptable.

Configs: `stryker.6706.backend.json` (full run: the three files, the non-live Jira connector tests),
`stryker.6706.backend.killround.json` (kill round: the connector only, the two test classes that cover the changed
lines).

## Backend, changed lines per file

| file | changed lines | tested | killed + timeout |
| --- | --- | --- | --- |
| `IssueFactory.cs` | 134, 150–159 | 5 | 5 |
| `JiraRestRoutes.cs` | 45–49 | 1 | 1 |
| `JiraWorkTrackingConnector.cs` | 2660–2732, 2782–2785, 2958–2959 | 19 | 8 |

### Closed by this pass

- **L2669 `return false` → `return true`** (a search answer whose changelog carries no total): pinned by
  `ItemWhoseSearchAnswerCountsNoEntries_IsDatedFromItWithoutAskingJiraAgain`, which also asserts that an item already
  dated from its search answer causes no extra call to Jira.
- **L2721 `reading.EntriesJiraReported ?? totalChangelogs` → `totalChangelogs`**: pinned by
  `ItemThatChangedBeforeItsWholeHistoryWasRead_StaysMarkedAsNotHoldingItsWholeHistory` — the whole-history read reports
  a larger total than the search answer did, so the item must stay marked partial.
- Also added: `ItemWhoseSearchAnswerHoldsEveryEntryJiraCounts_IsDatedFromItWithoutAskingJiraAgain` (no needless
  re-read when the answer is already whole).

### Accepted survivors

- **Debug logging only (9):** L2675 (negate the guard around a Debug line), L2677 (statement and message), L2716
  (statement and message), L2726 (two mutants of `stillMissing`, which only decides whether to log), L2727 (negate
  that guard), L2729 (the logged total). None changes what is read, merged or dated.
- **L2686 `cancellationToken.ThrowIfCancellationRequested()` removed — equivalent:** the very next call,
  `GetAsync(..., cancellationToken)`, throws for a cancelled token before any request goes out, so the observable
  result is the same. `JiraCancellationGranularityTest` covers the cancelled read on both deployments.
- **L2712 `?? string.Empty` fallback — no coverage:** an issue in a Jira search answer always carries its key; the
  fallback only keeps the compiler's null analysis satisfied.

### Not mutated

Nothing left out: all three changed production files were mutated whole.
