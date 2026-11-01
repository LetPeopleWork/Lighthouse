# Mutation testing — 6453 (Jira: work against the original status and work item type names)

Run 2026-11-01 against `main` @ `f7242a25f`. The gate is an 80 % kill rate on each stack the feature touched. The
frontend is N/A: the feature changed no frontend file.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, lines the feature wrote (`tools/mutation/score_the_slice.py`, baseline `2d324c45e^`) | **89.03 %** | 155 | 138 | 17 | 0 | — |
| Backend, whole files (Stryker.NET 5.0.0) | 71.44 % | 911 | 718 | 183 | 10 | 9 m 47 s |

The first run (before the kill round) scored 87.10 % on changed lines (135/155) and 71.15 % on whole files.

Config: `stryker.6453.backend.json`, with `coverage-analysis: perTest` and a test filter of the Jira connector folder,
the issue factory, the fingerprint tests and `SuggestionsControllerTest`. API integration and acceptance suites are
excluded.

Why the changed-line score is the gate: Stryker.NET mutates whole files, and `JiraWorkTrackingConnector.cs` has 4,000
lines, of which this feature wrote about 440. The whole-file figure is dominated by connector code that this
feature never touched, so it is recorded only for comparison. The maintainer chose this scope.

## Backend, changed lines per file

| file | killed | survived | not run | score |
| --- | --- | --- | --- | --- |
| `API/SuggestionsController.cs` | 2 | 0 | 0 | 100.0 % |
| `Factories/IssueFactory.cs` | 2 | 1 | 0 | 66.7 % |
| `Services/Implementation/WorkItems/FetchFingerprint.cs` | 3 | 0 | 0 | 100.0 % |
| `…/Jira/JiraOriginalNames.cs` | 32 | 2 | 12 | 94.1 % |
| `…/Jira/JiraReadException.cs` | 21 | 0 | 0 | 100.0 % |
| `…/Jira/JiraWorkTrackingConnector.cs` | 78 | 14 | 61 | 84.8 % |

"Not run" counts mutants that do not compile or that Stryker excluded (logging calls).

### Closed by this pass (f7242a25f)

- A list answered with an error status but a valid list body is still refused, for both lists. Before, the
  `IsSuccessStatusCode` check could be dropped unnoticed.
- A list entry with an id but no name leaves its id unknown, and an entry with a name but no id does not break the
  lookup.
- An exception that is not about an HTTP answer carries an empty `HowJiraAnswered`.
- The non-JSON 200 case (a gateway page) is now pinned for the work item type list too, not only the status list.

### Accepted survivors

- `JiraWorkTrackingConnector.cs` re-read bookkeeping (`ListsNotYetReadAgain`, `ForgetForReadingAgain`): `|=` → `^=`
  starting from `None` gives the same flags. `= true` → `false` on the per-connection flags is equivalent because
  only the key's presence is read. Removing the early return when nothing needs reading again only rebuilds the
  same answer from the memo, without an HTTP call.
- `ParseAsNonEmptyArray`: the `catch (JsonException)` emptied to `{}` still returns null. Dropping
  `document.Dispose()` only leaks a buffer.
- Board load: `statusIds.Except(missingIds)` → `Concat` is equivalent, because `StatusNameFor` already answers null for
  a missing id and the loop skips it. The fallback string passed there is therefore unreachable.
- Board issue types: the branch for an issue type without a string id, and the logical mutation around it. Jira
  always sends string ids; only hand-built fixtures lack them.
- `IdOfField` logical mutation on the issue payload shape, again a payload Jira does not send.
- `JiraWorkTrackingConnector.cs:1870` and `IssueFactory.cs:249`: guards for a work item without a state. The page
  check refuses such a page before the factory sees it, so the guard cannot be reached through the public API.
- `JiraOriginalNames.cs` non-array guards (NoCoverage): every caller hands over a list that `ParseAsNonEmptyArray`
  has already checked to be an array.

### Not mutated

- `Issue.cs` (one record member), `IIssueFactory.cs` (one signature) and `JiraRestRoutes.cs` (one route constant)
  hold declarations only, with nothing to mutate.
