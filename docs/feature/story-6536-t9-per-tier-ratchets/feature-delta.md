# Feature: story-6536-t9-per-tier-ratchets

Story #6536 · T9 A tight budget for fast tests, and a ceiling for the slow tiers that only goes down · part of Epic #6506 "Fast, reliable
test suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-50](../epic-6264-modular-monolith/slices/slice-50-t9-per-tier-ratchets.md).

DISCUSS: the brief, settled with the maintainer on 2026-11-07. Its sections "Maintainer decisions", "Changes from T6's DESIGN and DEVOPS",
"Test pyramid vision" and "Changes from T5's DESIGN and DEVOPS" override the text above them. Waves start at DESIGN. Rigor `thorough`.

## Wave: DISCUSS / [REF] Inherited

- Epic decision D-6264-5. job_id: infrastructure-only. Hard dependencies: T6 #6512 (the `category` column, slice F) and T4 #6462 (pushed). Soft: T10 #6537 lowers the ceilings.
- RBAC, Lighthouse-Clients, website, usage-data event, UI sketch, Terminology: N/A, as the brief answers item by item.

## Wave: DESIGN / [REF] Scope, mode and context

Application scope, propose mode, AFK: each engineering choice takes the recommended option and is recorded here. Paradigm OOP, as the
project states; the scripts stay plain Python with a pure core, as they are. No SPIKE, no wave-decisions file.

Read: slices 50 and 51; T6's delta (decisions, the timing CSV, slices, "For the maintainer", its DEVOPS and review revision, both "Maintainer
decisions", push 2); T5's delta (the guard, DEVOPS "Budgets and T9's ceilings", the last review revision); T4's delta (decisions, per-test
guard, the ratchet); ADR-237; `docs/ci-learnings.md`; `check_budget.py`, `trx_to_csv.py`, `vitest_to_csv.py`, `test-time-budget.json`,
`ci_backend.yml`, `ci_frontend.yml`, `ci_changes.yml`, `path-classifier.sh`, `vitest.config.ts`, `src/tests/timeBudget/*`; T6's `evidence/L2.csv`.

**Nothing was run.** This agent has no shell, and no CI artifact was downloaded. **The real `Integration` column does not exist yet**, so the
design fixes rules and shapes, and every figure below is a placeholder until real runs fill it ("The figures").

## Wave: DESIGN / [REF] Project rules, answered

| Rule | Answer |
|---|---|
| Bounded contexts touched (ADR-270) | **None.** No context, concept, edge or event is added; this is CI test tooling. ADR-270 and `ARCHITECTURE.md` §4 stay as they are |
| New terms (ADR-270) | **No domain term.** Engineering words stay out of the glossary: *fast tier*, *ceiling*, *ledger*, *left out*. "Integration" here is the test category, not ADR-270's Work Tracking Integration |
| Domain events | None published or reacted to |
| UI surface (ADR-271), loading and failure looks, visualization skill | N/A, because no user of the product sees anything. The step summary is CI text |
| nWave scenario tags in code | None. The ledger's `runs` are CI run ids, data the check reads, not tags |
| Production code | None changes. Mutation: N/A, because Stryker mutates the product and no product file changes. The deliberate break of each guard stands in, as in T4 (D-6462-20) |
| Ports and adapters | Product hexagon untouched. The check keeps its own small shape: a pure core, a shell that reads, prints and exits |
| ADR | No new ADR. **ADR-237 gets an amendment at finalization** (D-6536-19); this agent may write only this file. `brief.md`: unchanged |
| Outcome registry | Not run: no typed contract is added and this agent runs nothing |
| External integrations | None added, so no contract-test note |
| Improvement backlog check | N/A, because this Story is itself an item of the Continuous Improvement Epic |

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6536-1 | **No `ci*.yml` edit, no new step, no new workflow.** All of it lives in `check_budget.py`, its JSON, and the Vitest reporter | A workflow edit runs every live suite once more on a key shared with CI; both steps already have the inputs |
| D-6536-2 | Frontend fast limit: a second, lower base limit in the reporter for every file that is not `.test.tsx`; `.test.tsx` keeps 2 s; `LIMIT_FACTOR` and the first-test allowance apply to both | Only the reporter knows which test was first in its file, which shuffling now changes per run; the CSV cannot say |
| D-6536-3 | **The frontend does not wait for T6.** Its tier is the file name, so its figure comes from CSVs that already exist, from runs after T12's shuffle landed | The wait in the brief is a backend wait |
| D-6536-4 | Backend fast limit: `check_budget.py` checks every counted row that is not `Integration`, one test at a time. Exceptions sit in a new `fast_allowlist` keyed by **class**, with reason and ceiling, and reuse the stale and not-needed checks | Two classes have unstable test names, so a per-test key would rot; a class key reuses what exists |
| D-6536-5 | A class may be listed only if it holds a slow test in **every** calibration run. A cost that moves **between classes** stays in `L`; one that moves between tests of a listed class is covered by its class entry. At most 10 entries. Lowering the limit to a listed ceiling makes that entry not needed, so it leaves in the same commit | A real cost can be listed; a cost that lands on whichever class goes first would red an innocent class if the limit were set below it |
| D-6536-6 | **What counts:** rows with outcome `Passed`, in a class with no connector category. The 11 `NotExecuted` rows and all live rows are out of count, sum, mean and shares | A zero-second row would lower the mean; a connector push would lift it |
| D-6536-7 | **Connector classes are left out, not whole runs.** The check finds them by scanning the source tree for a class-level attribute naming one of the five connector categories, bound to the class it stands on | A verdict on every run beats a skipped one; judging plain runs only would give no verdict after any connector change. See "Leaving connector classes out" |
| D-6536-8 | Backend `Integration` has two ceilings, both failing the build: **mean seconds per test**, which only goes down, and **total seconds**, which may go up only by a reviewed entry with a reason. **The brief's literal rule, that every feature adding integration tests raises the total, is not met by this shape; that needs the maintainer's ruling, "For the maintainer" point 1** | The maintainer's decision, with one part it cannot deliver |
| D-6536-9 | Each limit is a **ledger**: an ordered list of entries in `test-time-budget.json`, each with seconds, CI run ids and a reason. An entry with 5 or more **distinct** ids is **enforced** and never changes. At most one **provisional** entry, with fewer ids, may sit last. The check refuses a ledger whose enforced mean or fast-limit entries rise, or a provisional one above them | "Only goes down" becomes a pure check with no git access, and the derivation sits next to the number, as ADR-237 asks |
| D-6536-10 | **Headroom** `h` = the measured largest / smallest of that figure over the runs used, with a floor of 1.13. Ceiling = largest seen x `h`, rounded up. **A measured spread over 1.25 is never capped:** the entry stays provisional and the maintainer is asked | The brief asked DESIGN to confirm 1.25 from measurement; none exists. A capped ceiling would sit inside runner noise and red trunk |
| D-6536-11 | **Raising the total** is a fresh derivation over the last 5 plain runs, the red one included, with a reason naming what grew. **Lowering** either ceiling uses the same formula and only applies when lower | A raise by only the feature's own seconds leaves no room for noise, so the next slow runner reds trunk |
| D-6536-12 | **The build fails against the newest enforced entry. A provisional entry only warns, on its own number, and every run prints "provisional, n of 5" as a warning.** More runs mean a re-derived number in the provisional entry, never ids added to an old number. The Story does not close while a limit is provisional | The maintainer does not want the 5-run limit to block progress, and one run must not red trunk. A newer short entry cannot switch off an older enforced one |
| D-6536-13 | No ledger entry: report only. A ledger in force and **no** counted `Integration` row: cannot judge, exit 2 | Slice B can land before the numbers; a column that silently reads all `Unit` must not pass as "mean 0" |
| D-6536-14 | Each run writes count, sum, mean, the limit and the room left to the log and `GITHUB_STEP_SUMMARY`, before the verdict. A failed write warns and changes no verdict | Publishing is not judging. The step runs only after green tests, so a red-test run has no summary |
| D-6536-15 | **Shares, no gate.** Each job prints its own two layers, labelled as not the pyramid shares. The three shares across everything come from a new read-only mode, `check_budget.py --pyramid`, fed the two CSVs of one commit; the E2E scan lives only there | No job holds both CSVs, and joining them needs a `ci*.yml` edit. See "The shares" |
| D-6536-16 | No frontend total, as decided. The frontend report prints its sum and says it does not compare across runs | The Vitest Duration is bimodal; a sum invites the comparison it cannot bear |
| D-6536-17 | `LIGHTHOUSE_BUDGET_WARN_ONLY` softens the new breaches too; a bad ledger or a stale entry is not softened. The frontend limit keeps `LIGHTHOUSE_TIME_BUDGET` | One knob per mechanism, as T4 decided |
| D-6536-18 | Three slices, in "Slices" | Frontend needs nothing from T6; the numbers are data, not code |
| D-6536-19 | ADR-237 amendment at finalization, plus `docs/ci-learnings.md`: the derivation recipe, and line 471, which still says the backend only warns | T4's pattern; the ledger line is wrong today, `ci_backend.yml` already fails |
| D-6536-20 | Peer review run once (`nw-solution-architect-reviewer`, Sonnet): conditionally approved, 0 critical, 2 high; see "Review". A second iteration is left to the orchestrator | Wave gate |

