# Product brief: story\-6366\-s03h\-b\-move

## Request

```text
ADO Story #6366 S03h Package Reports, feature B (move): the 31 Reports files that feature A's membership map (ReportsMembership) lists move into Modules/Reports/Domain, /Application and /Application/Metrics, namespace equal to folder, with their using lines, the Program.cs Reports registrations' full names and every literal on feature A's rename list (ReportsStringsTheMoveRenames) renamed (B1); the 18 Reports unit tests move to Tests/Modules/Reports/Domain, /Application and /Application/Metrics, namespace equal to folder, while the port, host, wire-contract and repository tests stay (B2). Behaviour-free. DESIGN and DEVOPS: docs/feature/story-6366-s03h-package-reports/feature-delta.md; feature A: docs/feature/story-6366-s03h-a-rules-first/.
```

## Work

S

```text
Reports is packaged in Modules/Reports and nothing a user, a tracker or the database sees changes
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every type that feature A's membership map (ReportsMembership) lists as moving lives in its new namespace Lighthouse.Backend.Modules.Reports.Domain, .Application or .Application.Metrics, in the folder of the same name, and in no other namespace; no file of the moving set is left in Services/Implementation/Reports or Services/Interfaces/Reports; the staying Reports files (ReportRepository, Models/Reports, the API controllers, base, DTOs and ReportOwnerWriteAccess) are where they were.
```

### P2

Outcome

```text
Every string on feature A's rename list (ReportsStringsTheMoveRenames) is renamed to the new place or, for the rows that only existed for the old folder, removed, and the list is left with no entry for a moved type; the Program.cs Reports registrations keep their order, lifetimes and service and implementation pairs and change only their using lines and full names; every architecture rule and feature A's preservation law stay green after the move.
```

### P3

Outcome

```text
The 18 Reports unit tests live under Lighthouse.Backend.Tests/Modules/Reports/Domain (ReportWindowPolicyTest, ReportCapPolicyTest), /Application/Metrics (the 5 metric and series-summary tests) and /Application (the other 11, the log-assertion tests together), namespace equal to folder; the Reports port, host and wire-contract tests and ReportRepositoryTest stay where they are, with only their using lines changed.
```

### P4

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: the Reports acceptance, integration and wire-contract tests pass with unchanged assertions (create, edit, read Then and Now, list across owners, bulk create, bulk removal, cap, refusals), the EF model is unchanged with no migration, routes, DTOs and headers are unchanged, CI selects the backend suite for a change under Modules/Reports, and the whole filtered backend suite stays green.
```

## Scope

```text
Moving the 31 Reports production files and the 18 Reports unit tests, namespace equal to folder; using lines across production and tests; the Program.cs Reports registrations' using lines and full names; renaming or removing every literal on feature A's rename list and emptying it of moved entries; scripts, Stryker configs, test-time budgets and classifier scenarios that name a moved file follow it.
```

```text
Narrowing the Reports rule patterns to Modules.Reports only, the new Reports boundary rules, the Reports unit-test placement test and the architecture documents (feature C); ReportRepository (stays); Models/Reports (stays); an AddReports extension (S09a); any change of order, lifetime or pair in Program.cs; any behaviour, route, DTO, migration or frontend change.
```

## Decisions

```text
Rigor standard for feature B: architect, acceptance designer and crafter on sonnet; reviewers and EXAMINE on haiku (DESIGN open question 1, maintainer's take-DESIGN-defaults on 2026-11-24).
```

#### Status

Confirmed


```text
Feature B starts after feature A is accepted (whole-candidate review and EXAMINE, 2026-11-25). The moves go in refactor(reports): move commits apart from edits; the narrowing and the new rules wait for feature C, and B and C are pushed together. Behaviour-free: no review window, UI sketch and loading or failure looks N/A, usage-data event N/A.
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only: architecture-rules (Reports and all), backend-tests with Reports fragments for the unchanged acceptance, integration and wire-contract classes, ef-model-unchanged, test-selection-show for a path under Modules/Reports, and the whole filtered backend-tests run as a background job. No reports-answers-compare command.
```

#### Status

Confirmed


```text
Quint and formal models not relevant: a namespace move of a finite, closed set of types with no state, ordering or concurrency claim.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
B1: the 31 Reports production files live in Modules/Reports/Domain, /Application and /Application/Metrics, namespace equal to folder, nothing of the moving set is left under Services, the rename-list strings and Program.cs full names follow, and every rule stays green.
```

#### Promises

P1

P2

P4

#### Depends on

None.

#### Slice work

S

```text
Reports is packaged in its module and every rule and registration follows it
```

##### Rework

None.

### S2

```text
B2: the 18 Reports unit tests live in Tests/Modules/Reports/Domain, /Application and /Application/Metrics, namespace equal to folder, and the port, host, wire-contract and repository tests stay.
```

#### Promises

P3

P4

#### Depends on

S1

#### Slice work

S

```text
The Reports unit tests sit beside the module they test
```

##### Rework

None.
