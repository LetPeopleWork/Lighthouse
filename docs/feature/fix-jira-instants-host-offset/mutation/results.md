# Mutation testing — fix-jira-instants-host-offset

Run 2026-10-16 on the fix branch, before the rebase onto `main`. Gate is 80 % kill rate.

| run | scope | score | story-line mutants | survived | wall clock |
| --- | --- | --- | --- | --- | --- |
| Stryker.NET 5.0.0, first run | `IssueFactory.cs` + `WorkItemService.cs`, whole files | 64.81 % | 43 | 4 | 4 m 46 s |
| Kill round | `WorkItemService.cs`, its own tests | 61.66 % | 32 | 0 | 3 m 43 s |
| **Fix's changed lines, after the kill round** | | **100 %** | **43** | **0** | |

Configs: `stryker.fix-jira-instants.backend.json` (first run), `stryker.fix-jira-instants.backend.kill.json`
(kill round). The whole-file scores are dominated by older code in both files that this fix did not touch;
the gate is judged on mutants on lines changed since `6f859d160`.

Unit tests only (`IssueFactory*`, `WorkItemServiceTest`, `JiraIncrementalSyncTest`,
`ZonedTimestampParsingArchUnitTest`); the acceptance suites were left out per the mutation-testing command,
which is why a run takes minutes. Frontend: N/A, nothing in `Lighthouse.Frontend` changed.

## Closed by the kill round

All four survivors were in the transition reconcile in `WorkItemService.cs`:

- `WithoutCorrections` pairing on From **and** To (`&&` → `||` survived): a move into the same state from
  another state is not taken for a correction.
- The pairing consumes the dropped row (removing the `Remove` survived): one dropped row stands in for only
  one of two moves between the same states.
- The Feature reconcile's whole-history guard (`true ?` survived): a Feature read without its whole history
  never loses a stored transition.
- The Feature reconcile's "still named" check (`Any` → `All` survived): a stored Feature transition the
  whole history still names keeps its row.

## Not mutated

- `JiraWorkTrackingConnector.cs` (about 3300 lines): the fix changed the changelog merge and one copy line.
  Both are pinned by offline tests in `JiraIncrementalSyncTest` (a short re-read is not whole; the flag is
  copied only when every entry reads), each shown to fail with the line removed.
- `WorkItemBase.cs`, `Issue.cs`: one property each, no logic.