## Wave: DESIGN / [REF] What is judged

| Limit | Stack | Unit | Fails when | Goes | Lives in |
|---|---|---|---|---|---|
| Fast per-test | Frontend | One test in a file that is not `.test.tsx` | Over the fast base limit (x 3 on CI, + 0.31 s if first in its file) | Down | `vitest.config.ts`, exceptions in `slowTests.ts` |
| Fast per-test | Backend | One counted row that is not `Integration` | Over the limit, or over its class's `fast_allowlist` ceiling | Down | `test-time-budget.json` |
| `Integration` mean | Backend | Sum / count of counted `Integration` rows | Over the ceiling | **Only down** | `test-time-budget.json` |
| `Integration` total | Backend | Sum of counted `Integration` rows | Over the ceiling | Up only by a reviewed entry | `test-time-budget.json` |
| Per-file / per-class | Both | T4's, unchanged: 54 s, 34 s, 23 entries | | | `test-time-budget.json` |

The shares are in no row: they are reported, never judged. **The hard rule is time.**

## Wave: DESIGN / [REF] Leaving connector classes out (D-6536-6, D-6536-7)

The five names are the ones `ci_backend.yml` line 106 excludes: `JiraIntegration`, `AdoIntegration`, `LinearIntegration`,
`ServiceNowIntegration`, `GithubIntegration`. The scan lives beside `discover_integration_class_names` in `trx_to_csv.py`, which
`check_budget.py` already imports. Today: 16 attributes on 16 classes in 16 files. What it must get right, each a test:

- **Bind to the class, not the file.** `AzureDevOpsHistoryCutoffTest`, `AzureDevOpsStateTransitionParserTest` and `JiraHistoryCutoffTest` share a file with a live class and are regular; they stay counted.
- **Read the attribute, not a mention.** `ServiceNowWorkTrackingConnectorIntegrationTest.cs` line 22 names the category in a doc comment.
- **Mixed classes leave whole.** `JiraWriteBackTest` and `AzureDevOpsWriteBackTest` carry the attribute on the class, so their offline rows leave with the live ones. That is 3 rows, in no tier here, as in T6's fast filter.
- **A hosted class in a connector folder stays counted:** it carries no connector attribute.
- **Other attributes may stand between.** `[Category]` need not touch the `class` line, unlike what `INTEGRATION_CATEGORY_AT_CLASS_PATTERN` assumes, and a file may hold two namespaces.
- **A class deriving from a connector class inherits the category in NUnit, so it leaves too.** None exists among the 14 classes T6 moved; the scan closes over the base chain T6's slice F reads, and the case is a test.
- **The list cannot drift.** A Python test reads `ci_backend.yml` and fails unless the five names match the workflow's. T6's guard rule 5 keeps a connector class on its base. A sixth connector then reds this test, not a ceiling.
- **`UsageDataCanary` is no sixth name.** CI excludes it from every filter, and T6's guard makes a fixture carrying it carry a connector too, so it never reaches the CSV as counted.

Rejected: a new `category` value (T6: the name keeps one meaning); a fifth CSV column (a shape change for two readers, for what one scan
gives); listing classes in the JSON (a second list to keep in step); judging plain runs only (no verdict after a connector change).

## Wave: DESIGN / [REF] The budget file (D-6536-9, D-6536-11, D-6536-12)

The `backend` section gains one block. Field names are the crafter's; the data is not.

| Item | Holds | Rule the check enforces, as cannot-judge if broken |
|---|---|---|
| Fast-limit ledger | Entries of seconds, run ids, reason | Each enforced entry below the one before |
| Mean ledger | The same | Each enforced entry below the one before. **No path raises it** |
| Total ledger | The same, plus count at derivation | May rise; every entry has a non-empty reason, as an allow-list entry has |
| All three | | Run ids distinct within an entry. At most one provisional entry, last |
| `fast_allowlist` | Class, reason, `ceiling_seconds`, as `allowlist` | Stale if the class is gone; not needed if the ceiling equals the limit |

The ledger catches an appended raise. It does not stop someone rewriting an old row; a reviewer sees that in the diff. Git history would
hold the same facts, but reading it from CI ties a gate to checkout depth and to what a merge did.

**Who moves a ceiling.**

| Event | What happens |
|---|---|
| A slow test makes the mean breach | The test gets faster, or moves down a tier |
| A feature's tests make the total breach | Move them down, or raise: a fresh derivation with a reason. The red run counts as one of the 5 |
| A slow-runner day makes the total breach, nothing grew | The report shows count unchanged. Re-run. If it repeats, `LIGHTHOUSE_BUDGET_WARN_ONLY` and a re-derivation |
| A move down lifts the mean past its ceiling | Removing cheap rows raises a mean, so a good act can red. **The mean has no raise, so this stops and asks the maintainer.** T5's remaining moves stay inside the headroom (about +2 %, below) |
| The runners get slower for good, so a mean or fast limit no longer fits | `LIGHTHOUSE_BUDGET_WARN_ONLY`, then the maintainer decides. Neither ledger has a raise; T4's rule is that a limit rises only by a named decision, and here that means rewriting the ledger by hand, in a reviewed diff |
| T10 finishes | Lowers both, after its 5 runs |

## Wave: DESIGN / [REF] The figures: what is a placeholder, and how it gets filled

**Rule, per limit.** `L` = the largest seen over the runs used. Fast limit: at least 2.1 x `L`, T4's rule, rounded up. Mean and total:
largest x `h` (D-6536-10). Runs used: at least 5, complete, with green tests (a budget step that went red is fine, D-6536-11); for the backend also plain and after
T6's slice F, for the frontend after T12's shuffle, which is on: `.github/actions/build-frontend/action.yml` line 71 passes it as a flag.
The CSV cannot tell which test was first in its file, so frontend `L` includes any cold start, and with the allowance on top the limit is up to 0.31 s loose. T4 accepted the same.
 Run ids are written down, as T4 did. T4's recipe also applies: the set holds both a fast and a slow runner day. On the
frontend, read which group each run fell in from its Vitest Duration (about 650 s or about 1,100 s); five from one group understate `L`.

| Figure | Today | Stand-in, for sizing only | Source of the stand-in | Filled by |
|---|---|---|---|---|
| Frontend fast base limit | **2 s, so it changes nothing until set** | Unknown. At most about 2.1 s from T4's 2.95 s, which mixes `.test.ts` and `.tsx` | T4 | Slice A: `ceil(2.1 x L / 3)` to 0.1 s, `L` the largest `.test.ts` over 5 shuffled runs. The factor 3 turns a CI time into a quiet-machine limit. Too few runs: it stays 2 s, nothing held |
| Backend fast limit | **None, not judged** | About 10.8 s with nothing listed; lower if D-6536-5 lets `ForecastLaneTest` and others be listed | T5: 5.1 s on CI, one run. Local `L2` shows 9.5 s for the same test, under load | Slice C |
| `Integration` mean | **None, report only** | 0.75 s, so a ceiling of 0.85 to 0.94 s | T5's stand-in: three folders, every test in them | Slice C |
| `Integration` total | **None, report only** | 2,068 s, so a ceiling of 2,340 to 2,590 s | The same | Slice C |
| `h` | 1.13, or more if measured | 1.13 | T4: whole backend runs, 2,556 to 2,883 s, 8 runs. **Not measured for the `Integration` sum alone**; container tests, 17 % of test time, swing more (T4: one class 63 to 87 s) | Slice C, from max / min of each figure |
| Backend shares | Unknown | | T6 has not labelled it | First run after slice F |
| Frontend shares | 3,648 fast, 5,857 `.test.tsx` | | The brief, 2026-11-07 | Every frontend run |
| E2E declared | About 56 | About 52 in `tests/specs` by one text search, parametrised specs counted once | This wave; not checked against Playwright | Each `--pyramid` run |

