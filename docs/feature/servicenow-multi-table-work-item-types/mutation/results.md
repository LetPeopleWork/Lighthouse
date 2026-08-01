# Mutation testing — US 5611 (ServiceNow work item types as record classes)

Run 2026-08-01 against `main` @ `766bebd81`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **88.44 %** | 504 | 454 | 45 | 5 | 8 m 51 s |
| Frontend (StrykerJS 9.6.1) | **85.71 %** | 14 | 12 | 2 | 0 | 17 s |

Configs: `stryker.5611.backend.json`, `stryker.5611.frontend.json`, `vitest.stryker.mutation.ts`.
Backend runs from `Lighthouse.Backend.Tests/`; frontend from `Lighthouse.Frontend/`.

A first backend run scored 88.25 % with 46 survivors; three tests written off that run's triage closed
`ServiceNowTeamQueryVerdict.cs:152`.

## Backend

Per-file, of the 11 files the slice changed:

| file | tested | survived |
| --- | --- | --- |
| ServiceNowWorkTrackingConnector.cs | 234 | 18 |
| WorkTrackingSystemFactory.cs | 95 | 14 |
| DataRetrievalSchemaDto.cs | 76 | 10 |
| ServiceNowTeamQueryVerdict.cs | 47 | 1 |
| ServiceNowHistoryQuery.cs | 11 | 2 |
| ServiceNowReadScope.cs | 15 | 0 |
| ServiceNowWorkItemMapper.cs | 21 | 0 |
| ServiceNowHistoryVerdict.cs | 9 | 0 |
| ServiceNowStateSpanMapper.cs | 9 | 0 |
| TeamSettingDto.cs | 2 | 0 |

`ServiceNowWorkTrackingOptionNames.cs` holds only constants and produced no mutants.

### Closed by this pass

- **`ServiceNowTeamQueryVerdict.cs:152`** — `statusCode != OK || !carriesRecords` survived `||`→`&&`,
  because all four existing cases had the two halves agreeing (400 + no records, 200 + records).
  Under `&&` a 200 whose body is a sign-in page falls through to the count branch and reports
  `class_is_not_a_kind_of_work` off `X-Total-Count` beside a body that was never data. Two cases
  added: a 400 whose error body parses, and a 200 that carries no result set.

### The Link-header pager, examined mutant by mutant

`ServiceNowWorkTrackingConnector.cs:764–853` looked like eight thin spots. Seven are equivalent or
unreachable:

| line | mutant | why it survives |
| --- | --- | --- |
| 806 | `position < len` → `<=` | `IndexOf('<', Length)` is legal and returns −1 |
| 811 | `close < 0` → `<= 0` | `close == 0` is unreachable — `header[open]` is `'<'`, so `close >= 1` |
| 817 | `following < 0` → `<= 0` | same reason |
| 825 | `return (true, …)` → `false` | the caller discards the flag once `nextPage` is non-null |
| 829 | `close + 1` → `close - 1` | still advances, except against a literal `<>` in the header |
| 853 | `return false` → `true` | `nextPage` stays null, so the caller lands on `NoLinks` either way |
| 788 | `\|=` → `^=` | needs two Link header *values* each naming next and each unfollowable |
| 849 | first `\|\|` → `&&` | see below |

`:849` is `!Uri.TryCreate(target, UriKind.Absolute, out var candidate) || candidate.Scheme != …`.
Mutated to `&&` it dereferences `candidate` after a failed parse. It stays alive because the
`!TryCreate` clause is unreachable for every target worth testing: **on Unix, .NET parses a rooted
path as an absolute `file://` URI**, so a proxy-rewritten `/api/now/table/task?…` yields
`TryCreate == true` and is refused one clause later, by the scheme check. Verified by probe, not
inference. Killing it would need a target that fails to parse outright — a shape no measured instance
or intermediary emits.

`ANextPageThatIsNotAnAbsoluteAddress_IsNotFollowedAndTheReadCarriesOnByOffset` was added anyway: the
relative-Link case had no coverage at all, and the `file://` behaviour above is the non-obvious reason
the guard holds.

### Left standing deliberately

- **`ServiceNowWorkTrackingConnector.cs:520`** — forcing the stable-order conditional to `false`
  survives, so nothing proves the `sys_id` tie-breaker is appended. This is **Bug #5621 F3** and is
  being fixed in a parallel worktree.
- **Equivalent or dead (34).** `DataRetrievalSchemaDto`'s survivors are property-initialiser defaults
  that every object initialiser overwrites, plus `WizardHint` strings — the field is never read.
  `WorkTrackingSystemFactory`'s are display and option-name labels.

## Frontend

Scoped to the regions the slice changed: `DataRetrievalSchemaDefaults.ts:56-66` (ServiceNow team
entry), `:104-111` (ServiceNow portfolio entry), `:114-122` (both resolvers).

A first run mutating `useCreateWizard.ts`, `useModifySettings.ts` and the whole schema table
whole-file scored **68.17 %** (531 mutants, 101 survivors). That number measures pre-existing debt,
not this slice: the story changed three lines in each hook — all deletions of `wizardHint`
plumbing — and no spec anywhere asserts the ADO / Jira / Linear / CSV rows of the schema table, which
is where 63 of the 79 survivors in that file came from.

The two survivors in the scoped run are both in the portfolio ServiceNow entry: `key` at `:105`
(unreachable — `inputKind` is `none`) and `isWorkItemTypesRequired: false` at `:109`, which no spec
asserts.

## Configuration notes

Both traps recorded against earlier runs turned out to be misreadings, and are corrected here.

**`13547 mutants created` is the pre-filter count.** Stryker.NET injects mutations into every file
in the project, *then* applies the `mutate` filter, *then* compiles. So a correct, well-scoped run
still prints the whole-project mutant count and still logs compile-error warnings for unrelated files
(`OAuthService.cs`, `JiraWorkTrackingConnector.cs`). The line that tells you the filter worked comes
later: `13043 total mutants are skipped` / `504 total mutants will be tested`. Confirm scope from
`reports/mutation-report.json`, never from the created-count.

**The real cost was the missing `test-case-filter`.** Without one, the initial run executed all 4235
tests under `perTestInIsolation` — about 11 minutes before mutation began. Every other per-feature
config in `Lighthouse.Backend.Tests/` carries one; this was the only one that did not. With the
filter the initial run is 311 tests. `&FullyQualifiedName!~IntegrationTest` is deliberate:
`ServiceNowWorkTrackingConnectorIntegrationTest` hits the live PDI, so letting it into the mutant
loop means a network round-trip per mutant. Excluding it can only depress the score.

**Stryker.NET still ignores line spans; StrykerJS honours them.** The frontend scoping above works.
The `Foo.cs{72..94}` form does not, and fails silently.
