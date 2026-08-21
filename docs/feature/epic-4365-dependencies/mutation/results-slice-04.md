# Mutation testing — Epic 4365, slice 04 (the two dependency settings a Portfolio owns)

Run 2026-08-21. Both stacks clear the project's 80 % floor.

| Stack | First run | After the tests it prompted |
|---|---|---|
| Backend (Stryker.NET) | 89.58 % | **93.75 %** — 45 killed, 3 survived, 0 timeout, of 48 |
| Frontend (StrykerJS) | 59.70 % | **90.30 %** — 121 killed, 12 survived, 1 uncovered, of 134 |

Configs: `stryker.4365.slice04.backend.json`, `stryker.4365.slice04.frontend.mjs`,
`vitest.4365.slice04.ts`. The frontend JSON report is committed beside them
(`stryker-4365-slice04-frontend.json`); the backend one is 13 MB and is not, so its per-file split is
written out below instead.

Run the two stacks **sequentially**. Overlapping them on this machine turns every mutant into a
timeout, and a run of pure timeouts is not a result.

## Backend

Scoped to the five production files the slice changed. The connector is deliberately left out: it is
over three thousand lines and Stryker.NET ignores line ranges, so mutating it whole would bury this
slice's score under untouched code. What the slice added there is a branch and a parse, both covered
by `AzureDevOpsDependencyRelationTest` and by the parse's own unit tests.

| File | Killed | Survived |
|---|---|---|
| `DependencyHonourPolicy.cs` | 25 | 1 |
| `DependencyVerdict.cs` | 7 | 0 |
| `DependencyFieldReferences.cs` | 6 | 0 |
| `FetchFingerprint.cs` | 5 | 1 |
| `DependencyFacts.cs` | 2 | 1 |

`16120 mutants created` is the whole-project pre-filter count and says nothing about scope. The line
that does is `48 total mutants will be tested`. The `Safe Mode!` compile-error warnings for files
nowhere near the list (`Program.cs`, `OAuthService.cs`, the Jira connector) are normal output of a
working run.

### What the first run found, and what it was worth

Three survivors were real, and each named a case somebody will hit. They are now tested:

- **A Feature waiting on one outside the Portfolio was told where that one sits.** Nothing asserted
  that it is not. Saying a Feature sits lower down when the reader cannot reach it at all is an
  instruction to re-order around something invisible.
- **Every Portfolio holding the *waiting* end could veto setting a dependency aside.** Only the
  Portfolios holding *both* ends have a say — a Portfolio that cannot see the other end has nothing to
  act on — and nothing distinguished the two rules, because in every existing scenario the two sets
  were the same.
- **A Feature belonging to no Portfolio read as set aside.** An empty set of deciders was being taken
  as unanimous agreement, reporting a deliberate choice where nobody had made one.

### The three that remain, and why they stay

- `DependencyFacts.cs:35` — the premium-licence flag flipped to `true`. Unkillable by construction:
  the flag is declared so the epic that turns it on adds a rule inside one type rather than a parameter
  through every caller, and **nothing in this epic may read it**. A test that killed this mutant would
  be a test that read it.
- `DependencyHonourPolicy.cs:157` — `First()` to `FirstOrDefault()` on a `GroupBy` result. A group
  always has at least one member.
- `FetchFingerprint.cs:134` — `OrderBy` to `OrderByDescending` inside the helper that renders a
  collection order-insensitively. Any total order does the job, which is the whole point of it.

## Frontend

Scoped with line ranges, which StrykerJS honours (Stryker.NET does not). The two grid files are cut to
the regions that learned about a dependency being set aside; mutating them whole would score the suite
slice 02 already wrote.

| File | Score |
|---|---|
| `models/FeatureDependency.ts` | 100 % |
| `utils/dependencies/dependencySentences.ts` | 100 % |
| `FeatureListDataGrid/WarningsIndicator.tsx` | 92.31 % |
| `ProjectSettings/Advanced/DependenciesComponent.tsx` | 81.82 % |
| `FeatureListDataGrid/columns.tsx` | 78.79 % |

### 59.70 % first time, and the four gaps behind it

- **The warnings column's sort had no test at all.** It reads its own predicate rather than the icons,
  so a row whose only warning is about a dependency has to sort with the warned rows — and a row whose
  dependencies were set aside has to sort with the clear ones, or setting them aside would make every
  row in the Portfolio look like it needed attention.
- **Nothing pinned that the four reasons say four different things.** The existing assertions checked
  for a Feature's name and the phrase about the forecast, both of which are true of the wrong sentence
  too, so any two of them could be swapped with nothing to show for it.
- **The payload schema's defaults were untested.** Every field a payload may omit has to read as the
  harmless answer; any of them arriving as `undefined` would make a row claim something is wrong with a
  dependency that is fine.
- **The settings group** had nothing on rendering before its settings arrive, on staying closed until
  asked for, or on the two sentences that are the entire contract of a field filled in by hand.

### Two of the twelve remaining survivors are false

StrykerJS reports false survivors in this repo. Both test-id mutants were applied **by hand** and each
kills a test:

- `columns.tsx:187` — `data-testid="dependency-set-aside"` emptied. Kills *says on the entry itself
  that a dependency has been set aside*.
- `columns.tsx:163` — the per-row test id emptied. Kills *labels each entry with the Feature the row is
  about*.

So the honest frontend figure is **123 of 134, ~91.8 %**. Everything else left standing is styling
(`sx` objects), React keys, which never reach the DOM, and the unreachable `IgnoredByPortfolio` entry
in the warning-kind lookup — a dependency carrying that reason never reaches that component, which is
the behaviour the slice exists to produce.