**How slice C fills the backend, without waiting for all of it.** First plain run after T6's slice F: a provisional entry with the run ids so
far; breaches warn (D-6536-12). The warnings are the test of the number. Each later run re-derives that entry's number with its id added. At
the 5th distinct id the entry is enforced and fails. The 5 that enforce it come **after T5's last move push, or after T5 stops at its checkpoint**: T5's moves shift both figures, and a saving
from them is read by class, never from this column.

Why T5's later moves are safe: its 165 rows at 510 ms leave a tier of about 2,750 rows at 752 ms, so the mean rises about 2 %, under the
floor of 1.13, and the total falls. Estimates on the stand-in.

**What the total can and cannot see.** This is the figure's honest size, on the stand-ins. A seconds ceiling has to clear runner noise, so from
the largest run seen it leaves 13 to 25 %: about 270 to 520 s, or **about 360 to 690 tests at the tier's mean**. So:

- It bounds **drift**, not a feature. A single new scenario in a class on T5's list does not trip it, and neither does a normal feature: 20 rows are about 0.7 %.
- It trips when growth has used the room, on whichever feature is last. That feature's raise (D-6536-11) re-derives and buys room again.
- The other limits on that scenario: T4's class ceiling at 2.1 x, T5's CLAUDE.md line, review. The report shows the count against the count at derivation, so growth is visible on every run.
- T5's line "a feature adding host time must raise" holds only as "growth past the room must raise". See "For the maintainer", point 1.

**What neither ceiling sees:** one-time set-up is outside TRX durations, so the cost of a new host never shows here. T2's and T5's guards own
that. The mean also falls if hosted rows are mislabelled `Unit`; T6's anchors in `test_trx_to_csv.py` own that.

## Wave: DESIGN / [REF] The report (D-6536-14, D-6536-15)

Backend, as text in the log and the step summary; wording is DISTILL's.

```
Tier          Tests   Seconds   Mean s   Ceiling                   Room
Fast         10,010     317.0    0.032   per test 10.8 s           largest 5.1 s
Integration   2,750   2,068.0    0.752   mean 0.85 / total 2,340   12 % / 12 %
Left out        275   (connector classes; live rows and their offline neighbours)
Since the total was set: +41 tests
This stack only, not the pyramid shares: 78.4 % fast, 21.6 % integration. No gate.
```

The figures in that sketch are placeholders. The frontend prints the same two tiers, with no ceiling, and one line saying its sum does not
compare across runs.

**The shares.** The three figures need three sources that no job holds together: the frontend CSV, the backend CSV, and Playwright, which runs
in other jobs. So:

- **Each run:** its stack's two layers, in counts and as a share of that stack, labelled so nobody reads them against 90 / 9.5 / 0.5.
- **Across everything:** `check_budget.py --pyramid` with the two CSVs prints the three shares beside the 90 / 9.5 / 0.5 upper limits, exit 0 always, writing nothing. Run at slice C, recorded here, and at T10's end. Same counting code, so the two cannot disagree. E2E is a text scan of `Lighthouse.EndToEndTests/tests/specs`: a count of `test(` calls, not of what ran. Parametrised specs read low, and at 0.25 % that moves nothing.
- **Layers:** fast = frontend not `.test.tsx` + backend `Unit`. Integration = `.test.tsx` + backend `Integration`. E2E = Playwright. A Python test reads `package.json`, so the `.test.tsx` rule and `test:fast` cannot part.

Rejected: a job that downloads both artifacts (a `ci*.yml` edit, one more live run, for a line with no gate; see "For the maintainer", point 2);
a separate non-`ci` workflow on `workflow_run` (a job skipped by path leaves no artifact, and it is a new pipeline for a report).

The review suggested cutting `--pyramid` for size. Kept: it is what makes the maintainer's three shares exist at all, and it reuses the tier counting; only the E2E scan is extra.

## Wave: DESIGN / [REF] Component decomposition

| Component | Path | Change | Contract shape |
|---|---|---|---|
| Tier counting | `Scripts/test-timings/check_budget.py` | NEW functions | Pure, return-only: rows and connector classes in, per-tier count, sum, mean out |
| Fast-test judge | same | NEW function, beside `judge` | Pure: rows, limit, exceptions in, `Finding`s out |
| Ceiling judge | same | NEW function | Pure: tier totals and ledgers in, `Finding`s out; provisional is a field of the finding |
| Ledger reader | same, `load_allowlist` | EXTEND | Read-only; raises `CannotJudge` on a rising or malformed ledger |
| Report | same | NEW function | Pure: returns text. The shell alone writes it |
| `_check` and `main` | same | EXTEND | The shell, **bounded change**: universe is the file `GITHUB_STEP_SUMMARY` names, delta is the report text appended. The path comes in as a parameter, as `warn_only_requested` takes the environment. Order: read, report, judge, exit. `--pyramid` writes nothing |
| Connector scan | `Scripts/test-timings/trx_to_csv.py` | EXTEND | Read-only over a passed root |
| Budget file | `Scripts/test-timings/test-time-budget.json` | EXTEND | Data |
| Check tests | `Scripts/test-timings/tests/` | EXTEND, sibling file as T4 | Planted rows, real tree, shipped file |
| Fast limit | `Lighthouse.Frontend/src/tests/timeBudget/compareToAllowList.ts` | EXTEND | Pure: `limitFor` picks by tier; `ran-far-under-budget` uses the test's own tier |
| Reporter and config | `timeBudgetReporter.ts`, `vitest.config.ts` | EXTEND | Prints both limits; one new constant |
| Docs | `docs/ci-learnings.md`, ADR-237 | MODIFY at finalization | |

Whether the new Python sits in `check_budget.py` or a sibling module is the crafter's call, kept to one entry point.

## Wave: DESIGN / [REF] Driving ports

- `check_budget.py --stack <stack> --csv <csv>`, unchanged, so neither workflow step changes. It is the same command locally on a downloaded artifact.
- `check_budget.py --pyramid`, new, no verdict.
- Vitest's reporter hook, unchanged; runs in `pnpm test` and `pnpm test:fast`.

## Wave: DESIGN / [REF] Driven ports and adapters

| Port | Adapter | Direction |
|---|---|---|
| Timing rows | `test-timings-*.csv`, `load_timings` | Read |
| Limits | `test-time-budget.json`, `load_allowlist` | Read |
| Test source | The tree under `--source-root`; `tests/specs` for `--pyramid` | Read |
| Annotations, verdict | stdout, exit code 0, 1, 2 as today | Write |
| Step summary | The file `GITHUB_STEP_SUMMARY` names; absent locally, so printed only | Write, the only new side effect |

## Wave: DESIGN / [REF] Technology choices

Python 3.12 stdlib, as `check_budget.py` requires; TypeScript in the reporter. No new dependency, no licence question.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | File | Overlap | Decision | Why | Assertion |
|---|---|---|---|---|---|
| `judge`, `Finding`, `breach_annotation` | `check_budget.py` | Name a thing over a limit | EXTEND | Same `::error::` form, a new `limit_kind` | Planted row, exit code, name in the message |
| `load_allowlist`, `_allowlisted`, `_positive_seconds` | `check_budget.py` | Read and refuse limits | EXTEND | Reasons and positive seconds already checked | Malformed file exits 2 |
| `stale_entries`, `declared_classes`, `_is_not_needed` | `check_budget.py` | List hygiene | EXTEND | `fast_allowlist` is keyed by class for this reason | Stale and not-needed cases |
| `warn_only_requested` | `check_budget.py` | The switch | REUSE | | Switch tests, new breach kinds |
| `class_of` | `check_budget.py` | Row to class | REUSE | Handles fixture arguments | Parametrised name row |
| `discover_integration_class_names` | `trx_to_csv.py` | Scan for an attribute | EXTEND | Same walk, five names; binds to the class | File with two classes |
| `compareToAllowList`, `limitFor` | `compareToAllowList.ts` | Per-test limit | EXTEND | Brief's second limit | Boundary per tier, first-in-file |
| `summarise.py` | | Table for people | NOT USED | Reads raw reports, ADR-237 kept it apart from the gate | |
| Reporter's `check_budget.py` overlap | | Frontend per-test | NOT DUPLICATED | The frontend limit stays in one place | |

