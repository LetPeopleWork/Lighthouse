# Mutation run — Bug #5756, a fetch that could not ask the tracker says so

Run 2026-08-13, Stryker.NET, config `Lighthouse.Backend.Tests/stryker-config.bug-5756-fetch-refusal.json`.
Report: `StrykerOutput/2026-08-13.17-57-28/reports/mutation-report.json`.

## What the number is

| Scope | Killed | Survived | NoCoverage | Score |
|---|---|---|---|---|
| The lines this change wrote (Azure DevOps) | 5 | 0 | 0 | **100 %** |
| Both whole connector files | 114 | 81 | 192 | 30.6 % |

The whole-file figure is not the change's score and is not worth reading as one. Stryker.NET
ignores line spans in `mutate`, so the smallest unit it can be pointed at is a file — and these two
files are 1283 and 899 lines of connector, almost none of which this change touched. The
first row is the number that describes the work: the two Azure DevOps fetch methods
(`FetchAdoWorkItemsByQuery`, `FetchAdoWorkItemsById`) and `GetWorkItemReferencesByQuery`, where the
catches were removed and the missing-result-set refusal was added.

## Accepted survivors

All fourteen sit in `LinearWorkTrackingConnector.GetWorkItemsForTeam` / `GetFeaturesForProject`,
lines 58-143 — the bodies of the two methods whose catch blocks were removed. The change deleted a
`catch` and re-indented what was already there; it wrote no logic in either body. None of these
mutants is new, and none of them is about the refusal.

They fall into three groups:

- **The configuration guards** (L58, L66, L76, L121): a team with no Linear identity configured, a
  team name that resolves to nothing, a workspace with no projects. These answer with no records
  deliberately, and whether they should is the open question recorded below — not something a test
  written for this bug should pin either way.
- **The diagnostic counters** (L90-L96, L141-L143): `issuesLinkedToProject`, `issuesWithoutProject`,
  `featuresWithInitiative`. Each is read by exactly one `LogInformation` and by nothing else.
  Killing them means asserting the wording of a summary line, which is narration rather than
  behaviour.
- **The state filter** (L133, `states.Count > 0`): real logic, and covered — by
  `LinearWorkTrackingConnectorTest`, which talks to a live Linear workspace and is therefore
  excluded from this run's test filter along with every other `*Integration` category.

## Still open, deliberately out of scope

Linear answers with no records when the configured team name cannot be resolved
(`LinearWorkTrackingConnector.cs:63-67`), and ServiceNow does the same for a missing query or
work-type configuration. Both reach removal by the same route this bug describes: a team whose
Linear identity was renamed loses every stored Work Item, quietly. Bug #5756 was filed and approved
against the failure catches, and these are configuration guards rather than swallowed failures, so
they were left alone — but the deletion they permit is the same one.
