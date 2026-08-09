# Mutation testing — 5505 (`notifyUsers=false` on Jira write-back)

Run 2026-08-09 against the slice-04 working tree. Gate is 80 % kill rate on every stack with changed
files.

| stack | scope | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **the methods slice 04 wrote** | **98.39 %** | 61 | 1 | 0 | ~2 m per pass, 3 passes |
| Backend (Stryker.NET) | all three mutated files, whole | 12.44 % | 96 | 28 | 700 | — |
| Frontend (StrykerJS) | — | **N/A** | — | — | — | — |

**Frontend is N/A, not skipped**: slice 04 changes no file under `Lighthouse.Frontend/`.

Config: `stryker.5505.backend.json` — three files mutated (`JiraWorkTrackingConnector`,
`AzureDevOpsWorkTrackingConnector`, `WriteBackService`), filtered to the six write-back fixtures. Run from
`Lighthouse.Backend.Tests/`; from anywhere else Stryker cannot resolve the test project.

## Read the 98.39 %, not the 12.44 %

Same trap as slice 02, same reason, now across three files: **700 `NoCoverage` mutants** sit in the
whole-file denominator, almost all of them in Jira issue search, board discovery and changelog parsing —
code this slice does not touch and this test filter does not exercise. Stryker.NET still cannot scope to
a line range, so the per-slice figure is computed from `mutation-report.json` by filtering mutants to the
changed methods: `UpdateIssue` / `TryWriteFields` / `Put` (Jira), `UpdateItem` (Azure DevOps),
`WarnAboutWatchersWeCouldNotSpare` / `ProjectOf` / `IsIssueNumber` (WriteBackService).

## Three passes: 59.21 % → 95.16 % → 98.39 %

Every point came from a test that was genuinely missing or genuinely weak. Nothing was disabled to raise
the number — the disables below are all diagnostics, and they were applied in the same pass that added
the tests, so the movement is the tests' doing.

| Pass | Scoped score | What it exposed |
| --- | --- | --- |
| 1 | 59.21 % (45/76) | The project-key derivation was almost untested; the Azure DevOps per-field fallback had no coverage at all |
| 2 | 95.16 % (59/62) | The new Azure DevOps fallback test passed **vacuously** — asserting `Is.All.EqualTo(...)` over a list the mutant had emptied |
| 3 | 98.39 % (61/62) | — |

### Pass 1 → 2: eight mutants, six real gaps

| Mutant | What it exposed | Test |
| --- | --- | --- |
| `separator >= 0`, `separator - 1`, block removal in `ProjectOf` | The warning named "PROJ" whether the derivation worked or not — the assertion matched `unknown project (item PROJ-1)` just as happily | `..._AJiraIssueKey_NamesItsProjectExactly` (3 cases) + `Does.Not.Contain("unknown")` |
| `IsEmpty` negation, two boolean returns, un-negated `IsAsciiDigit` | Only well-formed keys were ever passed in; `PROJ-`, `PROJ-1a` and `-1` were never tried | `..._AReferenceThatIsNotAnIssueKey_IsReportedRatherThanDropped` (4 cases) |
| `", "` → `""` in the project join | `"PROJOTHER"` still contained both project names, so the list separator was free to vanish | assert `"OTHER, PROJ"` |
| unsuppressed URL → `$""` | The retry could have been aimed at the base address and no test would have noticed | `..._SuppressionForbidden_RetriesAgainstTheSameIssue` |
| `throw;` in the cancellation catch (NoCoverage) | Nothing ever cancelled — shutdown could have been reported as a write failure | `..._Cancelled_PropagatesRatherThanReportingAWriteFailure` |
| Azure DevOps: single-field guard, per-field ternary ×3 (NoCoverage), id-not-an-integer message | The Azure DevOps fallback path had no unit coverage at all — slice 02 had assumed there was no seam, but the Moq'd `WorkItemTrackingHttpClient` reaches it | `UpdateItem_TheOnlyFieldIsRefused_DoesNotRetryIt`, `UpdateItem_BatchRefusedThenFieldsAccepted_...`, message assertion |

### Pass 2 → 3: the vacuous assertion

`UpdateItem_BatchRefusedThenFieldsAccepted_ReportsSuppressionPerField` asserted
`results.Select(...) Is.All.EqualTo(Suppressed)`. The statement mutant that deletes `results.Add(...)`
leaves an **empty** list — and `Is.All` over an empty collection passes. The mutant survived while the
test looked green, which is exactly the failure mutation testing exists to find.

Fixed by asserting the count first, and by adding `UpdateItem_OneFieldRefusedOnTheRetry_...`, which needs
the two fields to hold *different* outcomes and so cannot be satisfied by a conditional forced either way.

## Accepted survivor (1)

| Mutant | Why it stands |
| --- | --- |
| `WriteBackService.cs:92` `Order()` → `OrderDescending()` | Descending is an equally canonical order and names the same projects. Precedent: the identical disable in `JointCompletionDistribution.cs:33`. The `// Stryker disable once Linq` comment does not bind here because Stryker reports every Linq mutant in a chained expression at the statement's first line, so the survivor is recorded rather than silenced. |

## Deliberately not behaviour (39 ignored)

All diagnostics: the Jira write-failure `LogDebug` and its `"suppressed"`/`"allowed"` label, the
duplicate-mapping `LogWarning`, the `WriteBackService` error log and its stopwatch, and the
`string.Empty` error messages on success paths — `Written()` does not carry an `ErrorMessage`, so nothing
can read them. Each carries its reason at the call site.

The suppression **warning** text is not in that set. It is the slice's only user-facing output, so its
message, its project list and its remedy are all asserted and all killed.