Nothing is CREATE NEW above function level. `--pyramid` is a mode, not a script, so the counting has one home.

## Wave: DESIGN / [REF] What can lie, and what catches it

| Lie | Effect if unseen | Caught by |
|---|---|---|
| `category` reads `Unit` for hosted rows | Mean and total look fine; hosted rows hit the fast limit | Zero rows: exit 2. Partial: T6's anchors and its one-time id check |
| A new connector category | A force-full run reds the ceilings | The Python test against `ci_backend.yml` |
| Comment mentions a category | A class wrongly left out | Attribute-anchored scan, with a test |
| CSV of a red run | Partial numbers | The step runs only after green; a failed `bail: 1` run gets no check, as ADR-237 |
| A ceiling set from fast runners only | Slow-day red | Both-runner rule, provisional stage, the switch |
| The ledger edited to raise the mean | The ratchet goes | Exit 2 for an appended raise; a rewritten history is the reviewer's |
| A summary file that cannot be written | Report lost | A warning; verdict stands |
| Sharding (T8) | The check sees part of the rows: total and mean silently fall | **Not caught.** T8 must merge the CSVs before this step and re-derive. Owed line |
| A change in worker count or runner | Every figure shifts | Re-derive, as ADR-237 says. Mean has no raise, so a slower fleet stops and asks |
| Docker image pull on a cold runner | Container rows jump | Inside `h` if calibration saw it; provisional warnings show it |
| A check that lost its teeth | Nothing fails | "Test the budget check" plants each breach on every run, before the real check |

## Wave: DESIGN / [REF] C4

```mermaid
C4Context
  title System Context: per-tier ratchets
  Person(dev, "Maintainer or agent", "Adds tests, raises a ceiling in a reviewed diff")
  System(ci, "Verify jobs", "Run the tests, then judge their times")
  System_Ext(gh, "GitHub Actions", "Hosted runners, step summary, artifacts")
  Rel(dev, ci, "Pushes commits to")
  Rel(ci, gh, "Runs on and writes its summary to")
  Rel(gh, dev, "Shows the verdict and the summary to")
```

```mermaid
C4Container
  title Container: per-tier ratchets
  Person(dev, "Maintainer or agent")
  Container(vitest, "Vitest run", "TypeScript", "Fails a fast test over its limit")
  Container(dotnet, "dotnet test", "NUnit, TRX", "Runs the backend suite")
  Container(conv, "trx_to_csv, vitest_to_csv", "Python", "Write the timing CSVs")
  Container(check, "check_budget.py", "Python", "Reports tiers, judges limits")
  ContainerDb(json, "test-time-budget.json", "JSON", "Budgets, allow-lists, ledgers")
  ContainerDb(src, "Test source", "C#, spec files", "Connector attributes, E2E count")
  System_Ext(gh, "GitHub step summary", "Text per job")
  Rel(vitest, conv, "Hands its JSON report to")
  Rel(dotnet, conv, "Hands its TRX to")
  Rel(conv, check, "Feeds timing rows to")
  Rel(check, json, "Reads limits from")
  Rel(check, src, "Scans classes in")
  Rel(check, gh, "Writes count, sum and mean to")
  Rel(dev, json, "Raises or lowers a ceiling in")
```

## Wave: DESIGN / [REF] Slices

| Slice | Days (est.) | Delivers | Needs | Files |
|---|---|---|---|---|
| A | 0.5 | Frontend fast limit; set if 5 shuffled runs exist, else inert at 2 s | 5 shuffled runs for the figure; nothing from T6 or T5 | `timeBudget/*`, `vitest.config.ts`, `slowTests.ts` if a test is listed |
| B | 1 | Tier counting and report on both stacks (0.25); connector scan (0.25); ledgers with their rules, fast-test and ceiling judges (0.25, planted tests included); `--pyramid` and the E2E scan (0.25). **Ledgers ship empty: report only** | T6's slice F on `main` for real numbers; it is safe before, and prints that nothing is labelled yet | `Scripts/test-timings/*` |
| C | 0.25, then waiting on runs | First entries, provisional; fast allow-list; enforced at 5 ids; ADR-237 and `ci-learnings.md`; three shares recorded here | T6's slice F; T5's last move or stop for the enforcing 5 | `test-time-budget.json`, docs |

About 1.75 days of work, against the brief's two days, in three slices rather than two, because the numbers are a wait and not work.

Every push under `Scripts/test-timings/` runs backend, frontend and the tail, and no live suite (`classify_backend`, `ci_changes.yml` line 198),
so each also yields a calibration run. A and B can go in one push. Slice C's JSON edits ride a backend push already going where one exists,
as T5's D-6469-20 does.

**Rollback.** C: drop the entries, and it reports only. B: alone, once C is gone. A: alone. The switch covers a wrong number in the meantime.

## Wave: DESIGN / [REF] For the maintainer

**Point 1 needs your ruling before DISTILL writes the total's scenarios.** It changes a decision of yours, so it is not taken here. It blocks neither DEVOPS, nor slice A, nor the rest of DISTILL. Points 2 to 4 are decided in your absence.

1. **The total cannot see one feature, so "a feature that adds integration tests raises it" holds only for growth past its room.** A ceiling in
   seconds must clear runner noise, which is 13 to 25 %, about 360 to 690 tests at the tier's mean (stand-in figures). So T9 bounds drift, and
   T5's gap, a new scenario in a listed class, stays open for one scenario at a time.
   - **(a) Seconds only (the shape designed here, pending your word).** Your rule that time is the hard rule. A raise every so often, on whichever feature crosses, with the count printed on every run. Bounded drift, no per-feature overhead.
   - **(b) Add a count line beside the total.** Exact, no noise, so every feature adding an `Integration` row edits the JSON with a reason, which is what your decision literally says. Cost: a gate on count, not time; a trivial test pays too; each such push also starts the frontend job, and parallel Stories conflict on the line.
   - Recommendation (a), because you said time is the hard rule, and T5 and T6 both declined count constants. (b) is one field and one scenario, so it can be added later with nothing redone. Say so if the explicit act per feature is what you wanted.
2. **The three shares do not appear on each run.** Each run shows its stack's two layers; the three across everything come from one command on two CSVs, because joining them in CI is a `ci*.yml` edit. If you want them per run, the cheap moment is the next `ci*.yml` edit anyone makes for another reason.
3. **The mean has no raise, by your decision.** A good move down of cheap rows can lift it. T5's stay inside the headroom. If one ever does not, the work stops and asks you.
4. **The backend fast limit will not be very tight.** With nothing listed it lands near 11 s, against a 0.9 ms median. It stops a real wait or a stuck test, not a 1 s test: the real-SQLite Encryption classes run 1.3 to 1.6 s a test, in the fast tier. Listing can lower it only for costs that stay in one class across runs (D-6536-5).

## Wave: DESIGN / [REF] Open for DEVOPS, DISTILL and DELIVER

- **DEVOPS:** the usage-data answer (the brief says N/A); the pushes in the window; T8's owed line.
- **DISTILL:** report wording; the planted cases, one per stack and per ceiling, with "more tests at the same mean pass" up to the total, which waits on point 1. No nWave tags, as CLAUDE.md says.
- **DELIVER, slice C:** `h` from real runs; the fast allow-list by D-6536-5; which runs are slow-runner days on the backend. Whether the backend is bimodal like the frontend is unknown.
- **Not decided here:** who lowers a ceiling between T10 and the next re-derivation. T4 left the same gap for its limits; the room column makes it visible.

## Changed Assumptions

Original, slice-50, Proposals 1: "**Fast-tier per-test budget, both stacks: 2.1 x the largest fast-tier test over at least 5 `main` runs after T6**".
New: the frontend needs runs after T12's shuffle, not after T6. Why: its tier is the file name; shuffling changes which test pays the cold start.

Original, slice-50, Proposals 3: "Ceiling = the largest mean seen over at least 5 runs x 1.25 (an estimate; DESIGN confirms it from the measured max/min of the mean".
New: `h` is the measured max / min, at least 1.13; over 1.25 the entry stays provisional and the maintainer is asked. Why: no column exists to measure, so 1.25 cannot be confirmed; a tighter ceiling is worth having when noise allows.


