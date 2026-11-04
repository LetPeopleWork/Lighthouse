# Mutation testing — 6499 (Issue with Jira showing no Boards)

Run 2026-11-04 on the bug's branch @ `3a4d95fe9`, before the rebase. Gate is 80 % kill rate on each
stack touched.

| stack | scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), first run | lines this bug changed | 67.03 % | 91 | 61 | 29 | 1 | 15 m 7 s |
| Backend, after one kill round | lines this bug changed | **89.01 %** | 91 | 81 | 9 | 1 | 19 m 33 s |
| Backend, after one kill round | four whole files | 64.51 % | 1325 | | | | same run |
| Frontend (StrykerJS 10.1.1), first run | changed lines | 82.09 % | 67 | 55 | 11 | 1 | 4 m 10 s |
| Frontend, after one kill round (incremental) | changed lines | **86.57 %** | 67 | 58 | 8 | 1 | 1 m 55 s |

Configs: `stryker.6499.backend.json`, `stryker.6499.frontend.json`, `vitest.stryker.mutation.ts`. Copy
the vitest config into `Lighthouse.Frontend/` for the run; it is gitignored there.

The backend gate is read on the changed lines. Stryker.NET mutates whole files, and these are two
connectors of well over a thousand lines whose untouched code is pinned by the live tests, which the
filter leaves out because they would reach the real Jira and Azure DevOps per mutant. The changed-line
figures come from `mutation-report.json`: every mutant overlapping a line in the diff of `6a789ebef`
to `HEAD`, timeouts counted as killed, ignored and compile errors left out.

## Backend

| file | changed-line mutants tested | survived or uncovered | whole file killed / survived / no coverage |
| --- | --- | --- | --- |
| `AzureDevOpsReadException.cs` | 8 | 0 | 23 / 0 / 0 |
| `AzureDevOpsWorkTrackingConnector.cs` | 29 | 0 | 301 / 112 / 93 |
| `JiraReadException.cs` | 4 | 0 | 44 / 0 / 0 |
| `JiraWorkTrackingConnector.cs` | 50 | 10 | 591 / 237 / 95 |

### Closed by this pass

- Board ids where only one half is a GUID fail as malformed.
- A board with work item types but no states fails as unreadable; a board with only doing or only done
  states stays readable.
- The sentence for two failing projects is pinned exactly, with its separators.
- The verdict codes `board_list_unreadable` and `board_unreadable`, on both connectors.
- Exactly one Error line when Jira cannot be reached, for the list and for a single board.
- The exact technical details of every board read failure, which also proves no raw exception text
  reaches them.

### Accepted survivors

- `JiraWorkTrackingConnector.cs:517, 522, 544, 549`: log message wording.
- `:757` removing `Task.WhenAll`: the three tasks are awaited right after it, so the user sees the same
  failure. What it adds is that a second failure is not left unobserved, which no test can see.
- `:779` both mutants on `?? string.Empty`: forwarding the technical details of a partial read is not
  pinned. The sentence the user reads is; only the diagnostic text behind it could be lost.
- `:1184` and `:1186` (three mutants): without the guard, enumerating a missing `values` array throws
  the same kind of exception, and the outer catch gives the same unreadable answer.

### Cannot occur

- `AzureDevOpsWorkTrackingConnector.cs:436`, states with no work item types: both come from the same
  `StateMappings`, so a state always brings a type. The `||` mutant is still killed by the other half.

## Frontend

| file | mutated lines | tested | survived or uncovered |
| --- | --- | --- | --- |
| `GeneralSettingsComponent.tsx` | 206-215 | 3 | 2 |
| `CreateWizardShell.tsx` | 131-141 | 7 | 1 |
| `WizardPickerButton.tsx` | whole file | 9 | 4 |
| `BoardWizard.tsx` | 20-25, 44, 60, 80-101, 110, 117, 129, 136 | 25 | 1 |
| `DataRetrievalWizardRegistry.ts` | 26-33 | 3 | 0 |
| `useCreateWizard.ts` | 23-30, 170-172, 273-279 | 13 | 0 |
| `DataRetrievalWizard.ts` | 16-33 | 7 | 1 |

### Closed by this pass

- `BoardWizard.tsx:93`: a failed read takes the previous board's information away and disables Confirm.
- `CreateWizardShell.tsx:135`: the picker button follows the selected connection, not the first one.
- `useCreateWizard.ts:274`: Back from Name & Create returns to Configure when no wizard is offered.

### Accepted survivors

- `GeneralSettingsComponent.tsx:214`, `CreateWizardShell.tsx:140`, `WizardPickerButton.tsx:34` (two):
  `sx` and `style` objects.
- `BoardWizard.tsx:91`: `console.error` wording.
- `GeneralSettingsComponent.tsx:210`, `WizardPickerButton.tsx:33` (two): `?.` and the `?? ""` fallback on
  a wizard with no unserved sign-ins, where the tooltip stays empty.
- `DataRetrievalWizard.ts:29`: with an undefined key `includes` is false anyway, so the guard only
  satisfies the type.
