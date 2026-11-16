# Mutation testing — s06 (connector capability interfaces)

Run on 2026-11-16, on frozen code after feature B's whole-candidate review and EXAMINE (HEAD 53bc1ad25).
Config: `stryker.s06.backend.json`.

## Backend

Only two production files carry logic changes: `WorkItemService.cs` and `WriteBackService.cs`. The connector edits
are deletions, and the capability interfaces hold no code, so neither produces mutants.

| Scope | Killed | Survived / no coverage | Score |
|---|---|---|---|
| Both files, whole (Stryker's number) | 303 | 70 | 76.71 % |
| `WorkItemService.cs`, whole | 275 | 73 | 79.0 % |
| `WriteBackService.cs`, whole | 28 | 19 | 59.6 % |
| Lines this Story changed | 3 | 2 (both accepted) | 100 % of the killable ones |

The whole-file numbers are mostly old code, which this Story did not touch. Below 80 % on a whole file is not a
finding of this Story: `mutate` takes whole files only, and those survivors predate the change.

### Closed by this pass

Stryker reported all 30 mutants on the new capability guards (`connector is not IIncrementalSource source ||
!source.SupportsIncrementalSync(…)` at the Team, Portfolio and parent-Feature scans, and the write-back guard) as
CompileError. They introduce a pattern variable, which a mutant cannot rewrite, so Stryker tested none of them. The
guards were checked by hand mutation instead:

- **Deleting the probe check** (`|| !source.SupportsIncrementalSync(…)`) survived every relevant suite on all three
  scans, the acceptance suites included. Jira's own sweep refuses while its deployment is unknown, so the refresh
  fell back to the full path anyway, and no test looked at whether the sweep was called. The gap is older than this
  Story: the old boolean check would have survived in the same way. It is now closed by three tests in
  `WorkItemServiceTest`, each failing on its own mutant:
  - `UpdateWorkItemsForTeam_ConnectorCannotSweepThisConnection_DownloadsTheWholeQueryWithoutSweeping`
  - `UpdateFeaturesForPortfolio_ConnectorCannotSweepThisConnection_DownloadsTheWholeQueryWithoutSweeping`
  - `UpdateFeaturesForPortfolio_ConnectorCannotSweepThisConnection_DownloadsEveryIssueAboveWithoutSweeping`
- **Negating the probe** is killed by the existing tests.
- **The connector without the capability** is pinned by the three full-path cases in `IncrementalSourceCapabilityTest`.

### Accepted survivors

- `WorkItemService.cs` (`IdentityScan.NotScannable`, `Succeeded: false` → `true`): equivalent. `SyncModeResolver`
  returns the full path on "cannot be scanned" before it reads whether the scan succeeded.
- `WorkItemService.cs` (`FeatureSourceFor`, message → empty string): only a log reads the message. The type and the
  point of failure are pinned; the wording is not.
- `WriteBackService.cs`, the type of the write-back guard's exception (hand mutation, `NotSupportedException` →
  `InvalidOperationException`): equivalent for every caller, which catches any exception and fails each update.

### Not mutated

- The connector files: this Story only deleted code there.
- `IWorkTrackingConnector`, `IWriteBackTarget`, `IFeatureSource`, `IIncrementalSource`: interfaces, no mutants.