Original, slice-50, Maintainer decisions: "a feature that adds integration tests raises it in a reviewed diff with a reason".
Proposed, **awaiting the maintainer** (point 1): growth past the ceiling's room raises it. Why: seconds cannot resolve one feature through runner noise.

Original, slice-50, Maintainer decisions: "T9 prints the three shares in its report". New: each run prints its stack's layers, `--pyramid` prints the three. Why: no job holds all three sources.

Original, slice-50, AC: "Both limits derive from at least 5 plain `main` runs". New: a provisional entry with fewer ids may exist and only warns; the Story closes only with 5. Why: the maintainer's word not to hold for the limit; a failing verdict still needs 5.

Original, T5 DESIGN, "Limits": "The backstop is T9's total ceiling, which a feature adding host time must raise in a reviewed diff". New: see point 1.

**Owed to other briefs (not edited here).**

- **T10, Maintainer decisions:** "T10 lowers T9's ceilings in its last commit". That commit must come **after** T10's 5 runs, or its entry has too few ids and drops to a warning. It is a data commit.
- **T10, Proposals 1:** "`category` column T6 slice D writes" reads slice F.
- **T8:** a sharded backend must merge its CSVs before this step, and re-derive.
- **`docs/ci-learnings.md` line 471:** says the backend per-class check only warns; it fails since T4.

## Wave: DESIGN / [REF] Review

One pass by `nw-solution-architect-reviewer` (Sonnet), read-only: conditionally approved, 0 critical, 2 high, 7 medium, 1 low. It confirmed from the code the no-`ci*.yml` claim, the 16 attributes, the three classes sharing a file, the 23 backend entries and the stale `ci-learnings.md` line. It could not confirm any figure, T6's slice F labelling, or that no class inherits a connector attribute. Its priority check reads no data for Integration, as expected.

| Finding | Severity | Resolution |
|---|---|---|
| The total does not meet a settled maintainer decision, and it was recorded as decided | High | Point 1 is now a ruling that gates the total's scenarios; D-6536-8 says so |
| A short entry could switch off enforcement; ids could be padded | High | D-6536-9, D-6536-12: the newest enforced entry fails, a provisional one only warns, ids distinct, an enforced entry never changes, a warning on every run, the Story cannot close provisional |
| Per-run shares read against the pyramid's limits | Medium | Labelled as this stack only. E2E moved out of the per-run report |
| Fast limit has no raise | Medium | Kept down-only, with a row: a slower fleet goes to the maintainer |
| Spread over 1.25 undefined | Medium | D-6536-10: never capped, stays provisional, asks |
| D-6536-5 ambiguous; a lowered limit reds idle entries | Medium | "Between classes"; idle entries leave in the same commit |
| Connector scan edge cases | Medium | Three cases and the `UsageDataCanary` answer added |
| Frontend `L` double-counts the allowance | Medium | Not fixable from the CSV, which cannot tell the first test; stated and accepted, as T4 did |
| Slice A needs T12's shuffle, absent from `vitest.config.ts` | Medium | **Finding wrong:** the shuffle is a flag in `build-frontend/action.yml` line 71. The rule now says so |
| Size against S; cut `--pyramid` | Medium | Slice B re-estimated at 1 day with its pieces named, the Story at about 1.75. `--pyramid` kept, as the only source of the three shares |
| Effect contract undeclared | Medium | `_check` declared bounded change, path injected, `--pyramid` writes nothing |
| Green against the red run in the rule | Low | "Green tests; a red budget step is fine" |

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**The shape in hexagonal terms.** The product's hexagon does not change. No driving adapter, service, port, Model or tracker adapter is touched, and
there is no new external call. The check is its own small hexagon, and it grows inside its pure core.

```
 DRIVING                     PURE CORE (check_budget.py)              DRIVEN
+-----------------------+   +-----------------------------------+   +----------------------------+
| Check test-time       |   | tier counting            [NEW]    |   | timing CSVs          [same]|
| budget step    [same] |-->| fast-test judge          [NEW]    |<--| test-time-budget.json      |
| check_budget --stack  |   | ceiling judge            [NEW]    |   |   + ledgers        [EXTEND]|
|                       |   | ledger rules             [NEW]    |<--| source scan: connector     |
| --pyramid       [NEW] |-->| report text              [NEW]    |   |   classes, E2E count [NEW] |
+-----------------------+   | judge, stale, not needed  [same]  |-->| annotations, exit   [same] |
                            +-----------------------------------+-->| step summary         [NEW] |
+-----------------------+   +-----------------------------------+   +----------------------------+
| Vitest reporter [same]|-->| compareToAllowList                |
|                       |   |   limit by tier        [EXTEND]   |
+-----------------------+   +-----------------------------------+
```

- **Driving adapters:** the workflow steps are unchanged, on purpose; a workflow edit would run every live suite. `--pyramid` is new, for people.
- **Application core:** new pure functions. They take rows and limits and return findings and text, so every rule is tested without a file.
- **Domain:** none in the product sense. The ideas are a tier, a limit, and a ledger of limits.
- **Driven adapters:** CSVs and the JSON as today. New: the scan for connector classes and E2E tests, read-only, and the step summary, the one new write.
- **Unchanged:** every test, `trx_to_csv.py`'s labelling, T4's budgets and T5's and T6's guards.

**The patterns weighed.**

| Pattern | Verdict | Why |
|---|---|---|
| Functional core, imperative shell | **Chosen** | Rules stay pure and cheap to test, and the check already works this way |
| Ratchet by append-only ledger | **Chosen** | "Only goes down" is a check, not a habit, and the derivation sits by the number |
| Single responsibility, one mechanism per question | **Chosen** | The reporter owns the frontend test, the script owns the rest |
| Open/closed on the budget file | **Chosen** | New blocks, and T4's fields and tests stay as they are |
| Fail closed | **Chosen** | A check that sees no `Integration` rows refuses rather than passes |
| Provisional limit that warns | **Chosen** | Keeps work moving, one run cannot red trunk, and it cannot switch off an enforced limit |
| Two ceilings, mean and total | **Chosen, maintainer** | The mean catches slower tests; the total catches growth |
| Count ceiling | Rejected for now, offered | Exact, but it gates count rather than time |
| Run-speed normalisation (total over the fast tier's time) | Rejected | Unproven, and T5's moves shift both sides. The ratio can be read from the CSVs later |
| Reading the previous ceiling from git | Rejected | Ties a gate to checkout depth |
| Skipping the verdict on connector runs | Rejected | Loses the check exactly when a run is most mixed |
| New CSV column or category for live tests | Rejected | Changes a shared shape for what one scan gives |
| Job joining both CSVs | Rejected | A `ci*.yml` edit for a line with no gate |
| Percentile or median as the gate | Rejected | ADR-237: the bar rises with the suite |
| Domain events, outbox, unit of work, optimistic concurrency, value objects, JSON column | N/A | No product model or persistence changes |

## Wave: DEVOPS / [REF] Platform readiness

Existing platform; nothing is deployed. Written 2026-11-07 with the maintainer away, so each choice takes the recommended option and says why.
**No test ran**, because T6 is using the machine, and no file but this one changed. Read: T9's delta, slices 50 and 51, T5's and T6's DEVOPS,
`ci.yml`, `ci_backend.yml`, `ci_frontend.yml`, `ci_changes.yml`, `green-base-run.mjs`, `path-classifier.sh`, `check_budget.py`, the budget file,
the reporter. Read from GitHub, read-only: job and step results of recent `main` runs, two job logs, artifact lists, and the frontend CSVs of five
runs. Every backend figure above stays a placeholder. `environments.yaml` is not written: this wave may edit this file only, as in T5 and T6.

| Topic | Answer |
|---|---|
| Environments | **CI, `ubuntu-latest`:** the only place the limits judge; `Verify Backend` and `Verify Frontend`, Python 3.12 or the check refuses. **Linux, local:** the Vitest limit also fires in `pnpm test` and `pnpm test:fast`, unscaled; `check_budget.py` on a downloaded CSV, and `--pyramid`, print and write nothing. No Docker is needed for any of it. Windows and macOS jobs run no test |
| Pipeline | **No `ci*.yml` edit, no new step, no new workflow, confirmed from the files (D-6536-21).** `Check test-time budget` already passes `--stack`, `--csv`, `--source-root` and `LIGHTHOUSE_BUDGET_WARN_ONLY`; the runner sets `GITHUB_STEP_SUMMARY` itself. `Test the budget check` already runs the Python tests in both jobs, and both have the whole tree, so a test reading `ci_backend.yml` or `package.json` finds it. Both workflows' comments above the step speak only of a class or a file; that goes stale, and is fixed by whoever next edits them, not by an edit of ours |
| Deployment strategy | None: a trunk push to `main`. Rollout is the ledger itself: report only, then provisional, then enforced. Rollback is below |
| Branching | Trunk-based, unchanged. `ci.yml` has no `concurrency`, so each push waits for the one before to end, as in T5 |
| Observability | The log and the step summary of each run, the `::error::` and `::warning::` annotations, and the timing CSVs. No dashboard, no alert: the red run is the alert. A CSV is kept 90 days (run 42372930241 expires 2027-02-05), so a ledger's run ids outlive their data; the calibration table below keeps the figures |
| Mutation | N/A, because Stryker mutates the product and no product file changes. The planted breach of each limit stands in, as in DESIGN. Strategy stays `per-feature` for the project; `CLAUDE.md` is unchanged |
| Usage-data event | **N/A, because nobody using the product reaches a CI budget check, so no event could show use.** `UsageDataEventName` and `docs/settings/usagedata.md` are unchanged (D-6536-30) |
| Coexistence | T4's per-file and per-class budgets, the reporter's `slowTests.ts`, T5's and T6's guards, and `summarise.py` keep working as they are. `dev-test.sh` and `Test-Fast.ps1` do not read the ledger |
| Pre-requisites | T6's slice F on `main` for any backend figure. T5's last move, or its stop, for the enforcing 5. A clean change-detection window before each push (see "What each push triggers"). Slice A needs neither |
| Peer review | Not run: the skill's triggers do not apply (no new target, framework, stack or security change), and the review at the end of DISTILL covers this wave |

**Monitoring contracts.** No `outcome-kpis.md` exists; the outcomes are the brief's.

| Outcome | Instrument | Read when |
|---|---|---|
| A slow fast test fails the build | Reporter (frontend); fast judge (backend); the planted case on every run | Each run |
| `Integration` mean does not creep | Report row, ledger | Each run; at T10's end |
| `Integration` growth is visible | Report row, "since the total was set" | Each run |
| The three shares | `check_budget.py --pyramid`, recorded here | Slice C; T10's end |

## Wave: DEVOPS / [REF] What each push triggers

Read from `ci.yml`, `ci_changes.yml` and `path-classifier.sh`. The tail is `version`, package, Docker, SBOM, `verifysqlite`, `verifypostgres`,
`verifyauth`, the standalone packages and the Windows and macOS checks. Observed on 2026-11-07: backend job 13 to 20 minutes, frontend job 10 to 16.

| Push | Slice | Paths | Backend | Frontend | Live suites | Then |
|---|---|---|---|---|---|---|
| 1 | A and B | `Lighthouse.Frontend/src/tests/timeBudget/`, `vitest.config.ts`, `Scripts/test-timings/` | Plain | Yes | None | Tail. Its run is a calibration run if it is after slice F |
| A alone, if B is not ready | A | `Lighthouse.Frontend/` only | **Skipped** | Yes | None | Tail |
| 2 | C, first entries | `test-time-budget.json` | Plain | **Yes**, this path starts it | None | Tail. Rides push 1 if T6's last run was plain and green, or another backend push; see D-6536-22 |
| 3 | C, enforcing | `test-time-budget.json`, and `slowTests.ts` only if listed | Plain | Yes | None | Tail |
| Any | Finalization | `docs/` | No run | No run | None | Nothing: the path is not in `ci.yml`'s `paths` |

So **two or three code pushes in all**. A and B share a push, as DESIGN allows: a red in slice A shows in `Build Frontend (composite)`, a red in slice B in
`Test the budget check`, so the two stay apart without a second tail.

**"No live suite" holds only if the window is clean, and today it is not.** Change detection diffs from the last run that `isGreenBase` accepts, so a
`ci*.yml`, csproj or `Program.cs` change still in the window forces every live suite, whatever the push touched. Run 42382479502 shows it: a push of a
test scan and docs, with `connector_shared=true` and every connector `true`, because red run 42379254193 left `ci_backend.yml` in the window (base
6dc63d981, 21 commits back). **T6's check applies to each T9 push as it stands:** `git diff --name-only <last green> HEAD`, then each connector and shared
`classify_*`, and a grep for `^.github/workflows/ci.*\.yml`. Any `true` means wait for a green run, or tell the maintainer.

**A calibration run cannot be asked for.** `workflow_dispatch` after a green base finds no change and skips the backend, and re-running a job keeps the run
id, so it is no new id. The 5 come from real backend pushes, T9's own included.

## Wave: DEVOPS / [REF] What goes ahead without 5 runs

The maintainer's word: do not hold work for a 5-run limit where it blocks progress. This is the whole answer.

| Step | Goes ahead on | Effect on a breach | Waits for |
|---|---|---|---|
| Slice A code | Nothing needed | **Fails.** The frontend limit lives in the reporter and has no warn stage | Nothing |
| Slice A figure | **The 5 runs already exist**, see below. Re-read at DELIVER | **Fails at once**, in the test step. `LIGHTHOUSE_TIME_BUDGET=warn` softens every test, not this limit, so it is no provisional stage | Nothing. If the re-read cannot find 5, it stays 2 s, inert |
| Slice B, all of it | Empty ledgers | **Report only**, nothing judged | Nothing; the backend rows read "nothing labelled" before slice F |
| ADR-237, `ci-learnings.md` line 471 | Nothing needed | None; docs start no CI | Nothing. They need no number, so they do not wait for slice C's end |
| First backend entries: fast limit, mean, total | One plain green run after slice F; **T5 need not be finished** | **Warn only**, on their own number, with "provisional, n of 5" on every run | Slice F |
| `--pyramid`, the shares | The first plain run after slice F | Reported, never judged | Slice F |
| T10 starting | The report and the CSVs; provisional entries are enough | None | T5 and T6, as its brief says, not T9's 5 |
| `fast_allowlist` entries | **Not on a provisional entry.** D-6536-5 needs a class slow in every calibration run, which fewer than 5 cannot show | | The enforcing push |
| **Enforcing push** | **5 distinct eligible ids after T5's last move or stop** | **Fails** | The only step that waits. It blocks nothing but T9's closing |

So only one thing waits: T9 stays Active with slice C open while a limit is provisional (D-6536-12), and the chain moves on to T10 past it. If T5 stops late, the
cost is a warning on every run, not a held Story.

**Slice A's figure, read today (D-6536-23).** Five green shuffled frontend runs exist. In the table, "largest" is the largest test in a file not ending `.test.tsx`.

| Run | Vitest Duration | Largest `.test.ts` | Rows |
|---|---|---|---|
| 42369233738 | 587 s | 0.622 s | 9,505 |
| 42370581769 | 491 s | 0.475 s | 9,505 |
| 42370793984 | 454 s | 0.449 s | 9,505 |
| 42372930241 | 777 s | 0.664 s | 9,505 |
| 42379254193 | 598 s | 0.726 s | 9,505 |

`L` = 0.726 s, so `ceil(2.1 x L / 3)` to 0.1 s is **0.6 s, or 1.8 s on CI**, against 6 s today and DESIGN's stand-in of about 2.1 s. Run 42379254193's backend went
red, its frontend job did not, so its frontend CSV counts ("Reading a run"). Run 42366736330 has no seed line, so the shuffle starts at 42369233738.

- **One file sets it.** Every top test of every run is in `src/layerBoundaries.enforcement.test.ts`, and which one moves between runs. The next file's largest is 0.167 s. Without that file the rule gives 0.2 s, or 0.6 s on CI. Taken here: **0.6 s, nothing listed**, because the rule says so and it is the simple start. The limit only goes down, so listing that file's slow tests later costs one frontend push. See "For the maintainer", point 2.
- **The risk is local, not CI.** The base limit is unscaled on a developer's machine, and the factor 3 is T4's assumption, not measured for this file. Two sessions at once could red `pnpm test:fast` where CI is fine. DELIVER reads that file's local time from one `pnpm test:fast` when the machine is free, before pushing.
- The set holds one slow day at most: 777 s against 454 to 598 s. DESIGN's "about 650 or about 1,100 s" no longer describes the runs, which T12 and T13 changed, so the check that the set holds both is read as "the slowest run seen is in it".

## Wave: DEVOPS / [REF] Reading a run, so the ledger does not learn a bad one

Three facts make this a rule and not a habit, each read from GitHub:

- **The red run's CSV exists.** `Extract` and `Upload` carry `if: always()`. In run 42379254193 `Test Backend` failed and `test-timings-backend` was still uploaded. "The step runs only after green" protects the verdict, not the derivation.
- **One run id can hold two CSVs.** Run 41904560367 was re-run, and lists `test-timings-backend` twice, under the same id and name (artifacts 13360697176 and 13362801269). A ledger's run id alone does not say which one was read.
- **A run's conclusion means nothing here.** Most `main` runs sit at `waiting` on the deploy gate with no conclusion, and a red `verifyauth` marks a run failed with a sound backend. Picking runs by a run-level `success` would skip them, and would reject good ones.

**The rule (D-6536-25).** The unit is one job in one attempt. A CSV may enter a derivation only if, in that attempt, the test step succeeded (`Test Backend`, or `Build Frontend (composite)`), it has no `Failed` row, and, for the backend, all connector and `connector_shared` answers in Detect Changes are `false` and it is after slice F. The enforcing 5 must also be after T5's last move or stop.

| What happened | Backend CSV counts? | Verdict that run | What to do |
|---|---|---|---|
| `PortfolioUpdaterTest` red, or any one flake, on a plain run | **No, not that attempt** | None: the budget step is skipped | Read the name, as `ci-learnings.md` says. Re-run the failed job once: no live cost, and a green re-run closes the window, as `isGreenBase` reads the latest attempt. **The green attempt counts**, under the same id, with its artifact id written down. Red twice is a real failure, handed to whoever owns the test; T9 quarantines nothing |
| The same on a force-full run | **No, in any attempt:** it is not plain | None | Do **not** re-run for T9's sake: it spends the shared keys for an id that cannot count. Whether to re-run to close the window is the pushing Story's call |
| `verifyauth` red because `traefik` could not be pulled from Docker Hub, backend green | **Yes.** It is another job, and it is not in `isGreenBase`, so it holds no window either | Stands | Nothing for T9. Re-run it if a Release is wanted, as a red there skips Release |
| `verifysqlite` or `verifypostgres` red for Docker Hub, backend green | **Yes** | Stands | The window stays open, as both must succeed. Re-run the job |
| Docker Hub fails inside `Verify Backend`: Testcontainers pulls `postgres:18-alpine` | **No:** the container fixtures go red, so `Test Backend` does | None | As a flake. Check the log for the pull error; `Integration/Containers` red with no pull error is the known rotating flake |
| A slow pull or a slow runner, all green | **Yes. A green complete run is never dropped for being slow (D-6536-26).** It is the noise a ceiling must clear | Stands | One bad run counts twice, in the largest and in `h`, so a spread over 1.25 keeps the entry provisional and goes to the maintainer (D-6536-10), rather than being tidied by removing the run |
| `Check test-time budget` red, tests green | **Yes**, D-6536-11 | Red | See "A red on trunk" |
| `Test the budget check` red, tests green | **Yes**, the times are sound | None: the budget step is skipped | Fix the script or its pins |
| A connector or force-full run, all green | **No:** not plain | **Stands.** Connector classes are left out, so the run is judged | When one first passes after slice F, compare its `Integration` mean with the plain runs'. Ceilings come from plain runs and also judge the others, and whether live suites beside them slow those rows is unknown |

The frontend reads the same way, with no plain rule. Its `bail: 1` means a red run's CSV may also be short. A re-run of it keeps the seed, as the seed is the run id, so it is no new order.

**The calibration table.** Slice C keeps one row per run in this file: run id, attempt, artifact id, commit, the connector answers, `Failed` rows, then count, sum and mean per tier. The same command that CI runs prints those figures from a downloaded CSV, so no new tool is needed. A docs-only commit carries it, so adding a row starts no CI. The ledger keeps run ids only, as DESIGN says.

## Wave: DEVOPS / [REF] A red on trunk from a new ceiling

Provisional entries cannot red on a limit, so this starts at the enforcing push.

**What it does.** Observed on all three red runs read (42372874167, 42379254193, 42127795017):

- **`sonar-gates`, every standalone package, the Windows and macOS checks and Release are skipped.** The image, `packageapp` and the verify jobs still run, so the cost is paid and nothing can ship. The Sonar analysis itself is sent before the budget step, but its gate is unread, and the next green inherits the batch (`ci-learnings.md`).
- **The window stays open for everyone.** `Verify Backend` red is not a green base, so the next push by any Story diffs from the older commit. If that window holds a connector path or a `ci*.yml` edit, an unrelated push runs live suites on the keys CI shares. That is what happened to run 42382479502.
- **Other Stories stop.** T5's rule is that a red is diagnosed before anything else is pushed, so one red holds T5, T6 and T10 for at least one backend job, 13 to 20 minutes.
- **Their proofs survive.** A budget red comes after the tests, the CSV and the upload, so an id comparison or a host count from that run still stands. Only the verdict is red.
- **It may name nobody's change.** A mean or total breach is on a tier, so the Story that pushed may have done nothing.

**What keeps it rare (D-6536-28).** Enforcing waits for 5 runs that include the slowest seen. The enforcing push goes on a clean window with no other Story's push running, so its first verdict is read alone.

**What to do (D-6536-29).** Read the report before anything else.

| Report says | Cause | Action |
|---|---|---|
| Count up, or one class far up | The pushing Story | Its fix: move tests down, or for the total alone a fresh derivation with a reason (D-6536-11), one JSON push |
| Count unchanged | Slow runner | Re-run the failed job once. On a plain run it costs no live quota and, once green, closes the window |
| Count unchanged, red again | Slower fleet, or a real drift | **Stop and raise it.** `LIGHTHOUSE_BUDGET_WARN_ONLY` is a repository variable, and it also softens T4's limits, so **it is the maintainer's to set**: an agent working AFK does not change it |
| The same, the maintainer away, trunk blocked, T9 not yet closed | | `git revert` the enforcing commit: a data-only plain push that returns the entry to warning. Recorded here. After T9 closes this is a raise, and needs the maintainer's word |

## Wave: DEVOPS / [REF] The total ceiling: seconds only, and what option (b) adds

Designed for **(a), seconds only**, while the maintainer's ruling is pending. Nothing above changes with it: slice C's total line stands either way, and only DISTILL's total scenarios wait.

Option (b), a count line beside the total, is one field, one report row and one scenario. It needs no `ci*.yml` edit, and it can come later with nothing redone. On the platform it adds:

| Effect | Seconds only | With a count line |
|---|---|---|
| Calibration | 5 runs, provisional first | **None.** One plain run after slice F gives the number, so it can fail from the start, with no warn stage |
| Reading a red | Noise or growth, by the report | **Always real:** a re-run cannot help, and it names the Story that added rows |
| JSON edits | Every so often, when the room is used | **Every feature that adds an `Integration` row.** Each such push also starts the frontend job, 10 to 16 runner minutes, though not more wall clock, as backend takes longer |
| Reds on trunk | Rare | **More, and this is its real cost.** No local check counts rows, because `dotnet build` and `dotnet test` run no budget step. A Story that forgets learns from a red trunk, with everything in the section above |
| Parallel Stories | Nothing | One line that two Stories edit: a merge conflict, which an agent must stop on rather than rebase. If they merge cleanly, the second push reds for tests that are both reviewed |
| T5 and T10 | Nothing | Moves lower the count. As an at-most line it leaves silent room that the next feature uses with no edit; as an exact line every move edits it too. At most, lowered in T5's and T10's data commits, is the workable form |

Unverified for (b): that the count of the two classes with unstable test names holds still from run to run. The recommendation stays (a), as DESIGN's.

## Wave: DEVOPS / [REF] Rollback

Every step is a `git revert` and a plain push: no `ci*.yml`, no data, no product file, so no live run on a clean window, and nothing a user sees.

| Step | How | Constraint |
|---|---|---|
| A wrong number, right now | The maintainer sets `LIGHTHOUSE_BUDGET_WARN_ONLY=true`: no push, reaches the next run. Not a bad ledger or a stale entry, which stay red by design. Whether a re-run picks up a changed variable is not verified | Softens T4's limits too, so it is short-lived |
| C, enforcing commit | Revert: back to provisional, warnings only. Starts backend and frontend | Before T9 closes; afterwards it counts as a raise. T10's data commit goes first if it has landed |
| C, first entries | Revert: report only | After the enforcing commit |
| B | Revert alone, once C is gone | The ledgers it reads must be out first |
| A | Revert alone, at any time: a frontend-only push. Or the limit back to 2 s | Independent of B and C |
| Finalization docs | With what they describe; no CI | |

**One line on T6's rollback.** T6 says slice F reverts alone "until T9 builds on the column". That moment is T9's first ledger entry, not slice B: with an entry in the ledger and no `Integration` row the check exits 2 (D-6536-13), and a provisional entry counts as in force for that rule, because a column that reads all `Unit` is no limit to warn about. So T9's slice C goes first, then T6's F. Slice B alone only prints "nothing labelled". The reverse order of T6 and T5 stands.

## Wave: DEVOPS / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6536-21 | No `ci*.yml` edit stands; the stale workflow comments wait for the next edit | Their cost is a sentence; the edit's cost is every live suite |
| D-6536-22 | **Two ledger pushes at most: first entries, and the enforcing one. The number is re-derived at those two, not after each run.** Ids between are collected here, in a docs commit. First entries ride a push already going | Each JSON push starts backend, frontend and the tail, and creates the next run, so "each later run re-derives" never ends and costs a tail each time |
| D-6536-23 | Slice A takes `ceil(2.1 x L / 3)` from the latest 5 green shuffled runs at DELIVER, 0.6 s today, nothing listed, enforced from its first push | The runs exist and the frontend has no warn stage to wait in |
| D-6536-24 | `fast_allowlist` entries arrive only with the enforcing push | D-6536-5 cannot be shown with fewer than 5 runs, and a provisional limit only warns anyway |
| D-6536-25 | Eligibility is read per job and per attempt, and each counted run has a row in the calibration table with its artifact id | A run id alone can hold two CSVs, one of them red |
| D-6536-26 | A green complete run is never dropped for being slow | Removing the slow run is how a ceiling ends up inside the noise |
| D-6536-27 | **Tests of the shipped budget file assert the ledger's rules, never its numbers**, and planted cases read the limit from the file. For DISTILL | Run 42372874167: a one-line budget edit without its pinned constant failed `Test the budget check` in both jobs. A data commit must stay a data commit, T10's too |
| D-6536-28 | The enforcing push goes on a clean window, with no other push running | Its first verdict is the one most likely to be noise, and a red there spends other Stories' time |
| D-6536-29 | An enforced red: read the count, one re-run, then stop. The switch is the maintainer's; before T9 closes an agent may revert the enforcing commit | It changes a repository setting that also softens T4's limits; a revert is a reviewed, recorded diff |
| D-6536-30 | Usage-data event N/A | Nobody using the product reaches CI |

Rejected, to keep it small: a `--derive` mode (a second job for the script, for two derivations); an attempt field in the ledger (the table holds it; the check would have nothing to do with it); a check step that refuses a CSV with a `Failed` row (the step already runs only after green); a warn stage for the frontend limit (a second switch, when the 5 runs already exist); a synthetic calibration push (a tail each, for ids real pushes bring).

## Wave: DEVOPS / [REF] Changed assumptions

Original, DESIGN, "How slice C fills the backend": "Each later run re-derives that entry's number with its id added". New: re-derived at the first entries and the enforcing push only (D-6536-22). Why: each edit is itself a full run.

Original, DESIGN, "Slices": "Every push under `Scripts/test-timings/` runs backend, frontend and the tail, and no live suite". New: no live suite **on a clean window**. Why: run 42382479502.

Original, DESIGN, "What can lie", CSV of a red run: "The step runs only after green". It holds for the verdict; the CSV is uploaded regardless, so the derivation has its own rule (D-6536-25).

Original, D-6536-9: "each with seconds, CI run ids and a reason". It stands, and the id is not enough alone; the table adds attempt and artifact.

Original, DESIGN, "The figures", frontend: "Unknown. At most about 2.1 s". New: 0.6 s from five runs. "Too few runs: it stays 2 s" no longer applies today.

Original, DESIGN, "The figures": "about 650 s or about 1,100 s". New: 454 to 777 s across the five runs read.

Original, D-6536-13: "A ledger in force and **no** counted `Integration` row". New: a provisional entry counts as in force for this rule.

Nothing here changes the architecture, so no upstream change goes back to DESIGN.

## Wave: DEVOPS / [REF] Owed to other briefs (not edited here)

**T10 #6537.**

- The lowering commit comes **after** T10's 5 runs, as DESIGN says, and it is a data commit: it starts backend, frontend and the tail, and it needs no test edit (D-6536-27).
- Proposal 6 reads "both from T9's summary". **Read the CSVs**, with `check_budget.py` on a download: a step summary is text on a run page and exists only when the budget step ran. Proposals 1 and 6 use the eligibility rule here, attempts and artifact ids included, or T10's before and after can learn a red attempt.
- If T9 is still provisional at T10's end, T10 re-derives the provisional entry; the same rule, and T9 closes on that 5.
- A lower total or mean is a fresh derivation over those 5 by T9's formula and applies only if lower. Maintainer's word: the total goes down too.
- `category` is slice F's, not slice D's, as DESIGN owes.

**T8 #6514 (state Next; it splits backend unit and integration into parallel jobs and shards the frontend).**

- **One job must judge all rows, or each job judges only what it holds.** As the check stands, a backend job with no `Integration` rows exits 2 once a ledger exists, and an `Integration` job holding part of them passes on a smaller sum and mean. The second is silent. T8 merges the backend CSVs before `Check test-time budget`, or changes what the step is given.
- **It must re-derive every limit.** Fewer tests per job changes per-test times. Mean and fast limit have no raise, so if either rises, T8 stops and asks the maintainer. T4's class limits move too.
- The frontend limit lives in the reporter, so it holds per shard; `test-timings-frontend` and `--pyramid` then need the whole set.
- T8 is a `ci*.yml` edit in any case, so it is the moment for the per-run three shares (DESIGN's point 2) and the two stale step comments.

**T6 #6512.** The rollback line above. And **now:** "Push 3 is the first plain run" does not hold. See "For the maintainer", point 1.

**T5 #6469.** Nothing changes. A T9 enforced red stops its pushes under its own rule.

## Wave: DEVOPS / [REF] For the maintainer

Nothing here blocks DISTILL except the ruling already asked for.

1. **T6's push 3 is not plain (act on this first; it is T6's, found here).** Run 42382479502 has `connector_shared=true` and every connector `true`, so it is a second run of every live suite on the shared keys, and it cannot be T6's first plain run or the comparison against `T0`. Push 2's red left `ci_backend.yml` in the window. If this run ends green the window closes and the next backend push is plain. If it goes red on the same flake, the next one is live again.
2. **How tight the frontend limit starts.** Taken: 0.6 s, or 1.8 s on CI, with nothing listed. One file sets it. Listing that file's slow tests in `slowTests.ts` would give 0.2 s, or 0.6 s on CI, for everything else. It can follow at any time; say if you want it from the start.
3. **Still pending from DESIGN:** seconds only, or a count line. Designed for seconds only.
4. **The switch.** D-6536-29 keeps `LIGHTHOUSE_BUDGET_WARN_ONLY` yours. If you would rather let an agent set it in a blocked-trunk case, say so.

## Wave: DEVOPS / [REF] Open for DISTILL and DELIVER

- **DISTILL:** D-6536-27 for every shipped-file test; a provisional entry with no `Integration` row exits 2; the planted cases as DESIGN lists them. No nWave tags.
- **DELIVER, slice A:** re-read the 5 runs; the local read of `layerBoundaries.enforcement.test.ts`.
- **DELIVER, slice C:** the calibration table; the first comparison of a force-full run's mean against plain runs.

**Not verified.** Any backend tier figure, as the column does not exist yet. The factor 3 for `layerBoundaries.enforcement.test.ts` on a local machine. That the reporter's per-test seconds equal the CSV's `duration_ms`. Whether a re-run picks up a changed repository variable. Whether live suites slow the `Integration` rows beside them. How `gh run download` chooses between two artifacts of one name, which is why the table holds the artifact id. That the step summary cannot be read through the API. Why a skipped `sonar-gates` skips what needs it: observed on three runs, not explained by the `if` in `ci.yml`. The cause of the `PortfolioUpdaterTest` flake, which T6 records as suspected only.
