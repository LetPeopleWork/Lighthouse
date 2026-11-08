# Feature: story-6538-t11-test-writing-guidance

Story #6538 · T11 Test-writing guidance that the DISTILL and DELIVER agents follow · part of Epic #6506 "Fast, reliable test suites".
Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-52](../epic-6264-modular-monolith/slices/slice-52-t11-test-writing-guidance.md).

## Wave: DISCUSS / [REF] Inherited

The brief, settled with the maintainer on 2026-11-07. Its sections "Maintainer decisions", "Test pyramid vision", "Component test audit",
"Fast cycle in DELIVER" and "Change from T5's DEVOPS" override the proposals above them. Waves start at DESIGN.

- Job: infrastructure-only. Size S, two slices of at most a day. Hard dependency: T5 #6469. Soft: T10 #6537, T12 #6539, T9 #6536.
- Settled: the skill holds everything and `CLAUDE.md` keeps three lines. Close on the proxies and note when to read the next feature.
  One trial dispatch on a delivered feature runs. The audit touches no test.
- Pyramid, a direction and not a gate: about 90 % fast, 9.5 % integration, 0.5 % E2E by test count. Time is the hard rule.
- The fast cycle rule is T6's, put into `CLAUDE.md` by step 06-05. The brief says the skill and every DELIVER dispatch carry it.

## Wave: DESIGN / [REF] Scope, mode and context

Application scope, propose mode, maintainer away: each choice takes the recommended option and is recorded; what only the maintainer can
settle is under "Open for the maintainer". Paradigm OOP as the project states. No code, no test and no `CLAUDE.md` is changed by this
wave; the only file written is this one. Nothing was run: this agent has no shell.

Read: slice 52 in full, the T9 delta for shape, T5's convention, slices, "CLAUDE.md bullet" and "What can lie", T6's fast filter and the
roadmap step 06-05, T12's decisions, `docs/ci-learnings.md`, the Quality Gates section of `CLAUDE.md`, and
`.claude/skills/visualization/` for the layout.

**What exists and what a Story still has to build** (checked against the tree on 2026-11-07):

| Named thing | State | Planned by |
|---|---|---|
| `.claude/skills/visualization/SKILL.md` and `references/` | Exists, tracked | |
| `.claude/skills/test-writing/` | **Absent.** Slice A creates it, with `references/` | This Story |
| `vi.useFakeTimers({ shouldAdvanceTime: true })` in frontend tests (three files found, e.g. `ModifyTeamSettings.queryCommit.test.tsx`) | Exists | |
| `pnpm test:fast`, `src/tests/timeBudget/` (reporter, `slowTests.ts`), `actAndSettle.ts`, `inSequence.ts`, `waitPastTheDebounce.ts`, `pastTheAutosaveDelay.ts` | Exist | |
| `FakeLighthouseClock`, `TestToday`, `CapturedLogMessages`, `TestHostPerFixtureGuardTest`, `SharedHostsDrainTheUpdateQueueGuardTest`, `TestTierGuardTest` (edited, uncommitted), `IntegrationFixture` | Exist | |
| `Scripts/test-timings/check_budget.py`, `trx_to_csv.py`, `vitest_to_csv.py`; `ci_frontend.yml` uploads the `test-timings-frontend` artifact | Exist, as before T9 | |
| `nw-skill-reviewer` agent | Exists | |
| `ServicePortHarness`, `WireContractTest`, `Tests/Contracts`, `Tests/Acceptance/<Feature>`, `TestHostUseGuardTest`, T5's `CLAUDE.md` bullet | **Absent** | T5 #6469 |
| `Test-Fast.ps1`, the fast cycle rule in `CLAUDE.md` | **Absent** (`CLAUDE.md` has the `pnpm test:fast` lines only) | T6 #6512, steps 04 and 06-05 |
| Fast-limit ledger, `fast_allowlist`, `check_budget.py --pyramid` | **Absent** | T9 #6536 |
| T10's lessons on rule-versus-wiring | No workspace yet | T10 #6537 |

The skill may name only what exists on the day it is written. A thing in the last four rows goes in as "planned by Story N" or waits.

## Wave: DESIGN / [REF] Project rules, answered

| Rule | Answer |
|---|---|
| Contexts touched (context map) | **None.** No context, concept, edge or event is added. The context map and `ARCHITECTURE.md` section 4 stay as they are |
| New terms | **No domain term.** *Skill*, *fast tier*, *layer* are engineering words and stay out of the glossary |
| Domain events | None published or reacted to |
| RBAC | N/A, because no authorization code changes. The skill's examples may not show a test that re-implements `IRbacAdministrationService` rules |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or payload changes |
| Website | N/A, because nothing a user sees changes |
| Usage-data event | N/A, because nobody uses test guidance inside the product |
| UI surface, sketch, loading and failure looks, visualization skill | N/A, because no screen exists. The skill's frontend half carries one example of pinning both looks for a new view, which is test advice and not a screen |
| Terminology | N/A for the product. The skill's examples write `Feature` and `Work Item`, the seeded defaults, never a tracker's word |
| nWave scenario tags in code | None, and the skill says so. The trial's output is read for them |
| Production code | None. **Mutation: N/A.** Only test files and documents change, so Stryker has nothing to mutate; the deliberate-break rule in the skill stands in. The maintainer accepted this on 2026-11-07 for the Epic's chain where only test files change; the acceptance is recorded in the run's marker |
| Ports and adapters | Product hexagon untouched; see the summary |
| ADR | No new ADR: this is a process document, not a technical decision with a product impact. `brief.md` unchanged |
| External integrations | None added, so no contract-test note |
| Improvement backlog check | N/A, because this Story is itself an item of the Continuous Improvement Epic |
| Rigor profile | Not set in DESIGN. DELIVER proposes it per slice (likely `standard`) and passes `model` on every dispatch, see "Which model does what" |

**Quality gates (CLAUDE.md), one line each.**

| Gate | Answer |
|---|---|
| `pnpm test`, `pnpm test:fast` | N/A, because no frontend test or source file changes. The audit reads a Vitest listing only; that run is not a gate |
| `pnpm build`, Biome | N/A, because Biome checks `./src` and nothing in it changes |
| `dotnet build`, `dotnet test` | N/A, because no backend file changes |
| Coverage, local worker cap | N/A, because nothing is measured for coverage. The listing run keeps the default cap |
| SonarQube Cloud | N/A, because no code is added; Markdown is not analysed |
| CI | A docs-only push starts no CI run; none is expected |

**Finalize checklist, per item.** Docs prose: N/A, because the product docs describe the product and none changes; the skill is a developer
file. Per-feature screenshots: N/A, no screen. Demo data: N/A, no data shape changes. Website assets: N/A, because the website hot-links
product docs and images and none of those change. Release notes: none at finalize, they are written at release.

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6538-1 | **One home: `.claude/skills/test-writing/`.** `SKILL.md` (the layers, the can-it-fail rule, the checklist, the short fast cycle rule), `references/frontend.md`, `references/backend.md` (rules, one example each, the guards' messages). Slice A creates the folder and `references/` | A rule in one place cannot drift. A dispatch reads `SKILL.md` and its own half |
| D-6538-2 | **Files stay short and the checklist comes first.** Each file ends with a line "End of <file>". The dispatch wording tells the agent to read with `ctx_read` in full mode, fresh, and to quote that last line back | A subagent's `ctx_read` can cut a large file to a fraction. A cut that loses the end must be seen, not assumed away |
| D-6538-3 | **Rules cite the measured finding in words, not a Story number.** "A page test that renders a large list cost seconds; the same rule on the function cost milliseconds" | Story numbers archive. A stranger must get the point from the line alone |
| D-6538-4 | **The guards are named with their rule and quoted by message only where the guard exists.** Planned guards are named as planned | A quoted message that no tool reads goes stale; each quote is checked by a grep at DELIVER (below) |
| D-6538-5 | **`CLAUDE.md` gets three lines:** the service-port rule (new acceptance scenarios go to the port), the pointer to the skill, the load instruction. T5's longer bullet is cut to the rule and its body moves into `references/backend.md`. **No Story, slice or ADR number appears in the three lines** | Maintainer's decision. The convention appears once. Numbers archive |
| D-6538-6 | **The fast cycle rule is carried, not left as a pointer.** `CLAUDE.md` stays the rule's home (T6 step 06-05). A **short text** of the rule sits in `SKILL.md`, and the same short text sits in the DELIVER dispatch wording. DELIVER proves by `git grep` that the skill's text and the dispatch wording both contain the rule's distinctive phrases as `CLAUDE.md` words them | The brief says the skill and every DELIVER dispatch carry it. A copy is allowed here because a grep checks it |
| D-6538-7 | **The pyramid goes in once, as a layer rule and a direction:** "a test lands in the lowest layer that can prove it; moving down beats the share; time is the gate". The shares are stated as the direction only. **No dated counts in the skill**; the live ones come from T9's `--pyramid` once it exists | Counts go stale the week after they are written |
| D-6538-8 | **YES, answered by the maintainer 2026-11-08; in slice B:** the "Where to test" bullet in `docs/ci-learnings.md` becomes a one-line pointer to the skill. The brief did not list that file | One home is better, and the maintainer widened the file's scope on 2026-11-08, so slice B makes the bullet the pointer |
| D-6538-9 | **No new guard and no edit to an existing guard or its message.** A gap found while writing the skill goes to the list "Candidates for a tool" below | The brief's OUT list. A guard is its own Story |
| D-6538-10 | **The audit is read-only, sampled by a fixed rule, and its verdict rule is written before the numbers** ("The audit") | A rule fixed afterwards fits the answer |
| D-6538-11 | **Two slices.** A: audit and the frontend half. B: the backend half, `CLAUDE.md`, the trial, the record. Peer review of this design is left to the orchestrator, as T9 and T12 did | See "Slices" |
| D-6538-12 | **The trial prompt is the standard dispatch wording, no hint of the checklist.** The output is read against the checklist by a reviewer, not by the agent that wrote the skill | An agent handed the answer proves nothing |

## Wave: DESIGN / [REF] Where the guidance lives, and what enforces what

**Why one place.** Three homes exist today for parts of this: the ledger (`docs/ci-learnings.md`), T5's `CLAUDE.md` bullet, and the guards'
messages. Each has a different reader. The skill is the only one an agent loads *before* writing a test, so it is the only place that can
prevent the slow shape instead of reporting it. `CLAUDE.md` keeps the hard rule and the pointer, because every dispatch carries it.

**How a dispatch gets it.** nWave subagents have no skill loader, only the `ctx_*` tools. So the dispatch text names the files and says to
read them first. The three `CLAUDE.md` lines make the orchestrator write that sentence into every DISTILL and DELIVER dispatch, and tell
every reviewer that the skill is the standard. Nothing is placed in the nWave package, because a reinstall reverts it.

**What enforces which part.** A tool first; the ledger only where no tool can see it.

| Lesson in the skill | Enforced today by | Planned | Not enforced (convention only), and what to do |
|---|---|---|---|
| Logic on the function, one render per wiring, large lists only when rendering is the test | The frontend per-test limit (reporter, `slowTests.ts`) names a slow test after the fact | T9: a lower limit for files that are not `.test.tsx`; a backend fast limit | Where the logic belongs: no tool can tell |
| No order dependence | Frontend only: the shuffled CI run (T12). **The backend has no order check** | | Backend order dependence: none |
| No `testTimeout` or `retry` as the fix | **Nothing** | | Candidate 1 below |
| Fake clock, no real wait | Backend: `BannedSymbols.txt` bans the ambient clock (its reach into test code is read at DELIVER). Frontend: the time limits catch the cost only | | The skill names `FakeLighthouseClock` (backend) and `vi.useFakeTimers({ shouldAdvanceTime: true })` (frontend). A real wait is a convention; candidate 2 |
| No shared state across fixtures | Backend: `TestHostPerFixtureGuardTest` is the host-per-fixture ratchet only; `SharedHostsDrainTheUpdateQueueGuardTest` covers the update queue | | **Fixture size has no check** in either stack; frontend shared state has the shuffle only |
| Scenario at the port; host test only as a skeleton or in `Contracts` | | T5: `TestHostUseGuardTest` | Convention until T5 lands; named as planned |
| Tier is carried by the base class | `TestTierGuardTest` (T6) | | None |
| A check that cannot fail is not a check | Stryker, per feature, for product code. Ledger rule kept for copy, because StrykerJS does not mutate JSX text | | A test-only change has nothing to mutate: the deliberate break is a convention |
| A new view pins its loading and failure looks | | | **Documented in `CLAUDE.md`, not enforced.** E2E pages waiting for `data-widget-status="ready"` is a convention too |
| A Stryker config must name `Tests.Acceptance.<Feature>` | | | Nothing. Configs are written per feature, under that feature's `mutation/` folder. An unnamed folder shows as uncovered code in the report, late |
| Agents load the skill | | | Nothing. See candidate 3 |

**Candidates for a tool** (not built here, each its own Story; the maintainer picks):

1. A `LEDGER-CHECKS` pattern for `retry:` or `testTimeout:` in the Vitest config and for a real wait in a test file.
2. A Biome or GritQL rule against `await new Promise(... setTimeout ...)` in `*.test.ts(x)`, once the count of existing hits is known.
3. A `PreToolUse` hook that refuses a dispatch to `nw-acceptance-designer` or `nw-software-crafter` whose prompt does not name the skill.

## Wave: DESIGN / [REF] The audit

**Question.** How much of the `.tsx` test layer is really logic, and so could be a `.test.ts` over an extracted function? The answer
decides whether 9.5 % integration can be reached or is only a direction. It touches no test.

**Frame, counted.** Every `*.test.tsx` under `Lighthouse.Frontend/src`. Count on 2026-11-07 by `ctx_glob` with pattern
`<area>/**/*.test.tsx` from `Lighthouse.Frontend/src`, one run per area: **408 files**. A review glob over the whole tree found **410**. The
whole-tree glob output is cut before its count line, so the two are not reconciled here. The record at DELIVER takes the file and test
count from a Vitest listing run and uses that number in the quota below; 408 is used here.

| Group (path under `Lighthouse.Frontend/src`) | Files | Quota | Pick step |
|---|---|---|---|
| `components/Common/Charts` | 37 | 5 | 7.4 |
| `components/Common`, without `Charts` | 125 | 15 | 8.3 |
| `pages/Common/MetricsView` | 31 | 4 | 7.75 |
| `pages/Portfolios` | 54 | 7 | 7.7 |
| `pages/Common/Reports` and `pages/Reports` | 21 + 15 = 36 | 4 | 9 |
| `pages/Settings` | 28 | 3 | 9.3 |
| `pages/Teams` | 45 | 6 | 7.5 |
| the rest: `components/App`, the other `components/*`, `hooks`, `context`, `services`, `tests`, `pages/Common/AdvancedInputs`, `pages/Connections`, `pages/Features`, `pages/Overview` | 52 | 6 | 8.7 |
| **Total** | **408** | **50** | |

**Quota and rounding.** Quota = 50 x group files / total files, rounded half up; the rounded quotas here already add to 50. If a different
count changes that, the largest group takes or gives the difference.

**How the files are picked.** Sort the group's files by path. The i-th pick, for i = 1 to the quota, is the file at position
ceil(i x group files / quota), counting from 1. No random draw, so anyone can redo it. At about 14 tests a file, 50 files are about 715
tests (5,857 `.tsx` tests / 408 files x 50), not 1,000.

**The estimate weights each file by its test count**, because the share is by test and one big file would otherwise count like a small one.

**A second list, kept apart:** the ten files with the most test seconds in the `test-timings-frontend` artifact (the CSV that
`ci_frontend.yml` uploads) of one named, green `main` run. The record names that run. They say where a rewrite pays first and do not enter
the estimate, because they are chosen for being slow.

**What is decided per test.** One word each, by one rule: would the same assertion hold on the return value of a function that needs no
DOM?

- **Logic**: arithmetic, formatting, ordering, filtering, validation, a rule's branches, a state change, building a link.
- **UI**: needs the DOM. Role or name, focus, click wiring, layout, copy in place, a MUI control's behaviour, a view's loading or failure look, routing.
- **Mixed**: one render checks both. Counted apart.

**Tightened after the first agreement round (2026-11-08; the only edit of this rule, made through the round).** The first agreement on ten files was
0.7397, below 0.80. The raters split on three kinds of test, so the rule now says how each is read:

- **A test that mounts a component to get its output is never Logic**, even when the assertion reads a prop handed to a mocked chart, a call count on a service
  or a colour value. Logic is a test that calls a function or a hook (with `renderHook`, no component rendered) and asserts its return. Disagreeing examples:
  `WorkItemAgingChart.test.tsx` "reds the dot of a Work Item Lighthouse marks stale, however recently it moved" and the asks-once tests of `DeliveryCreateModal.loadsOnce.test.tsx` (first rater
  Logic, second rater Mixed): both mount the tree, so they are Mixed.
- **Mixed or UI is decided by what the assertion states.** If it states a value a rule decided (a number, an order, a wording, a colour, a count of calls),
  it is Mixed. If it states only that something is present, shown, hidden, enabled, focused, dimmed or wired to a click, it is UI. Disagreeing examples:
  `WorkItemsDialog.test.tsx` "blocked icon works with different time metrics" (presence only, UI) against "orders worst band first however old the items
  themselves are" (an order a rule decided, Mixed); `LighthouseVersion.test.tsx` "renders update button if newer version available" (presence of a button, UI).
- **A rule proved only through the DOM's text still counts as Mixed when the text is the rule's result** ("says 'More than 5 years'"), and as UI when the text is
  fixed copy.

**One example of each**, judged from the test title and to be confirmed on reading, so the rater sees the rule applied:

| Class | Example | Why |
|---|---|---|
| Logic | `ForecastLevel.test.tsx`, "should set the level, IconComponent, and color for probability <= 50" | A probability goes in, a level comes out. No click, no layout |
| UI | `AppErrorBoundary.test.tsx`, "reloads the page when the reload button is pressed" | Needs a rendered button and a click |
| Mixed | `DateRangePresets.test.tsx`, "offers a team every window it was given, in the order it was given them" | The ordering is a rule; it is proved through a render |

**Lookup steps per file (the same five, every file):**

1. List the file's test titles from the Vitest listing.
2. Read the file's imports and its first test to see what is rendered and what is mocked.
3. Find the unit under test, and any pure function or class it uses, in the component's folder and in `src/utils`, by `ctx_search`.
4. Look for a `.test.ts` for that function. One exists: the logic is **E0**. None: judge the extraction.
5. Label each test by the rule, give the file its cost class and call, and write the row.

**What is decided per file** (a row in the record, nothing edited): tests; logic, UI, mixed counts; whether the logic already lives in a
function with its own `.test.ts`; the extraction cost, **E0** the function exists, **E1** a pure function comes out of the component in one
sitting, **E2** it needs a hook or a context change; and the call, *move*, *keep*, or *duplicate of an existing `.test.ts`* (a candidate
to delete, not a move).

**Estimate and verdict rule (written now, before any number).**

- **Statistic.** Each share = logic tests in files of the stated cost classes / all tests in the sample, weighted by test count. **Low** =
  logic tests in E0 files. **Middle** = logic tests in E0 and E1 files, the headline. **High** = logic plus mixed tests in files of every
  class. Low <= Middle <= High by construction. Each has a 95 % interval from resampling files within their group. **Every comparison below
  uses the lower end of the interval**, so a lucky sample cannot declare it reachable. The point and the upper end are reported beside it.
- Also reported: the **seconds** in those tests from the CSV. A share moved is not seconds saved, and time is the hard rule.
- **Needed share N** = the share of `.tsx` tests that must move for the fast tier to reach 90 %. Frontend alone it is about 84 % (4,907 of
  5,857, from the brief's counts). For the whole suite it needs the backend labels from T6, so the record says which N it used.
- **Verdict, exhaustive:**

| Band | Rule | Verdict |
|---|---|---|
| 1 | Low (lower end) >= N | Reachable with the functions that already exist (E0) |
| 2 | Middle (lower end) >= N > Low (lower end) | Reachable with small extractions (E1) |
| 3 | High (lower end) >= N > Middle (lower end) | Needs the E2 work (hooks, context changes) |
| 4 | High (lower end) < N | A direction only; moving logic cannot reach it |

The number goes to the maintainer before any rewrite is planned.

**Which model does what.** The profile is `standard` (DELIVER confirms it with the maintainer). First-rater audit dispatches and the trial
dispatch use the profile's `agent_model`; the second rater, the skill review and the trial reviewer use its `reviewer_model`. `model` is
passed on each dispatch. The values are read from the profile at DELIVER, not written here. If `reviewer_model` equals `agent_model`, the
record says so.

**Cost and check on the audit.** About 715 tests to read: five batches of ten files. A second rater reads the same 10 files blind, the
5th, 10th, 15th and so on of the 50 picks.

- **Agreement** = the share of those tests, about 140, given the same label by both raters (logic, UI or mixed). Cohen's kappa is reported
  beside it and does not gate.
- **At 80 % or more:** the first rater's labels stand.
- **Below 80 %:** list the disagreeing tests, tighten the rule's wording with them as examples, and both raters redo those 10 files; the
  first rater also re-reads, in the other 40 files, every test of a kind that disagreed. One round only. If still below 80 %, report the
  figures to the maintainer and mark the headline "low confidence".

## Wave: DESIGN / [REF] What T11 cannot start until

| Waits for | State on 2026-11-07 | What it blocks |
|---|---|---|
| **T5 #6469** (hard, from the brief) | Not delivered: `ServicePortHarness`, `WireContractTest`, `TestHostUseGuardTest` and the bullet are absent | The backend half, and the cut of T5's bullet. Without it the skill would teach a convention that no guard backs |
| **T6 #6512 step 06-05** | Not done: no `Test-Fast.ps1`, no fast cycle rule in `CLAUDE.md` | The short rule text in `SKILL.md`, the DELIVER dispatch wording (nothing to copy yet) and the backend share in the audit |
| T12 #6539 | Closed or closing | **Soft**: the order-dependence lesson, and the frontend per-test numbers |
| T9 #6536, T10 #6537 | Not delivered | Soft: numbers to cite, the rule-versus-wiring lesson. The skill is written without them and takes a line when they land |
| DEVOPS and DISTILL of this Story | Not run | Waves go in order; nothing is skipped |

Only the backend half, the cut of the bullet and the fast cycle text need T5 or T6. The audit and the frontend half need none of them. **If the maintainer wants
slice A before T5, say so; the default stays the brief's order.** Answered 2026-11-08: slice A starts now, beside T5's DELIVER, provided it touches no file T5 touches.

## Wave: DESIGN / [REF] Slices

| Slice | Days (est.) | Delivers | Needs |
|---|---|---|---|
| A | 1 | The audit and its record; creates `.claude/skills/test-writing/` with `SKILL.md` and `references/frontend.md`; a `nw-skill-reviewer` pass | Nothing hard. T12 is soft. Starts now, beside T5 (2026-11-08), touching no file T5 touches |
| B | 1 | `references/backend.md`; the fast cycle text in `SKILL.md`; the three `CLAUDE.md` lines and the cut of T5's bullet; the trial; the closing record with the deferred check open. the ledger pointer (D-6538-8, yes on 2026-11-08): the "Where to test" bullet in `docs/ci-learnings.md` becomes a one-line pointer to the skill | T5 done; T6 step 06-05 done |

**Proof for each.**

- Every identifier the skill names is found by a search of the tree on the day, listed in the record; a planned one carries "planned by".
- Every quoted guard message is found in the guard's source by the same search.
- The T5 convention text has one home: a search for its distinctive phrase prints `references/backend.md` and `CLAUDE.md`'s one rule line only.
- The fast cycle rule: a search for its distinctive phrases in `CLAUDE.md`, `SKILL.md` and the DELIVER dispatch wording prints all three, and the three agree.
- The three `CLAUDE.md` lines hold no Story, slice or ADR number.
- Each file read as a subagent with `ctx_read` shows its last line.
- The trial: one `nw-acceptance-designer` dispatch on a small delivered feature (candidate: story 6501, ordered parent fields, **not checked for size**),
  with the standard wording and the skill named, writing **scenario text into the Story's workspace only**, not into the test project.
  Read by a reviewer against the checklist; the result, with any deviation, is written down.
- **The deferred check is recorded open:** the next feature's DISTILL writes "skill loaded: yes/no, deviations" in its
  `feature-delta.md`; the maintainer reads it after that DELIVER. T11 closes on that note, as decided.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision | Contract shape, universe |
|---|---|---|---|
| `.claude/skills/visualization/` layout | Skill with `references/` | REUSE the layout | Files only, no effect |
| Ledger "Where to test" bullet | Same how-to | **YES**, maintainer 2026-11-08 (D-6538-8), slice B | Documentation, no effect |
| T5's `CLAUDE.md` bullet | Same convention | MODIFY: cut to the rule, body moves | Documentation, no effect |
| T6's fast cycle rule | The cycle | CARRY a short text in skill and dispatch; home stays `CLAUDE.md`; a search checks the three agree | Documentation, no effect |
| `nw-skill-reviewer` | Skill structure and scope | REUSE | Read-only agent |
| Frontend `test-timings-frontend` CSV, Vitest listing | Counts and seconds for the audit | REUSE, read only | Reads existing files; writes the record only |

Nothing is CREATE NEW beyond the skill itself, which has no existing alternative: the rest are rules of a different reader.

## Wave: DESIGN / [REF] What can lie, and what catches it

| Could be false | Effect if unseen | Caught by |
|---|---|---|
| The skill is read whole | A cut file loses its end, and an agent follows half a rule | Last-line sentinel, quoted back; short files; the trial's agent reports it |
| The skill names what does not exist yet | Agents write against a missing harness | The search list; the "planned by" mark; T5 gates the backend half |
| A quoted guard message changes | The skill teaches a message nobody sees | The search of each quote at DELIVER. Not kept in step later: **not caught**, so quotes stay few |
| The carried fast cycle text drifts from `CLAUDE.md` | Two versions of the rule | The search at DELIVER. Later drift: **not caught**; the next DELIVER's dispatch check is the only reader |
| An agent follows the skill | Slow shapes return | The trial (one run); the guards and T9's limits fail the build on the next feature; the deferred read |
| The trial agrees because it was primed | A false pass | Standard wording only; a reviewer reads the output |
| The orchestrator forgets the load sentence | The skill is never read | The `CLAUDE.md` lines; the deferred "loaded: yes/no"; candidate 3 |
| The audit sample is biased or the call is generous | The pyramid looks reachable and is not | Fixed picking rule, weighting by test, lower interval ends in every comparison, the heavy files kept out of the estimate, a second rater, E-classes so a hard extraction is not counted as cheap |
| A share moved is read as time saved | A rewrite that saves nothing is planned | Seconds reported beside the share |
| The skill and `CLAUDE.md` drift on the convention | The convention exists twice | Cut in the same change; the search proof |
| A Stryker config misses `Tests.Acceptance.<Feature>` | A new scenario looks uncovered | In the backend half; the report shows it |
| "Fast" means a file name | The skill names the wrong tier | Fast is decided by time; the skill says so |

## Wave: DESIGN / [REF] C4

```mermaid
C4Container
  title Test-writing guidance: who reads what
  Person(dev, "Maintainer", "Agrees waves and reads the record")
  System_Boundary(repo, "Lighthouse repository") {
    Container(claude, "CLAUDE.md", "Markdown", "Three lines, and the fast cycle rule's home")
    Container(skill, "test-writing skill", "Markdown", "Layers, checklist, short fast cycle text, frontend and backend halves")
    Container(guards, "Guards and budgets", "Vitest, NUnit, Python", "Fail a slow or wrongly placed test")
    Container(ledger, "ci-learnings.md", "Markdown", "Failure signatures no tool sees")
  }
  Container(agent, "DISTILL and DELIVER subagent", "nWave, ctx_* tools", "Writes tests")
  Rel(dev, claude, "Reads the pointer in")
  Rel(claude, agent, "Tells the dispatch to load the skill for")
  Rel(agent, skill, "Reads before writing tests from")
  Rel(skill, claude, "Copies the fast cycle rule from")
  Rel(guards, agent, "Fails the build of")
```

## Wave: DESIGN / [REF] Open for the maintainer

1. **Start of slice A before T5?** (Answered yes, 2026-11-08; points 1, 2, 3 and 4 below are answered in "Maintainer decisions (2026-11-08)".) The audit and the frontend half need nothing from T5. Recommend: keep the brief's order (T5 first); say so if you want the audit earlier.
2. **Candidate tools** (three, above). Recommend 1 and 3 as small Stories after T11; 2 only once the existing hits are counted. Candidate 3 touches your hook settings, which no agent changes unasked.
3. **The ledger pointer (D-6538-8) is pending.** It edits `docs/ci-learnings.md`, which the brief did not list. Recommend yes, to keep one home. It stays out of slice B until you answer; with no answer the bullet stays.
4. **The trial's feature.** Story 6501 is a candidate, not checked. Any small delivered feature with DISTILL scenarios will do.
5. **The audit's verdict** goes to you before any rewrite is planned, as the brief says. Nothing about it can be decided in your absence.
6. **The file count: settled.** `git ls-files 'Lighthouse.Frontend/src/**/*.test.tsx'` gave 410 on 2026-11-07, so the review's glob was right and the per-area count of 408 missed two files. The quotas above use 408; DELIVER takes the Vitest listing, as said, which moves a quota by at most one file. Nothing for the maintainer here.

## Wave: DESIGN / [REF] Review

The review came back NEEDS_REVISION. What changed:

- **Fast cycle (D-6538-6):** the brief says skill and dispatch carry the rule, so both now carry a short text; `CLAUDE.md` stays the home and DELIVER searches that the three agree. Reuse table, slices, risk table and C4 follow. The earlier remark about a user instruction is gone.
- **Verdict rule:** four exhaustive bands, each on the lower end of the 95 % interval of a named statistic (a test-count-weighted share).
- **Audit frame:** recounted by area on 2026-11-07, 408 files (the review's glob said 410, left open). Groups, quotas, rounding and pick step are in a table; the Reports group uses the exact paths `pages/Common/Reports` and `pages/Reports`. Read cost is about 715 tests, not 1,000. The redundant "start at the first" term is dropped.
- **Mutation:** N/A kept: nothing to mutate, the deliberate break standing in, the acceptance noted as recorded in the run's marker.
- **D-6538-8** was answered yes on 2026-11-08 and is in slice B.
- **"Enforced today" table** rebuilt with a "Planned" column and a "not enforced" column: the shuffle is frontend only, timeout and retry are unenforced, `TestHostPerFixtureGuardTest` is the host-per-fixture ratchet only, fixture size has no check, the loading and failure rule is documented and not enforced, and T9's limits and `TestHostUseGuardTest` moved to "planned".
- **Frontend fake** named: `vi.useFakeTimers({ shouldAdvanceTime: true })`, found in three test files, beside `FakeLighthouseClock`.
- **The CSV** is the `test-timings-frontend` artifact of a named green `main` run; the record names it.
- **T12** is soft in both places (slice A and the dependency table).
- **Models:** `agent_model` for the first-rater audit dispatches and the trial; `reviewer_model` for the second rater, the skill review and the trial reviewer; profile `standard`, values read at DELIVER.
- **Medium items:** agreement metric and the below-80 % path; one example per class and five lookup steps; `references/` created in slice A; one line per quality gate; the finalize checklist answered per item; no Story numbers in the `CLAUDE.md` lines.
- **Changed assumption.** Original, slice 52, pyramid vision: "about 90 % fast, 9.5 % integration, 0.5 % E2E ... as upper limits on the two upper layers." New: a direction only, nothing gates it, the shares are reported. Why: the same section says "Direction, not a gate" and "the hard rule stays time".

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**The feature's shape in hexagonal terms.** There is no product hexagon to change. T11 works on the reader side of the tests, the agents
and their instructions.

```
  DRIVING SIDE                      APPLICATION / DOMAIN               DRIVEN SIDE
  (who triggers work)               (production code)                  (trackers, persistence)
  --------------------              ------------------------           ---------------------------
  controllers, routes   unchanged   services, ports   unchanged        trackers, database  unchanged
  hooks, pages          unchanged   domain Models     unchanged        no new external call

  THE TEST SIDE (where T11 works)
  dispatch -> reads CLAUDE.md [MODIFIED: 3 lines] -> loads test-writing skill [NEW]
                                                        |-- frontend half [NEW]
                                                        |-- backend half  [NEW, after T5]
  guards and budgets [unchanged] fail what the skill failed to prevent
  audit record [NEW, read-only] -> number to the maintainer
```

New: the skill (three files), the audit record, the trial record. Modified: `CLAUDE.md` (three lines, T5's bullet cut). The ledger's "Where to
test" bullet is modified in slice B (the maintainer said yes on 2026-11-08). Unchanged: every driving adapter, application service, port, domain Model and driven adapter, every guard, Playwright.
New external call: none.

**Patterns weighed.**

- **Single source of truth:** chosen for the convention. The skill holds how-to, `CLAUDE.md` the pointer. Rejected: repeating T5's full bullet in both (it drifts).
- **A checked copy:** chosen for the fast cycle rule only, because the brief wants it in skill and dispatch. Rejected: a bare pointer (an agent may not follow it) and an unchecked copy (it drifts).
- **Prevent early, detect late as a pair:** chosen. The skill prevents; the guards and T9's limits detect. Rejected: guidance only (nothing fails when ignored) or guards only (a slow test is named after it is written).
- **Functional core, imperative shell, as a test rule:** chosen. Logic on a function is a millisecond test; the render keeps the wiring. Rejected: making every page test cheaper, which keeps the logic hidden in the component.
- **Test the lowest layer that can prove it (pyramid) as a direction:** chosen. Rejected: a share gate, because a share is met by adding trivial tests; time stays the gate.
- **Ratchet with a named list (T9, T4):** reused, not new. Rejected: a count of "tests moved", which hides which test.
- **Dependency inversion for the clock:** chosen, as the guidance to inject a fake clock in both stacks. Rejected: real waits with longer timeouts.
- **Fail-fast probe of the dependency (earned trust):** applied to the skill itself: the last-line check and the trial prove it is read and followed, rather than assuming so.
- **Sampling with a fixed rule and a rule for the verdict set first:** chosen for the audit. Rejected: reading every file (hours, no extra decision) and a random draw (cannot be redone).
- **Domain events, outbox, unit of work, value objects, optimistic concurrency, JSON column against child table:** not applicable, no domain or persistence change.
- **Enforcement by hook on dispatch:** weighed, **not chosen**: it is a new mechanism with false hits, and the deferred read will show if it is needed.

## Wave: DEVOPS / [REF] Platform readiness

Existing platform; nothing is deployed. Written 2026-11-07 with the maintainer away, so each choice takes the recommended option and says why.
**No test ran and no file but this one changed.** Read: this delta, T9's DEVOPS sections for shape, `ci.yml`, `pages.yml`, `ci_changes.yml`,
`ci_frontend.yml`, `.githooks/`, `docs/_config.yml`, `package.json`. Read from GitHub, read-only: the runs of recent `main` pushes, and the artifact
lists of two runs. `environments.yaml` is not written: this wave may edit this file only.

| Topic | Answer |
|---|---|
| Environments | **Local only. Nothing is deployed.** The skill, the three `CLAUDE.md` lines and the two records are files in the repository, read by agents on the maintainer's machine. No CI job judges any of it. The one hosted effect is the documentation site (see the push table) |
| Pipeline | **No `ci*.yml` edit, no new step, no new workflow, no new tool.** T11 adds Markdown and edits Markdown |
| Deployment strategy | None: a trunk push to `main`, one commit per step. Rollback is below |
| Branching | Trunk-based, unchanged |
| Observability | None to add. The only signal is the deferred note: the next feature's DISTILL writes "skill loaded: yes/no, deviations" in its `feature-delta.md`, and the maintainer reads it |
| Mutation | N/A, because only documents change and Stryker has nothing to mutate. The deliberate-break rule in the skill stands in, as in DESIGN. `CLAUDE.md` keeps its strategy line |
| Usage-data event | **N/A, because nobody using the product reads a test-writing skill, so no event could show use.** `UsageDataEventName` and `docs/settings/usagedata.md` are unchanged |
| Peer review | Not run: no new target, framework, stack or security change. The review at the end of DISTILL covers this wave |

## Wave: DEVOPS / [REF] What each push triggers

Read from `ci.yml`, `pages.yml` and `docs/_config.yml`, and checked against real runs on 2026-11-07.

| Path pushed | Build And Deploy Lighthouse (`ci.yml`) | Deploy Documentation (`pages.yml`) |
|---|---|---|
| `.claude/skills/**` | **No run.** Not in the `paths` list | No run |
| `CLAUDE.md` | **No run** | No run |
| `docs/feature/**` (this delta, the audit record, the trial scenarios) | **No run** | **Runs**, about 40 seconds. The site excludes `feature`, so the build leaves it out, but the workflow still starts |
| `docs/ci-learnings.md` (the pointer was approved 2026-11-08) | **No run** | **Runs**, and the page is published |

`ci.yml`'s push `paths` are the backend, backend tests, E2E and frontend folders, `ci*.yml`, `.github/actions`, `.github/scripts`,
`Scripts/test-timings`, `examples/postgres`, `Dockerfile`, `.nvmrc`, `.dockerignore`, `LICENSE` and `NOTICE`. None of T11's paths is in it.
`ci.yml` also has a `pull_request` trigger with no paths filter, but this project pushes to `main` and opens no pull request.

**Confirmed, not assumed.** Commit 71e1d3cea changed only files under `docs/`. Its sha has one run, Deploy Documentation, and no Build And Deploy
Lighthouse run. The docs-titled commits that do show a Build And Deploy run are explained by the push: a push is judged on all its commits, and
those pushes also carried backend test files, so the docs commit was only the head. **So the one rule for each T11 push:** list the unpushed range
first (`git diff --name-only origin/main..HEAD`) and read it. Any file outside `docs/`, `.claude/` and `CLAUDE.md` is another session's code and
would start the full tail. If it is there, say so to the maintainer and do not push. Today the working tree holds an uncommitted edit of
`TestTierGuardTest.cs` that is not T11's.

**Hooks.** The commit hook scans staged files for secrets with `gitleaks`, so the skill's examples must hold no token-shaped string. The push hook
scans with the `sonar` CLI and skips itself when the CLI is absent. Neither is a CI step. Sonar's analysis excludes `.claude/**` and `docs/**`.

## Wave: DEVOPS / [REF] What needs a run, and what the checks run as

**Nothing needs a CI run or a test run.** No gate in `CLAUDE.md` applies, as DESIGN answered line by line. The proofs listed under DESIGN "Slices" are
**by-hand searches at DELIVER**, run once per slice and written into the record with their output. They are not a ledger rule and not a tool.

| Check | Runs as | Why not more |
|---|---|---|
| "End of <file>" on each file, read as a subagent would | By hand: a `ctx_read` of each file in full mode, fresh, and the last line quoted into the record. The trial agent reports it too | One reading per file; a tool would check a line that a human must also see in a subagent's cut output |
| Fast cycle rule: skill, dispatch wording and `CLAUDE.md` agree | By hand: `git grep` of the rule's distinctive phrases in `CLAUDE.md` and `SKILL.md`, output pasted, and the dispatch wording compared against the same phrases | The dispatch wording is text the orchestrator writes, not a file, so no tool can read it. A ledger rule would claim a check nobody runs |
| Every named identifier and every quoted guard message exists | By hand: `git grep` of each, list in the record, "planned by" marks left as they are | A quote is stale the day its guard changes, and a later check is not kept (DESIGN says so) |
| Convention has one home; no Story, slice or ADR number in the three lines | By hand: one `git grep` for the T5 phrase, one for the number patterns in `CLAUDE.md` | One-off at the cut |

**The drift after DELIVER is not caught**, as DESIGN says: the next DELIVER's dispatch check and the deferred read are the only readers. If the
maintainer wants it caught, that is DESIGN's candidate 3, its own Story.

## Wave: DEVOPS / [REF] The audit, local and read-only

Runs in `Lighthouse.Frontend` on Linux. It writes nothing but the record, edits no test and needs no Docker, no backend and no database.

| Input | How it is read | What the record keeps so it can be redone |
|---|---|---|
| The file population and counts | `git ls-files` for the `.test.tsx` files, and Vitest's listing command for the titles. The listing runs no test; DELIVER confirms its flags on the day | The commit it ran at; the counts per group; the sorted list of each group; the picked files, in pick order |
| Seconds per test | The `test-timings-frontend` artifact of one green `main` run, downloaded read-only with `gh`. Run 42372930241 holds it and does not expire until 2027-02-05 (checked today). DELIVER may pick a newer one and names it | The run id; the ten slowest files with their seconds, pasted, because the artifact expires after 90 days |
| Labels | Two raters per DESIGN's rule and five lookup steps | One row per file: tests, logic, UI and mixed counts, extraction class, call. The second rater's labels for its files. The agreement figure and, if used, the list of disagreeing tests |
| The estimate | Computed from the rows; resampling within groups | The seed or the resampled intervals as printed, the statistic named, the N used and where it came from |
| The models | Read from the profile at DELIVER | The `agent_model` and `reviewer_model` used, and a line saying so if they are the same |

A stranger with the commit, the pick list and the labelled rows can recompute every share. The CSV is the one input that expires, so its figures are
pasted. The audit needs no `LIGHTHOUSE_TEST_WORKERS` setting: listing is light. Runs elsewhere in the checkout (T6, T9) are not disturbed by it.

## Wave: DEVOPS / [REF] Rollback

Every step is a `git revert` and a plain push: no `ci*.yml`, no data, no product file, nothing a user sees. A docs revert starts Deploy Documentation
again and nothing else.

| Step | How | Constraint |
|---|---|---|
| Slice A, the audit record | Revert the commit. Nothing reads it | None |
| Slice A, the skill and `references/frontend.md` | Revert the commit | **Not while the three `CLAUDE.md` lines are in.** They point at the skill, so revert slice B's `CLAUDE.md` commit first |
| Slice B, `references/backend.md` and the fast cycle text | Revert the commit | If `CLAUDE.md`'s lines are still in, they point at a skill without a backend half; revert them in the same push |
| Slice B, the three `CLAUDE.md` lines and the cut of T5's bullet | Revert the commit: T5's bullet comes back whole, so the convention then also stands in the skill's backend half until that is reverted. State that in the revert message | Revert before the skill folder, never after |
| The dispatch wording | Not a file. The orchestrator stops writing it, and the next dispatch is without it | |
| The trial and record | Revert the docs commit | None |

A wrong rule in the skill needs no revert: it is a one-line edit of a Markdown file, with the same searches re-run.

## Wave: DEVOPS / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6538-13 | No `ci*.yml` edit, no new step, no tool, no new workflow | Markdown has no build; each addition would be code to maintain for text |
| D-6538-14 | The "End of <file>" and fast-cycle agreement checks are **by-hand searches at DELIVER, output pasted in the record**. No ledger rule, no tool | The dispatch wording is not a file, so no tool reads all three; a ledger rule would promise a check nobody runs |
| D-6538-15 | Before each push, read the unpushed range; push only if it holds nothing outside `docs/`, `.claude/` and `CLAUDE.md` | A push is judged on all its commits. A stray code commit would start the whole tail for a docs change |
| D-6538-16 | Usage-data event: **N/A, because nobody using the product reads test guidance** | Nothing in the product changes |
| D-6538-17 | The audit's record keeps the commit, the pick list, the labelled rows and the pasted top ten seconds | The artifact expires; the record must stand without it |
| D-6538-18 | Rollback per commit; the `CLAUDE.md` commit goes before the skill's | The lines point at the skill |
| D-6538-19 | Environments: local only | Nothing is deployed |

## Wave: DEVOPS / [REF] Changed assumptions

| Was | Is | Why |
|---|---|---|
| DESIGN, quality gates: "A docs-only push starts no CI run; none is expected" | **Holds for the build and test workflow.** A push of `docs/**` does start Deploy Documentation, about 40 seconds, which publishes the site | `pages.yml` triggers on `docs/**`. It cannot fail on `docs/feature/**`, which the site excludes |
| The stored note that a docs-only push starts no run | Too wide: it holds for Build And Deploy Lighthouse only, and its path list is longer than the note says | Read from `ci.yml` today |

## Wave: DEVOPS / [REF] For the maintainer

Nothing new needs you here. DESIGN's list was answered on 2026-11-08 (see "Maintainer decisions (2026-11-08)"); what follows is the list as it stood: the ledger pointer, the order of slice A against T5, the three tool
candidates, the trial's feature, and the audit's verdict when it exists. One thing to know: a `docs/**` push redeploys the documentation site
whether or not any page changed.

## Wave: DEVOPS / [REF] Open for DISTILL and DELIVER

- DISTILL: the proofs under DESIGN "Slices" become the scenarios, each written as the by-hand search above. No tags in them.
- DELIVER: run the push check of D-6538-15 before each push; paste each search's output; name the CSV run; read the profile's models.
- The Vitest listing command's flags are confirmed on the day, because this wave ran nothing.

## Wave: DISTILL / [REF] Scope, reconciliation and baseline (2026-11-07, maintainer away)

AFK: each choice takes the recommended option and says why. Written as a specification only.

- **Shape.** T11 delivers a skill, three `CLAUDE.md` lines and two records. There is no test code to scaffold, so this wave writes no test file, no production file, no scaffold, no Tier B, no domain-types module and no state-delta port. The only file changed is this one. A scenario here is a Given-When-Then that a person can check by a search, by reading a record, or by a run, and each one says how it fails.
- **No nWave scenario tag goes anywhere,** not in these scenarios, not in the skill, not in the trial's output. An untagged scenario is correct here. The classification lives in the plain `Kind` column: `H` happy path, `B` boundary, `E` the check fires on a planted fault or refuses.
- **UI sketches: N/A,** because nothing a user of the product sees changes. Loading and failure looks: N/A, no screen. The skill's one example of a new view pinning both looks is test advice, and a scenario reads that it is there.
- **Language and policy.** Markdown and shell searches. Deliverable type: skill, so the reviewer is `nw-skill-reviewer`. Journey, KPI contract: none. Infrastructure policy read as inherited; no port is added. Outcome registry: not run, since no typed contract is introduced.
- **No test, build or search ran in this wave.** Every "then" below is a rule for reading what DELIVER runs, not a result.
- **Contract shape.** One row is bounded-change: the changed-path ranges (C-5), whose universe is the list of changed paths. Everything else is a pure check of text.

**Reconciliation: 0 hard contradictions** across DISCUSS, DESIGN and DEVOPS. Soft points:

| Point | Resolved |
|---|---|
| DESIGN "Slices" says slice A needs "nothing hard"; the brief says T5 is a hard dependency | DESIGN's open point 1 keeps the brief's order. B-1 gates slice B |
| DESIGN says a docs-only push starts no CI run; DEVOPS found Deploy Documentation starts for `docs/**` | DEVOPS's changed assumption stands. C-5 holds the push check |
| File count 408 (DESIGN) against 410 (`git ls-files`, DESIGN's own open point 6) | 410 is the count. Quotas are recomputed from it, and the listing is compared against it (A-1) |

## Wave: DISTILL / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6538-20 | **Each scenario names its proof kind:** search, read, listing or run. A search prints its output into the record; a read quotes a line | A claim with no kept output cannot be checked later |
| D-6538-21 | **Every scenario names how it can fail. Where a check could pass by luck, a planted fault is run once:** a read cut short for the last-line check, a known-missing name for a names search, a known-present pattern for a search that must print nothing | A check never seen failing may be one that cannot fail |
| D-6538-22 | **The trial's wording is frozen here** and pasted unchanged in the record. The one sentence that is the project's own, the no-tags sentence, is named and exempt from the "no hint" check | A wording changed after the result fits the result |
| D-6538-23 | **The audit's band wording is fixed here,** before any number | Same reason as the verdict rule in DESIGN |
| D-6538-24 | **The trial reviewer is never the agent that wrote the skill or ran the trial,** and runs on the profile's `reviewer_model` | A writer reading its own output finds what it expects |
| D-6538-25 | **A deviation found by the trial changes the skill only by a separate edit,** after which the searches of the changed slice run again | A silent fix would leave the record saying the skill was followed as written |
| D-6538-26 | **A guard's message is quoted only from committed source. If the file differs from `HEAD`, the work stops.** No wait, no quoting of the working tree | `TestTierGuardTest.cs` is edited and uncommitted in this checkout today, and a quote from it can go stale in the next commit |
| D-6538-27 | **A planned name carries a role and never a number:** "planned: the per-tier report". The number search stays strict and has no exceptions | An exception in the search would let a number in |
| D-6538-28 | **One stop form.** A dated STOPPED block (see "Stop rules"). No other way of reporting a stop | The orchestrator reads one shape and takes it to the maintainer |
| D-6538-29 | **No control trial for the tags.** The trial does not measure the tag rule, and B-11 records that | The wording carries the project's sentence, so a clean result says nothing about the skill |

## Wave: DISTILL / [REF] The trial, as worded

The standard DISTILL wording as the three `CLAUDE.md` lines will make the orchestrator write it. Only the angle-bracket parts change. **The last sentence is the project's own no-tags sentence** and is the only part of the wording exempt from the "no hint of the checklist" check (B-9). The checklist is not in the wording.

```
You are the acceptance designer for <feature>, a feature that is already delivered. Its stories are in <feature workspace>/discuss/.
First read, with ctx_read in full mode and fresh=true, .claude/skills/test-writing/SKILL.md and the file in
.claude/skills/test-writing/references/ for your half (frontend.md or backend.md). Each file ends with a line "End of <file>".
Quote that last line back in your report, one per file. If you did not see it, say so and read the file again in pages.
Then write the acceptance scenarios for <feature> into <scratch workspace>/ and nowhere else. Write no test file and no production file.
Report: the files you read with their last lines, the scenarios you wrote, and any rule of the skill you did not follow and why.
No nWave scenario tags (@us-, @slice-, @kpi-, @contract-shape and the like) in anything you write.
```

Fixed for the trial: the feature (small, delivered, with DISTILL scenarios; story 6501 is the candidate, its size not checked, B-7), the scratch workspace (`docs/feature/story-6538-t11-test-writing-guidance/trial/`), the model (`agent_model`) and the reviewer (a different dispatch, `reviewer_model`).

## Wave: DISTILL / [REF] Scenarios

**Kinds:** `H` happy path, `B` boundary, `E` the check fires on a planted fault or refuses. **Proof kinds:** search, read, listing, run. A run is a Vitest listing or a `gh` download; no test is executed. Rows marked C run at the end of each slice, over what that slice changed.

### Slice A: the audit and the frontend half

| # | Scenario | Given / When / Then | Proof and output kept | Can fail when | Kind |
|---|---|---|---|---|---|
| A-1 | The population is counted and compared with the file list | Given the commit the audit runs at. When the test files are listed by `git ls-files` (410 on 2026-11-07) and the tests by Vitest's listing. Then the record holds the commit, the file count per group and quotas recomputed from 410 that add to 50, and any difference between the listing and `git ls-files` is named file by file. **Any difference left unexplained stops the audit** | Listing and `git ls-files`, both pasted with counts | The counts differ and no file is named, quotas add to 49 or 51, or labelling starts with an open difference | E |
| A-2 | The picks can be redone from the rule | Given a group sorted by path. When the i-th pick is taken at position ceil(i x files / quota). Then anyone gets the same files; for `pages/Settings` (28 files, quota 3) the positions are 10, 19 and 28 | Search: the smallest and largest group redone by hand, pasted beside the record's lists | A redone list differs, or the last pick is not the last file | B |
| A-3 | Every sampled file has a row and the rows add up | Given the 50 picks. When the rows are written. Then each file has tests, logic, UI and mixed counts, an extraction class and a call, the test counts add to the sample's total, and the record says the call is a label for the reader and plans nothing | Read: the table with its sum line and the sentence | A file has no row or call, the sum is off by one, or the sentence is absent | H |
| A-4 | Agreement decides whether the first rater's labels stand | Given a second rater read the same 10 files blind. When agreement is the unrounded share of tests given the same label. Then **80 % means the share is at least 0.80**: 0.80 stands with no redo and kappa is reported beside it, not gating; 0.7996 does not stand even though it prints as 80.0. Below 0.80, disagreeing tests are listed, the rule's wording is tightened with them, both raters redo those files, and a second result below 0.80 marks the headline "low confidence". One round only | Read: the share as a fraction, before and after, the list, the wording change | A redo at 0.80, no redo at 0.7996, a second round, or an unmarked headline after a second miss | B |
| A-5 | The verdict comes from the lower interval end by the bands written in design | Given Low, Middle and High, each with its interval, and N with its source. When the band is chosen. Then the lower end of the interval is compared. The interval is a percentile bootstrap: files resampled within their group, **10,000 resamples, seed 6538**, the 2.5th and 97.5th percentiles; count and seed are printed | Read: the statistic table and the method line. Search: `git diff` of the verdict rule's text against the DESIGN commit (3ad54cfa0) prints nothing | The point or the upper end picks the band, N has no source, the method line is missing, or the diff is not empty | E |
| A-6 | The heaviest files come from a named green `main` run and stay out of the estimate | Given the `test-timings-frontend` file of one green run. When the ten files with the most seconds are listed. Then the run id and ten rows with seconds are pasted and none enters the estimate unless the pick rule chose it. If the file has expired, another green run is named, and if none exists the seconds line reads "unavailable" and the headline stands on the shares | Run: the `gh` download or listing, pasted | The run id is missing, a failed run's file is used, or seconds are dropped with no mention | E |
| A-7 | The verdict reaches the maintainer before any rewrite is planned | Given the finished audit. When slice A closes. Then the record ends with the verdict block below, and no roadmap changed by this slice holds a step to rewrite or move a test | Search: the roadmap files in the slice's changed paths for "rewrite" and "move", pasted; read: the block is last | A changed roadmap holds such a step, or the block is not the last thing in the record | E |
| A-8 | The skill folder has its files and the checklist comes first | Given slice A done. When the folder is listed. Then `SKILL.md` and `references/frontend.md` exist and the checklist is the first section of `SKILL.md` | Search: listing; read: the first heading | A file is missing, or the checklist is below the rules | H |
| A-9 | Each frontend rule has a title, a measured finding in words and one example | Given `references/frontend.md`. When the rules named below are read. Then each has all three, and the three counts (titles, findings, examples) are equal | Read: the table below filled with the line of each | A rule is missing, has no finding or no example, or two rules share one example | H |
| A-10 | The deliberate break is in `SKILL.md` for both halves | Given `SKILL.md`. When it is read. Then "a check that cannot fail is not a check" has a title, a finding and an example: delete the production line and the test must go red; a test-only change has nothing to mutate, so the break is the proof | Read: the three lines quoted | The rule has no example, or says the break is optional | H |
| A-11 | The pyramid is a direction and a layer rule, with no dated count | Given the skill. When it is read. Then the shares appear once as a direction, "lowest layer that can prove it; time is the gate" appears, and no share is written as a gating limit. The counts are searched in C-4 | Read, quoted | A share is written as a limit that gates | E |

Rules A-9 reads, each with title, finding in words and example (the finding is a sentence, never a Story number):

| Rule | What the example shows |
|---|---|
| Logic on the function, not through a render | A rule asserted on a function, with the render kept for wiring |
| One render per wiring | Several checks sharing one render |
| Small fixtures | A short list where a long one is not the test |
| Fake clock, no real wait | `vi.useFakeTimers({ shouldAdvanceTime: true })`, never a sleep |
| No `testTimeout` or retry as the fix | A slow test made fast, not allowed more time |
| No order dependence | A test that passes alone and in a shuffled run |
| A new view pins its loading look and its failure look | Both looks, a failed load never shown as "no data", E2E waiting for `data-widget-status="ready"` |

### Checks at the end of each slice (rows C)

| # | Scenario | Given / When / Then | Proof and output kept | Can fail when | Kind |
|---|---|---|---|---|---|
| C-1 | Each file ends with its last line when read as a subagent would, after every edit | Given a file ending "End of <file>". When it is read with `ctx_read`, full mode, fresh. Then the last line is quoted. A read cut to a window shows no such line. Run after slice A for `SKILL.md` and `frontend.md`, after slice B for `SKILL.md` and `backend.md` | Read: a quote per file, and one deliberately short read pasted without the line | The line is absent in a full read (shorten the file and read again), or the cut read still seems to show it | E |
| C-2 | Every name the skill gives exists, or is marked planned with a role | Given the skill names identifiers, scripts, folders and tools. When each is searched for in the tree. Then each is found, or its line reads "planned: <role>" such as "planned: the per-tier report", never a number. After slice B the four T5 names (`ServicePortHarness`, `WireContractTest`, `TestHostUseGuardTest`, `Test-Fast.ps1`) carry no planned mark | Search: name, hits, and the marked ones; one known-missing name run to show an empty result | A name has no hit and no mark, a mark carries a number, or a T5 name is still marked | E |
| C-3 | Every quoted guard message is found in the guard it is attributed to | Given a message quoted in the skill, frontend (the per-test limit) or backend (`TestHostPerFixtureGuardTest`, `TestTierGuardTest`, `TestHostUseGuardTest`). When it is searched for in that guard's source at `HEAD`. Then it is found there. If the file differs from `HEAD`, the work stops | Search with `git grep` at `HEAD`, and `git diff --stat HEAD -- <file>` printing nothing | No hit, a hit in another guard, or the file differs from `HEAD` | E |
| C-4 | One search, three pattern lists, finds nothing | Given the skill's folder and, after slice B, the three `CLAUDE.md` lines. When searched for list 1, references: `#` and four digits, `Story`, `slice`, `ADR-`, task names such as `FE7`, `T5`; list 2, scenario tags: `@us-`, `@slice-`, `@kpi-`, `@contract-shape`, `@walking_skeleton`, `@driving_port`, `@real-io`, `@BUG-`; list 3, dated counts: 3,648; 5,857; 9,505; 22,600. Then nothing prints for any list. The skill says "no nWave scenario tags" in words | Search, with each list run once on a file known to hold one of its items (D-6538-21); the rule's own wording is not in the lists | Any hit, including inside an example | E |
| C-5 | Each range changes only the paths it may | Given the range. When `git diff --name-only` over it is read. Then it matches the row of the table below | Search: the diff and `git status --short`, pasted | A path outside the allowed set. At the push, another session's code in the range: do not push | E |
| C-6 | The examples hold nothing token-shaped | Given the examples. When the commit's secret scan runs. Then it passes with no bypass | Run: the hook's own result | The hook rejects: remove the string, never bypass it | E |
| C-7 | The skill review leaves no open critical point | Given `nw-skill-reviewer` on `reviewer_model`, told an untagged example is correct here. When it reviews the skill after each slice. Then no critical point is open after at most two rounds | Read: the verdict per round | A critical point stays open after round two: stop | E |

Ranges for C-5:

| Range | Allowed paths | Must not appear |
|---|---|---|
| The audit's commits (slice A) | `docs/feature/story-6538-t11-test-writing-guidance/` only | Anything under `Lighthouse.Frontend`, `Lighthouse.Backend`, `Lighthouse.EndToEndTests` |
| Slice A's commits | The above (the audit record is `docs/feature/story-6538-t11-test-writing-guidance/audit/frontend-audit.md`), `.claude/skills/test-writing/` | Any guard test, budget file, script under `Scripts/test-timings`, workflow, `docs/ci-learnings.md` |
| Slice B's commits | Slice A's, `CLAUDE.md`, `docs/ci-learnings.md` (the "Where to test" bullet only, answered yes 2026-11-08) | The same list as slice A's, minus those three paths |
| The trial | `docs/feature/story-6538-t11-test-writing-guidance/trial/` | Any test project or other workspace |
| The unpushed range | Under `docs/`, under `.claude/`, or `CLAUDE.md` | Anything else |

### Slice B: the backend half, `CLAUDE.md`, the trial and the record

| # | Scenario | Given / When / Then | Proof and output kept | Can fail when | Kind |
|---|---|---|---|---|---|
| B-1 | Slice B does not start until T5 and the fast cycle rule are on `main` | Given the working tree. When the four T5 names and the fast cycle rule in `CLAUDE.md` are searched for. Then each is found | Search, each name with its hit | A name has no hit: STOPPED, nothing is written for the backend half | E |
| B-2 | Each backend rule has a title, a measured finding in words and one example | Given `references/backend.md`. When the rules below are read. Then each has all three | Read: the table below filled with the line of each | A rule or its example is missing | H |
| B-3 | The T5 convention has one home | Given the distinctive phrase of T5's bullet, copied before it is cut and pasted first into the record. When the tree is searched for it. Then the hits are `references/backend.md` and the one rule line of `CLAUDE.md` | Search, with the phrase | A third file holds it, or `CLAUDE.md` keeps the long body | E |
| B-4 | `CLAUDE.md` gets three lines, in the right place, and T5's bullet is cut to the rule | Given `CLAUDE.md` before and after. When the diff is read. Then three lines are added: the service-port rule, the pointer, the load instruction; the load line sits in the Wave section and names DISTILL, DELIVER and reviewers; T5's bullet is the rule line | Search: `git diff` of `CLAUDE.md`; read: the section | A fourth line, the load line outside the Wave section, a missing name among the three, or the bullet left whole | B |
| B-5 | The DELIVER dispatch wording is written down with the fast cycle rule | Given the rule as `CLAUDE.md` words it. When the wording is written into the record section `## Wave: DELIVER / [REF] Dispatch wording` of this delta. Then it names the skill and the read instruction, and carries the fast cycle rule's distinctive phrases | Read: the section, pasted into the closing record | The section is absent, or lacks a phrase | H |
| B-6 | The fast cycle rule reads the same in three places, with its clause | Given the rule's distinctive phrases, chosen from `CLAUDE.md` and pasted first. When `CLAUDE.md`, `SKILL.md` and the B-5 section are searched for each. Then all three hold every phrase, including the clause that a step whose behaviour lives in a `.test.tsx` or an integration test also runs its own named test | Search, pasted, each phrase marked in each place | A phrase, or the clause, is missing from any one of the three | E |
| B-7 | The trial's feature is small and has scenarios to compare | Given the candidate. When its workspace is read. Then it has a DISTILL section and not more than about 20 scenarios; the count is written | Read, with the count | Larger, or no DISTILL: take the next; if none fits, STOPPED | B |
| B-8 | The checklist's minimum items are fixed before the trial runs | Given the skill's checklist. When its items are written into `## Wave: DELIVER / [REF] Skill checklist, minimum items` and committed. Then it holds at least one item for each rule of A-9, A-10 and B-2, the load-and-quote item and the fast cycle item | Read: each rule with its item. Search: `git log` shows this commit before the trial's output | A rule has no item, or the trial's output is older than the list | E |
| B-9 | The trial wording is the frozen one and gives no hint of the checklist | Given the wording pasted in the record. When it is read against the B-8 items. Then it equals "The trial, as worded" with only angle-bracket parts filled, and no item's phrase appears in it, **except the project's no-tags sentence, which is named and exempt** | Read: the wording, each item struck off | A changed wording, or a non-exempt item phrase in it | E |
| B-10 | The trial agent quotes the last line of each file it read | Given the report. When it is read. Then it quotes "End of SKILL.md" and the half's last line | Read | A line is missing: the trial is void; shorten the file and run once more | E |
| B-11 | A reviewer reads the output against the checklist item by item | Given a reviewer that wrote neither the skill nor the trial, on `reviewer_model`. When it reads the output and the trial writes only in the trial folder (C-5). Then each item is marked followed, not followed or not applicable, with a quoted line. **The tag item is recorded as not measured by the trial** | Read: the reviewer's table | An item has no mark or quote, the reviewer is the writer, or the tag item is marked followed | H |
| B-12 | A deviation is recorded, not fixed silently | Given an item not followed. When the record is written. Then the deviation is listed with the output's line, and any change to the skill is a separate edit after which C-1 to C-4 run again | Read: the list; search: the re-run outputs | The skill changes with no re-run | E |
| B-13 | The deferred check is recorded open, with where and who reads it | Given the closing record. When read. Then it says: the next feature's DISTILL writes "skill loaded: yes/no, deviations" in its `feature-delta.md`, the maintainer reads it after that DELIVER, status open | Read | Status reads passed or done, or no place or reader is named | E |

Rules B-2 reads, each with title, finding in words and example:

| Rule | What the example shows |
|---|---|
| New acceptance scenario goes to the service port | A scenario through the port harness |
| A host test only as a named skeleton or in `Contracts` | One skeleton test named as such |
| The rule by a unit test, the wiring by one host test | The same rule at both levels, one host test |
| No shared state across fixtures | A fixture that builds its own state |
| Fake clock, no real wait | `FakeLighthouseClock`, never the ambient clock |
| A feature's mutation config names `Tests.Acceptance.<Feature>`, with the reason | The folder is outside the integration filter, so unnamed code shows as uncovered |

## Wave: DISTILL / [REF] The audit verdict going to the maintainer

Closes slice A's record. It is the only thing the audit decides.

| Field | Content |
|---|---|
| Commit, file and test counts | From A-1, with any difference named |
| N used and where it came from | Frontend alone is about 84 %; the whole suite only once T6 has labelled the backend |
| Low, Middle, High | Point, lower end, upper end, weighted by test count; the method line of A-5 |
| Seconds | In the logic tests, from the named run (A-6), beside each share |
| Agreement and confidence | From A-4 |
| Band and verdict | One of the four below |
| What the maintainer decides | Whether to plan any rewrite |

| Band | Wording sent to the maintainer |
|---|---|
| 1 | "The logic tests could move to existing functions. Whether moving them pays is for you to decide; time is the rule." |
| 2 | "Small extractions would move the logic. The files are listed." |
| 3 | "Hook or context changes are needed before any move. That is design work." |
| 4 | "Moving logic cannot reach 90 %. Keep the shares as a direction and let time stay the rule." |

A "low confidence" mark from A-4 is printed above the band. No rewrite is planned in this Story under any band.

## Wave: DISTILL / [REF] Stop rules

One form. The record gets a block, and the orchestrator reads it and takes it to the maintainer:

```
STOPPED <date>, row <id>
Evidence: <the output, pasted>
Last completed row: <id>
```

| Stop when | The check that decides | Instead of |
|---|---|---|
| T5 or the fast cycle rule is not on `main` (B-1) | The B-1 search has an empty hit | Writing the backend half, the cut of T5's bullet, or the fast cycle text |
| The listing and `git ls-files` differ and the difference is not named file by file (A-1) | The A-1 comparison | Labelling, or running tests for a count |
| Agreement is below 0.80 after the one round (A-4) | The unrounded share after the redo | A second round; mark "low confidence" and go on |
| The verdict rule was edited after a number was seen (A-5) | `git diff 3ad54cfa0 -- docs/feature/story-6538-t11-test-writing-guidance/feature-delta.md` over the rule's text is not empty, and for the band wording the same diff against this wave's commit | Using the edited rule |
| The same row's check fails a **second** time, meaning the same row, the same pattern, after one fix of the text | The row's own search, run again | Editing the search |
| A quoted guard's file differs from `HEAD` (C-3) | `git diff --stat HEAD -- <file>` is not empty | Quoting it, or waiting |
| The commit hook rejects a file (C-6) | The hook's exit | Bypassing it |
| The skill review has a critical point open after round two (C-7) | The reviewer's second verdict | A third round |
| The trial agent did not see a last line (B-10) | The report lacks the quote | Reading its result. One rerun is allowed after shortening; a second miss is a stop |
| No trial feature fits (B-7) | The scenario count | Using a large one |
| The unpushed range holds a path outside the allowed set (C-5) | The range diff | Pushing |
| A gap in a guard or a budget is found while writing | Reading it | Editing the guard; it goes on the candidate list, and the work goes on |
| The ledger pointer: answered yes 2026-11-08, so this row no longer stops anything. The edit is limited to the "Where to test" bullet | `git diff` of `docs/ci-learnings.md` shows only that bullet | Editing any other line of the file |

## Wave: DISTILL / [REF] Coverage

| Slice | Scenarios | `H` | `B` | `E` |
|---|---|---|---|---|
| A | 11 | 5 | 2 | 4 |
| C (end of each slice) | 7 | 0 | 0 | 7 |
| B | 13 | 3 | 2 | 8 |
| **All** | **31** | **8** | **4** | **19** |

No share is given for errors: seven of the 31 rows are checks whose whole job is to fail on a planted fault, and a percentage over them would say nothing.

| Source | Covered by |
|---|---|
| Brief: the skill has both parts and a checklist; a reviewer pass leaves no open critical point | A-8, A-9, A-10, B-2, B-8, C-1, C-7 |
| Brief: `CLAUDE.md` names the skill and the load instruction, and the convention appears once | B-3, B-4, C-4 |
| Brief: the trial is read against the checklist and written down; the deferred check is recorded open | B-7 to B-13 |
| Brief: the audit's number to the maintainer before any rewrite | A-1 to A-7 and the verdict block |
| Brief: the fast cycle rule in the skill and every DELIVER dispatch | B-5, B-6 |
| Brief: the pyramid as a direction, time the hard rule | A-11 |
| Brief: rules per half, including the deliberate break | A-9, A-10, B-2 |
| DESIGN "what can lie" rows | C-1 (cut file), C-2 (missing names), C-3 (changed quotes), B-6 (drift), B-9 and B-11 (primed trial), B-13 (forgotten load), A-1 to A-6 (biased sample, seconds), B-2 (Stryker config) |
| DESIGN: no new guard; ledger pointer answered yes | C-5 |
| DEVOPS: by-hand searches with pasted output; push check; secret scan; expiring CSV | Every search row, C-5, C-6, A-6 |

**Driving ports.** The reader of the skill is a subagent with `ctx_read`: C-1, B-10. The orchestrator's dispatch: B-5, B-9. The maintainer's read of the record: A-7, B-13. Beyond files, `git`, `gh` and Vitest's listing, all read-only, there is no adapter with real I/O.

**Mandates, for this shape.** No state-delta port, property tests or step layer: there is no test code. Pillar 2: rows build on each other, with C-1 to C-4 reused by both slices and B-12 re-running them. Pillar 3: every proof reads the real tree.

## Wave: DISTILL / [REF] Completeness audit

| Item | Result |
|---|---|
| C1a empty or zero | Pass: A-6 (no file), C-4 |
| C1b each boundary | Pass: A-2, A-4, B-4, B-7 |
| C2a state machine documented | N/A, counted as passing: the only state is the deferred check, open or done (B-13) |
| C2b illegal event per state | Pass: B-13 refuses "passed" |
| C3 zero, one, many | Pass: A-2, B-4, B-7 |
| C4a apply twice | Pass: B-12 re-runs the searches after an edit |
| C4b inverse without prerequisite | Pass: B-1; rollback order is DEVOPS's |
| C5a mode combinations | N/A, counted as passing: no flags |
| C5b flag orthogonality | N/A, counted as passing: no flags |
| C6a malformed input | Pass: A-1, A-6, C-6 |
| C6b each declared error | Pass: every stop row has a row above |
| C6c closed error set | Pass: each row ends in a pass or the one STOPPED block |
| C7a degraded resource | Pass: A-6 |
| C7b interruption | Pass: B-10 voids a cut trial |
| C7c concurrent actors | Pass: C-5 and C-3 guard against other sessions' edits |

**15 of 15: COMPLETE.** No gap needs an upstream answer.

## Wave: DISTILL / [REF] Changed assumptions

Original, DESIGN, "The audit": 408 files, "the two are not reconciled", "408 is used here". New: **410** by `git ls-files` (DESIGN's own open point 6 said so). The listing is compared against 410 and quotas are recomputed from it (A-1).

Original, DESIGN: agreement "at 80 % or more". New: the unrounded share is at least 0.80 (A-4).

Original, DEVOPS "Open for DISTILL": the proofs become the scenarios. New: they do, with added rows for the audit, the rules per half, the dispatch wording, the checklist and the trial. Nothing here changes the architecture.

## Wave: DISTILL / [REF] Review

The first review came back NEEDS_REVISION. What changed:

- **Planted marks.** Carry a role, never a number; the number search stays strict (D-6538-27, C-2, C-4).
- **DELIVER dispatch wording.** B-5 writes it with the fast cycle rule into a named section; B-6 reads that text. The frozen trial wording is unchanged.
- **Content.** Rules per half as read rows with title, finding and example (A-9, B-2), the deliberate break (A-10), and one row fixing the checklist's minimum items before the trial (B-8).
- **Tags.** The project's no-tags sentence is named and exempt in B-9; B-11 records the tag item as not measured; no control trial (D-6538-29).
- **Stops.** One dated block, each row naming its deciding check; "the second time" defined; the edit-after-a-number check is a diff against the design commit. Guard messages: committed source only, and a stop if the file differs, in D-6538-26, C-3 and the stop table, which now agree.
- **Verdict wording.** The reviewer's text for bands 1 to 3; the interval method, 10,000 resamples and seed 6538 named.
- **A-1.** 410 against the listing, no 408 tolerance, any unexplained difference stops.
- **Merged to 31 rows** from 42: the changed-path family (C-5 with a table), names (C-2), last line (C-1), quotes (C-3), three pattern lists (C-4), agreement (A-4), fast cycle (B-6). Row ids were renumbered and every reference follows them.
- **A-7** scoped to roadmaps the slice changed; **A-3** says the call is a label. **80 %** means at least 0.80. **B-4** reads the load line's place and names. The error-share figure is dropped.

**Could not settle.**

- Two references in the review ("631 against 711", "row 635") are line numbers I could not map with certainty; I read them as the clash between "wait for the commit" and "quote nothing from the working tree" and made the quote rule one rule everywhere. If they meant something else, say so.
- The section names for the dispatch wording and the checklist items, and the seed 6538, are my choice.
- Quotas recomputed from 410 need the listing, so they are not in this wave.

**Not verified.** No search, listing or run was made, so none of the 31 scenarios has been seen to fail. The A-2 positions were computed by hand. Whether the trial candidate is small enough is untested.

## Wave: DISTILL / [REF] For the maintainer

Nothing here blocks DELIVER, apart from the order the brief already sets.

1. **Slice B cannot start** until T5 and T6's step 06-05 are on `main` (B-1). Slice A starts now, beside T5's DELIVER (answered 2026-11-08).
2. **The trial's feature is unchecked for size.** Story 6501 is a candidate; B-7 sets the limit and takes the next one if it does not fit.
3. **The trial does not measure the tag rule.** The wording carries your no-tags sentence, so a clean tag search says nothing about the skill (B-11).
4. **Answered 2026-11-08:** the ledger pointer is yes (slice B); the three tool candidates stay out of T11 and become small Stories after it. **Still yours:** the audit verdict when it exists.

## Wave: DISTILL / [REF] Open for DELIVER

- The Vitest listing flags are confirmed on the day. If they would run a test, use the file list and stop.
- Pick the CSV run on the day (A-6). Run 42372930241 was seen to hold it, with no expiry before 2027-02-05.
- Choose the fast cycle rule's phrases from `CLAUDE.md` once step 06-05 has written it, and T5's phrase from its bullet before the cut; paste both first (B-3, B-6).
- No title, comment or file carries a tag. Nothing in `ci*.yml` changes.

## Wave: DELIVER / [REF] Maintainer decisions (2026-11-08)

These answer DESIGN's open points and supersede any earlier line that waits on them.

| # | Question | Answer |
|---|---|---|
| 1 | Start slice A before T5 (DESIGN open point 1)? | **Yes.** Slice A starts now, beside T5's DELIVER. It touches no file T5 touches (T5 works under `Lighthouse.Backend/Lighthouse.Backend.Tests/`); its allowed paths are the feature workspace and `.claude/skills/test-writing/` |
| 2 | Ledger pointer (D-6538-8)? | **Yes.** Slice B makes the "Where to test" bullet in `docs/ci-learnings.md` a one-line pointer to the skill. One home. No other line of that file changes |
| 3 | The trial's feature? | **Story 6501** (ordered parent sources), subject to B-7's size check; the next feature that fits if it does not |
| 4 | Candidate tools? | **Out of T11.** Small Stories after it |
| 5 | Rigor | `standard` (agreed 2026-11-07, recorded in `.nwave/afk.json`): crafter model sonnet, reviewer model haiku |
| 6 | Improve the tests you touch (2026-11-08, afternoon) | **Yes, and it goes in CLAUDE.md, not only the skill.** Slice B adds a **fourth** CLAUDE.md line beside the three B-5 names: when a change touches a test class on either stack, it leaves the class better than it found it. On the backend, the rows it touches move down to the service port or to unit level, so a line comes off `HostUsersStillToMove`. On the frontend, a test moves down a layer the frontend half allows. The skill carries the how. This supersedes the "exactly three lines" of step 02-01's criterion 4, which now reads four. The line holds no Story, slice or ADR number, like the other three |

Roadmap: `deliver/roadmap.json`, two steps, one per slice.

## Wave: DELIVER / [REF] Slice A checks (2026-11-08, step 01-01)

Every output below was run on the day against the tree. Scripts are in `audit/` (`names_check.sh`, `guard_check.sh`, `skill_patterns.sh`, `rule_diff.sh`).
**Audit result:** band 4, low confidence (agreement 0.719008 after the one round); see `audit/frontend-audit.md`, whose last section is the verdict block.

**A-8, A-9, A-10, A-11 (read).** Folder: `.claude/skills/test-writing/SKILL.md` and `references/frontend.md` (listing printed by `ls -R`). The first section of `SKILL.md` after its title is `## Checklist`
(line 12). `frontend.md` holds seven rules: `grep` counts titles 7, findings 7, examples 7 (equal), one example per rule, none shared. `SKILL.md` has the section "A check that cannot fail is not a check"
with a finding ("stays green after the production line is deleted"), an example (`getBubbleSize`) and "the deliberate break is the proof, not an option". The pyramid appears once, as "a target picture, not a
measurement ... a direction and gates nothing ... What gates is time", with the layer rule "A test lands in the lowest layer that can prove it"; no share is written as a limit and no dated count appears.

**C-1 (last line, read with `ctx_read` mode full, fresh).** `SKILL.md` ends `End of SKILL.md`; `frontend.md` ends `End of frontend.md`. Planted fault, a deliberately short read
(`ctx_read` mode `lines:1-20`, fresh) of `SKILL.md`: it returned `SKILL.md 70L lines:1-20` and stopped at line 20, with no `End of` line. The check can fail.

**C-2 (names).** Output of `names_check.sh` (files holding each name, outside the feature workspace and the skill; `git grep -F`):

```
pnpm test:fast                                               9 files
vitest.config.ts                                             50 files
BUDGET_SECONDS                                               3 files
FAST_BUDGET_SECONDS                                          1 files
timeBudgetReporter.ts                                        8 files
slowTests.ts                                                 24 files
src/tests/timeBudget                                         35 files
computePaceBandRects                                         16 files
STATE_BAND_HALF_WIDTH                                        5 files
WorkItemAgingChart.tsx                                       46 files
getBubbleSize                                                6 files
scatterMarkerUtils.tsx                                       3 files
SaveStateIndicator                                           24 files
generateWorkItem                                             10 files
waitPastTheDebounce                                          5 files
pastTheAutosaveDelay                                         15 files
mockSaveTeamSettings                                         5 files
mockGetTeamSettings                                          1 files
COULD_NOT_LOAD_MESSAGE                                       12 files
WidgetShell                                                  134 files
data-widget-status="ready"                                   19 files
vi.useFakeTimers({ shouldAdvanceTime: true })                29 files
Stryker                                                      669 files
Playwright                                                   300 files
pnpm test                                                    276 files
CLAUDE.md                                                    293 files
@screenshot                                                  149 files
@auth                                                        42 files
@rbac                                                        48 files
@proxyauth                                                   12 files
src/tests/timeBudget/slowTests.ts                            15 files
NoSuchNameInTheTreeXyz                                       0 files
paths tracked: yes
```

All names are found. The known-missing name `NoSuchNameInTheTreeXyz` returns 0 files (the planted fault). The backend half and the fast cycle rule's short text are marked `planned:` with a role and no number.
The four backend names that T5 will add are not named in the skill.

**C-3 (guard messages and limits).** Output of `guard_check.sh` (`git grep` at `HEAD`, and `git diff --stat HEAD` for the three files, empty):

```
HEAD: 703cf4cec8311c264b6734ac38d99b1454202ddc
HEAD:Lighthouse.Frontend/src/tests/timeBudget/timeBudgetReporter.ts:32:			`Time budget: ${mode} mode, ${runKind} run, ${workers ?? "default"} workers, ${availableParallelism()} processors`,
HEAD:Lighthouse.Frontend/src/tests/timeBudget/timeBudgetReporter.ts:55:			`Time budget: ${options.mode} mode, budget ${options.budgetSeconds} s, fast budget ${options.fastBudgetSeconds} s, limit factor ${options.runKind === "scaled" ? options.limitFactor : 1}, first-test allowance ${options.firstTestAllowanceSeconds} s, ${verdict.findings.length} offenders`,
HEAD:Lighthouse.Frontend/src/tests/timeBudget/slowTests.ts:1:// A test over its budget is listed here with why it is slow and a ceiling in seconds on a quiet machine.
HEAD:Lighthouse.Frontend/vitest.config.ts:10:const BUDGET_SECONDS = 2;
HEAD:Lighthouse.Frontend/vitest.config.ts:12:const FAST_BUDGET_SECONDS = 0.7;
--- git diff --stat HEAD (nothing below means no difference)
--- end
```

Quoted from `timeBudgetReporter.ts`: `Time budget:` and `offenders`; from `slowTests.ts`: "listed here with why it is slow"; from `vitest.config.ts`: `BUDGET_SECONDS`, `FAST_BUDGET_SECONDS`.

**C-4 (three pattern lists).** `skill_patterns.sh`. Planted fault first, a file holding one item of each list:

```
--- list 1: internal references
planted.md:1:see #6538 and Story 12 and a slice, ADR-270, FE7, T5
--- list 2: nWave scenario tags
planted.md:2:@us-02b @kpi-OUT-1
--- list 3: dated counts
planted.md:3:5,857 tests
```

Then the skill folder, nothing under any list:

```
--- list 1: internal references
--- list 2: nWave scenario tags
--- list 3: dated counts
```

The skill says "Write no nWave scenario tags" in words, and does not hold the tags it lists.

**C-5 (changed paths).** Audit commit `07eaab7d9`: `git diff --name-only 07eaab7d9^ 07eaab7d9` holds 51 paths, all under `docs/feature/story-6538-t11-test-writing-guidance/` (the feature delta,
40 files in `audit/`, 10 in `audit/parts/`); the count of paths outside the workspace is 0. Nothing under `Lighthouse.Frontend`, `Lighthouse.Backend` or `Lighthouse.EndToEndTests`. The skill commit holds
only `.claude/skills/test-writing/` and this section of the feature delta. `git status --short` before the skill commit, with the files that are **not this step's** named:

```
 M Lighthouse.Backend/.../ForecastRealityCheck/ForecastRealityCheckAcceptanceTest.cs     (not mine: parallel agent, backend tests)
 M Lighthouse.Backend/.../ForecastRealityCheck/RealityCheckForecastScript.cs             (not mine)
 M Lighthouse.Backend/.../ForecastRealityCheck/Slice04TheAnswerOpensInADialogScenarios.cs (not mine)
 M Lighthouse.Backend/.../ForecastRealityCheck/Slice04TheAnswerOpensInADialogSpecifications.cs (not mine)
 M Scripts/test-timings/check_budget.py                                                  (not mine: parallel agent)
 M Scripts/test-timings/trx_to_csv.py                                                    (not mine)
?? .claude/skills/test-writing/                                                          (mine, committed next)
?? Lighthouse.Backend/.../ForecastRealityCheck/RealityCheckAtThePortTest.cs              (not mine)
?? Lighthouse.Backend/.../TestHelpers/PortDoubles.cs                                     (not mine)
?? Scripts/test-timings/tests/test_check_budget_tiers.py                                 (not mine)
```

None of those files is staged or committed by this step; no guard test, budget file, `Scripts/test-timings` script, workflow, `docs/ci-learnings.md` or `CLAUDE.md` is touched by it.

**C-6 (secret scan).** The examples hold no token-shaped string. `gitleaks detect --no-git` on the skill folder: "no leaks found". The commit hook's own scan runs at each commit with no bypass.

**C-7 (skill review, `nw-skill-reviewer`, model `haiku` = the profile's `reviewer_model`, told that an untagged example is correct here).**

- Round 1: NEEDS_REVISION, no critical point. One high (checklist item 11 named `pnpm test:fast`, which skips `.test.tsx`; now `pnpm test:fast --changed` for logic tests and `pnpm test --changed` after a component edit), several medium and low
  (fake-clock restore added in rule 4, an empty example body in rule 5 given a real line, a cleanup added to rule 6, the dimmed state asserted in rule 7, item 9 now requires `git diff` to show the file restored, the read mode spelled out, the planned line moved out of the tick list).
- Round 2: NEEDS_REVISION, no critical point. One high (the tag rule would have led an agent to strip the Playwright title tags the package scripts filter on: the rule now names them as real and kept) and four low (rule 7 source label, item 5 wording
  with `shouldAdvanceTime`, the full path of `slowTests.ts`, the pyramid figures marked "a target picture, not a measurement"). All five were applied after round 2; the rule allows no third round, and no critical point was open in either round.
  The pyramid sections and the tag section carry no example of their own, left as they are: the checklist and the layer rule carry them.

## Wave: DELIVER / [REF] Slice A, review fixes (2026-11-08, step 01-01)

One revision pass after the adversarial review (0 critical, 4 high). Audit record: the agreement paragraph and the verdict block now say that 26 of the 86 Logic tests were rated by both raters (Low rests mostly on
unrated tests), report Low/Middle/High on the labels as first written beside the final ones (Middle 17.9 % against 12.6 %, band 4 on both, `audit/bootstrap-output-round1.txt`), describe the tightening as a narrowing of
what counts as Logic, state that the UI-against-Mixed kind was not re-read (High likely understated), say that Low includes the E0 reading (0 % without it), and note the mixed bases of N (0.8373 on the current CSV
total). Row 27 was relabelled (two Mixed tests, class E1); final numbers: Low 9.8 % (0.0 to 21.7), Middle 12.6 % (2.5 to 23.9), High 45.2 % (33.9 to 54.7), band 4. Skill: rule 5 uses `PAGE_SIZE + 1`, rule 2 shows one test
with several expectations, rules 1, 2 and 3 state what was measured, the Stryker claim is softened, the CI limit factor and first-test allowance are named, and the audit pointer is gone.
Re-run after the edits: patterns (nothing printed), names (all found, including `PAGE_SIZE`, `RowRangeFooter`, `LIMIT_FACTOR`, `FIRST_TEST_ALLOWANCE_SECONDS`; the known-missing name returns 0), guards (`git diff --stat HEAD` empty), the rule
diffs (empty, 18 and 7 lines), and a full fresh read of both skill files, ending `End of SKILL.md` and `End of frontend.md`; outputs in `audit/names-output.txt` and `audit/guard-output.txt`.

## Wave: DELIVER / [REF] Dispatch wording

The wording the orchestrator puts into every DISTILL and DELIVER dispatch, and into every reviewer's. It names the skill and the read instruction, then carries the fast cycle rule as `CLAUDE.md` words it.

```
Before you write, change or plan any test or scenario, read with ctx_read in full mode and fresh=true the skill
.claude/skills/test-writing/SKILL.md and the half for your stack in .claude/skills/test-writing/references/ (frontend.md or backend.md).
Each file ends with a line "End of <file>". Quote that last line back in your report, one per file. If you did not see it, read the file again in pages.
Inside a DELIVER step, work on the fast loop; the full gates run later.
Red, green and refactor use `pnpm test:fast --changed` (`pnpm test --changed` after a component edit) and
`Lighthouse.Backend/Test-Fast.ps1`, which builds first,
plus the test the step names, whatever its tier (a step whose behaviour lives in a `.test.tsx` or an integration test also runs its own named test).
The full gates run once at the end of a slice and before every push.
A clean fast loop never replaces them.
```

## Wave: DELIVER / [REF] Slice B checks (2026-11-08, step 02-01, part 1)

RED: SKIPPED, `NOT_APPLICABLE: guidance text; the searches in criteria 1, 3, 5, 7 and 16-18 are the proof`.

**B-1 (T5 and the fast cycle rule are on `main`).** `git merge --ff-only origin/main`: "Already up to date." Each name searched with `git grep -l` outside `docs/`: `ServicePortHarness` (`CLAUDE.md`, `TestHelpers/ServicePortHarness.cs`, `TestHostUseGuardTest.cs` and others), `WireContractTest` (`Contracts/WireContractTest.cs`, the guard, 7 files), `TestHostUseGuardTest` (`Architecture/TestHostUseGuardTest.cs`, its `HostUsersStillToMove` part and `TestHostPerFixtureGuardTest.cs`), `Test-Fast.ps1` (`Lighthouse.Backend/Test-Fast.ps1`, `TestSelectionClauseTest.cs`, `CLAUDE.md`). The fast cycle rule is in `CLAUDE.md` ("Inside a DELIVER step, work on the fast loop; the full gates run later."). All found; nothing stopped.

**B-3 (one home for T5's convention).** Distinctive phrase, copied before the cut: `New backend acceptance scenarios drive the application-service port.` The bullet was 9 lines (`CLAUDE.md` lines 134 to 142 before). `git grep -n --untracked` for the phrase now prints `references/backend.md:7` (rule 1's title), `CLAUDE.md:134` (the one rule line), and the archived T5 workspace (`docs/feature/story-6469-t5-acceptance-at-the-service-port/` delta line 379 and `roadmap.json`), which is history and outside this step's allowed paths, plus this section. `CLAUDE.md` keeps no long body. The three ways out are in `backend.md` word for word, and its four fragments are found at `HEAD` in `TestHostUseGuardTest.cs` by `git grep -c -F` (1 each); `git diff --stat HEAD -- Lighthouse.Backend` prints nothing.

**B-4 / criterion 4 (`CLAUDE.md`, four added lines).** The diff: T5's bullet cut to its rule line (a), then three bullets: the pointer (b), the load instruction naming DISTILL, DELIVER and reviewers, in the Wave section (c), and "Improve the tests you touch" (d), per maintainer decision row 6. No fifth line changed. Number search over lines 134 to 147 (`story|slice|ADR-|#NNNN|T5|FE7`, case-insensitive): NONE.

**B-6 (the fast cycle rule in three places).** Phrases chosen from `CLAUDE.md`, one line each, and counted in `CLAUDE.md`, `SKILL.md` and the "Dispatch wording" section: "work on the fast loop; the full gates run later." 1/1/1; "`pnpm test:fast --changed` (`pnpm test --changed` after a component edit)" 1/1/1; "`Lighthouse.Backend/Test-Fast.ps1`, which builds first," 1/1/1; "plus the test the step names, whatever its tier" 1/1/1; "run once at the end of a slice and before every push." 1/1/1; "A clean fast loop never" 1/1/1. **The clause** that a step whose behaviour lives in a `.test.tsx` or an integration test also runs its own named test: `CLAUDE.md` carries it as "plus the test the step names, whatever its tier" (1/1/1); `SKILL.md` and the wording add the gloss "also runs its own named test" (0 in `CLAUDE.md`, 1, 1). The existing `CLAUDE.md` bullet is not one of the four lines, so the gloss was not added there; flagged for the reviewer.

**B-7 (the trial's feature).** Story 6501 (`docs/feature/story-6501-ordered-parent-fields/`) has DISTILL sections, but its scenario tables hold 21 (slice 01) + 7 (slice 02) + 8 (slice 03) = **36 scenarios** (57 test methods), more than about 20: it does not qualify. The next feature that fits: **Story 6083** (`docs/feature/story-6083-over-time-history-fill-on-by-default/`), delivered (mutation gate commit `d5bc64ab8`), with a DISTILL section "Scenario list with tags" of **11 scenarios** (8 acceptance methods, 10 cases, and 3 unit tests). Its stories are in the DISCUSS sections of its `feature-delta.md` and in `slices/`; the workspace has no `discuss/` folder (6501 has none either). Backend, so the trial reads `backend.md`.

**Criterion 8.** `docs/ci-learnings.md`: the "Where to test" bullet is the one-line pointer; `git diff --stat` shows that one hunk (+1 -10), no other line.

**C-1 (last lines, read fresh in full mode).** `SKILL.md` ends `End of SKILL.md`; `backend.md` ends `End of backend.md`; `frontend.md` ends `End of frontend.md`. A deliberately short read, `backend.md` `lines:60-70`, printed `backend.md 96L lines:60-70` and lines 60 to 70 with no such last line.

**C-2 (names).** Found by `git grep -l -F` in `Lighthouse.Backend` and `CLAUDE.md`: `ServicePortHarness` 4, `WireContractTest` 7, `TestHostUseGuardTest` 4, `HostUsersStillToMove` 3, `SkeletonsByUserFlow` 1, `FakeLighthouseClock` 81, `RealityCheckWireContractTest` 4, `ForecastRealityCheckServiceTest` 2, `ForecastDataSufficiencyPolicy` 5, `TestDatabaseTemplate` 4, `NewScope` 6, `RegisterWhatTheCheckReads` 1, `Tests/Contracts/` 1, `Lighthouse.Backend/Test-Fast.ps1` 2, `FullyQualifiedName!~API.Integration` 1, `service-port-` 1. Known-missing `NoSuchNameXyz`: 0. The one marked name: `Tests.Acceptance.<Feature>` ("planned: the first feature to put port scenarios there", no number). The four T5 names carry no planned mark.

**C-3 (guard messages at `HEAD`).** The three ways out and "Its line goes in this commit: take it off the list, which names only types that still start a host." are each found once in `Architecture/TestHostUseGuardTest.cs` by `git grep -c -F ... HEAD`; `git diff --stat HEAD -- Lighthouse.Backend` printed nothing.

**C-4 (patterns).** Each list run first on a planted file (`planted.txt`): list 1 hit 2 lines, list 2 hit 1, list 3 hit 1. On the skill folder plus `CLAUDE.md` lines 134 to 147: list 2 and list 3 print nothing; list 1 prints one line, `SKILL.md:42` "The full gates run once at the end of a slice and before every push." (the word "slice" of the fast cycle rule, with no number: the pattern `slice[ -]?[0-9a-z]` also matches "slice and"; `CLAUDE.md`'s own rule has the same words).

**C-5 (changed paths).** `git status --short` before the commit: the skill folder, `CLAUDE.md`, `docs/ci-learnings.md` and this delta; nothing else.

**C-6.** The examples hold no token-shaped string (`gitleaks` is not on the shell allowlist here); the commit hook's own scan runs at each commit, no bypass.

## Wave: DELIVER / [REF] Skill checklist, minimum items

Fixed and committed before the trial runs (B-8). Each row: the rule it answers, the item the reviewer marks followed, not followed or not applicable with a quoted line, and the phrase it is searched by in the frozen wording (B-9).

| # | Rule | Item | Phrase checked against the wording |
|---|---|---|---|
| 1 | Frontend 1 | A rule is asserted on a function or hook, with no render | `on a function or hook, with no render` |
| 2 | Frontend 2 | Several checks of one wiring share one render | `share one render` |
| 3 | Frontend 3 | The fixture is the smallest the behaviour allows | `smallest the behaviour allows` |
| 4 | Frontend 4 | Time is a fake clock and no sleep | `a fake clock and no sleep` |
| 5 | Frontend 5 | A slow test is made smaller, with no longer timeout and no retry | `no longer timeout` |
| 6 | Frontend 6 | A test passes alone and shuffled, building its own state | `passes alone and shuffled` |
| 7 | Frontend 7 | A new view pins its loading look and its failure look | `loading look and its failure look` |
| 8 | Frontend 8 | A touched frontend test moves down a layer | `touched frontend test moves down` |
| 9 | The deliberate break | The test was shown red by deleting or inverting its production line, then the line was restored | `shown red by deleting or inverting` |
| 10 | Backend 1 | A new scenario starts at the application-service port | `starts at the application-service port` |
| 11 | Backend 2 | A host test is a named skeleton or sits in Contracts | `named skeleton or sits in Contracts` |
| 12 | Backend 3 | The rule is pinned by a unit test and the wiring by one host test | `wiring by one host test` |
| 13 | Backend 4 | No state is shared across fixtures | `shared across fixtures` |
| 14 | Backend 5 | Time is `FakeLighthouseClock`, never the ambient clock | `ambient clock` |
| 15 | Backend 6 | The feature's mutation config names the acceptance folder of the feature | `mutation config names` |
| 16 | Backend 7 | A touched backend class sheds rows from `HostUsersStillToMove` | `sheds rows from` |
| 17 | Touch it, improve it | A touched test class ends better than it began, on either stack | `ends better than it began` |
| 18 | Load and quote | The report carries the closing line of each file read | `closing line of each file read` |
| 19 | The fast cycle | The step names the fast-loop commands and its own named test | `names the fast-loop commands` |
| 20 | No tags | No nWave scenario tags in anything written. **Not measured by the trial** (the wording's own last sentence says so); recorded as not measured | exempt: the project's no-tags sentence |

20 items.

## Wave: DELIVER / [REF] Trial wording, frozen (feature: Story 6083)

The wording of "The trial, as worded" with only the angle-bracket parts filled: `<feature>` is Story 6083 (the over-time history fill on by default), `<feature workspace>` is `docs/feature/story-6083-over-time-history-fill-on-by-default`, `<scratch workspace>` is `docs/feature/story-6538-t11-test-writing-guidance/trial`. Note for the orchestrator: that workspace has no `discuss/` folder, so its stories are read from the DISCUSS sections of its `feature-delta.md` and from `slices/`; the frozen sentence is left as it is.

```
You are the acceptance designer for Story 6083 (the over-time history fill on by default), a feature that is already delivered. Its stories are in docs/feature/story-6083-over-time-history-fill-on-by-default/discuss/.
First read, with ctx_read in full mode and fresh=true, .claude/skills/test-writing/SKILL.md and the file in
.claude/skills/test-writing/references/ for your half (frontend.md or backend.md). Each file ends with a line "End of <file>".
Quote that last line back in your report, one per file. If you did not see it, say so and read the file again in pages.
Then write the acceptance scenarios for Story 6083 (the over-time history fill on by default) into docs/feature/story-6538-t11-test-writing-guidance/trial/ and nowhere else. Write no test file and no production file.
Report: the files you read with their last lines, the scenarios you wrote, and any rule of the skill you did not follow and why.
No nWave scenario tags (@us-, @slice-, @kpi-, @contract-shape and the like) in anything you write.
```

Overlap check (B-9): each of the 19 phrases of the table, searched with `grep -c -i -F` in the block above, has 0 hits (control: "full mode", a phrase that is in the wording, has 1 hit). The block equals "The trial, as worded" with only the three angle-bracket parts filled (`diff` against the original with those parts substituted: no difference). The one exempt sentence is the last line, the project's no-tags sentence.

## Wave: DELIVER / [REF] Trial and review (2026-11-08, step 02-01, part 2)

The trial was one `nw-acceptance-designer` dispatch on sonnet with the frozen wording; its output is `trial/story-6083-acceptance-scenarios.md` (101 lines, committed at dd07b8c70). A separate reviewer on haiku scored it against the 20 checklist items. The table is the reviewer's, verbatim.

| # | Item | Verdict | Quote / reason |
|---|---|---|---|
| 1 | Frontend 1, rule on fn/hook | N/A | Line 3: "Backend story." |
| 2 | Frontend 2, one render | N/A | Backend-only output. |
| 3 | Frontend 3, smallest fixture | N/A | Frontend rule; backend fixture line: "A row is one `OptionalFeatures` row plus at most one `AppSettings` row." |
| 4 | Frontend 4, fake clock | N/A | "no clock (nothing here reads time, so no `FakeLighthouseClock`)" (claim not checked against source) |
| 5 | Frontend 5, slow test | N/A | No frontend test; no timeout or retry mentioned. |
| 6 | Frontend 6, alone and shuffled | Not followed (owed) | "run the class alone, in file order and shuffled." Nothing run; no code written. |
| 7 | Frontend 7, loading/failure look | N/A | No view. Report says "nothing visible"; output silent. |
| 8 | Frontend 8, touched frontend test moves down | N/A | No frontend test touched. |
| 9 | Deliberate break | Not followed (owed) | "Break check: for each of scenarios 1, 4 and 8, invert the seeded `Enabled` value..." Not run. |
| 10 | Backend 1, starts at port | Followed | "built with `ServicePortHarness`" under `Tests/Acceptance/OverTimeHistoryFill/` (scenarios 1-7). |
| 11 | Backend 2, skeleton or Contracts | Followed | "one row on `WireContractTest` in `Tests/Contracts/`"; "Skeleton \| none". |
| 12 | Backend 3, unit plus one host test | Followed | "unit test next to `OptionalFeatureSeeder`" (8); one host row (9). |
| 13 | Backend 4, no shared state | Followed, with caveat | "each scenario builds its own harness. ... No shared state." But Givens of 4-7 point at "scenario 2's start" and "scenario 1's start". |
| 14 | Backend 5, FakeLighthouseClock | N/A | "no clock (nothing here reads time...)". Unverified. |
| 15 | Backend 6, mutation config names folder | Followed | "the test-case filter names `Tests.Acceptance.OverTimeHistoryFill` as its added last term". |
| 16 | Backend 7, sheds HostUsersStillToMove rows | Not followed (unaddressed) | Output adds a row to `WireContractTest` but never says whether it is on the guard list. |
| 17 | Touch it, improve it | Not followed (unaddressed) | Same touch of `WireContractTest`; no improvement stated. |
| 18 | Load and quote closing lines | Followed (report claim only) | Output file quotes nothing. Report claims "End of SKILL.md", "End of backend.md", "End of frontend.md". |
| 19 | Fast cycle named | Followed | "Run `Lighthouse.Backend/Test-Fast.ps1` and the named scenarios". Named, not run. |
| 20 | No tags | Not measured | Output says "Plain words, no tags." Scan: no `@`-tags; only NUnit `[TestCase]`. |

### Deviations the reviewer listed, each with its output line

Nothing here is fixed in the trial output, and no deviation was resolved silently. Output lines are those of `trial/story-6083-acceptance-scenarios.md`.

| # | Deviation | Output line |
|---|---|---|
| 1 | The Givens of scenarios 4 to 7 chain to other scenarios, so those scenarios are not self-contained. | 51 ("Given scenario 2's start ..."), 58, 64 and 71 ("Given scenario 1's start") |
| 2 | Scenario 8 is unit-level but numbered with the acceptance scenarios. | 76 (heading "8. The once-only rule (unit level, no harness)") |
| 3 | The coverage line's count disagrees with its own list: it names 3, 4, 5, 6, 7, 9 and the off rows of 8, then says "6 of 9". | 95 |
| 4 | "Nothing here reads time" is asserted but not shown. | 17 |
| 5 | Items 6, 9 and 11 are owed: no code was written, no break run, no test run. | 100 and 101 (the "Checks owed at hand-over" list) |
| 6 | A touched existing class is not checked against the guard list. | 12 (the `WireContractTest` row), and nowhere else |
| 7 | The loading/failure reasoning appears only in the trial agent's report, not in the output. | none in the output; the report only |

### The reviewer's judgements

- Placing the seeder "at the port" under backend rule 1 is a stretch. Rule 1 names application services, so the skill should cover seeders. (Acted on below, edit (a).)
- Scenario 9, the 403 wire contract, is right under rule 2.

### Orchestrator's notes

- **Items 16 and 17 are arguably misapplied. Both readings stand.** Reading one (the reviewer's): the output adds a row to `WireContractTest`, a hosted class, so rule 7 and "touch it, improve it" apply and the output is silent on both. Reading two (the orchestrator's): scenario 9 adds a **new** wire-contract class and does not touch an existing hosted class, so there is no row to shed from `HostUsersStillToMove` and no existing test to improve. The output's line 12 says "one row on `WireContractTest`", which is ambiguous between the two. This record does not choose; the checklist items stay as frozen, and the maintainer may rule on them.
- The trial agent did not read the nw-* skill files, and it skipped Phase 0 because the brief asked for scenarios only. The trial therefore measured the skill's reach on an agent without the nWave method loaded, which is the weaker setting.
- The workspace has no `discuss/` path. The frozen wording names one, a dead path. The agent read the stories from the Story 6083 workspace instead.
- Item 18 is a report claim only: the output file quotes no closing line, so the check that the files were read in full rests on the agent's word.

### Skill edits made because of the trial

Two edits, each made separately and each followed by the same searches (below). The reviewer's judgement and deviation 1 are the two real lessons; nothing else in the skill was changed.

**Edit (a), rule 1 of `references/backend.md`: code that is not an application service.** A new paragraph, "Code that is not an application service", after the three ways out. A start-up seeder, a background job or a hosted service is driven at the port by running it against the harness's database context and services directly, with no host (build the harness, take a scope, hand the code the scope's context and services, call its entry method, read the result from the database). If that cannot be done it becomes a named skeleton with its user flow, and the scenario says why. Answers the reviewer's judgement and the trial's stretch of "at the port" for the seeder. Not verified against a real seeder in this step: it states the way the harness is built (rule 1's own example), and the next feature that has a seeder tests whether the wording is enough.

**Edit (b), rule 4 of `references/backend.md`: a Given is stated in full.** A new paragraph, "A scenario's Given is stated in full": a Given never refers to another scenario's set-up or outcome; where scenarios start alike each lists the rows again, or they call a shared named builder, never a state an earlier test left behind. Answers deviation 1.

**Searches after edit (a), then again after edit (b).** Script: one `checks.sh` run twice, output saved after each edit; the two outputs are identical (`diff` prints nothing, exit 0), as expected, because neither paragraph adds a name, a reference or a pattern hit.

- Last lines, read fresh from disk with `tail -n 1`: `SKILL.md: End of SKILL.md`; `references/backend.md: End of backend.md`; `references/frontend.md: End of frontend.md`. `backend.md` is now 108 lines.
- Names. Backend names, `git grep -l -F` outside the workspace and the skill, files: `ServicePortHarness` 14, `WireContractTest` 22, `TestHostUseGuardTest` 14, `HostUsersStillToMove` 6, `SkeletonsByUserFlow` 3, `FakeLighthouseClock` 106, `RealityCheckWireContractTest` 13, `ForecastRealityCheckServiceTest` 7, `ForecastDataSufficiencyPolicy` 23, `TestDatabaseTemplate` 7, `NewScope` 9, `RegisterWhatTheCheckReads` 1, `Tests/Contracts/` 10, `Lighthouse.Backend/Test-Fast.ps1` 6, `FullyQualifiedName!~API.Integration` 65, `service-port-` 1; known-missing `NoSuchNameXyz` 0. Marked planned in the skill: `Tests.Acceptance.<Feature>`. The frontend names script ran as well: every frontend name found, `NoSuchNameInTheTreeXyz` 0 files, "paths tracked: yes". The counts are higher than at C-2 because the tree has moved since then (more files use these names); none dropped to 0.
- Guard quotes, `git grep -c -F` at `HEAD` (dd07b8c70 plus the working edits, which do not touch `TestHostUseGuardTest.cs`), file `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.cs`: "move it to the application-service port" 2; "list it as a skeleton with its user flow" 2; "or make it a wire contract on WireContractTest" 1; "Its line goes in this commit" 2. The frontend guard strings (`Time budget:`, ` offenders`, `listed here with why it is slow`, `BUDGET_SECONDS = `) are found at `HEAD` too.
- Pattern lists, positive control first: the planted line `Story 6083 #1234 @us-01 3,648` hit all three lists (list 1, list 2, list 3: one line each). On the skill folder: list 2 and list 3 print nothing; list 1 prints one line, `SKILL.md:42` "The full gates run once at the end of a slice and before every push." the same single, accepted hit as at C-4 (the word "slice" with no number, in `CLAUDE.md`'s own rule too). Neither new paragraph adds a hit.
- B-3, `git grep -n --untracked -F` for "New backend acceptance scenarios drive the application-service port." outside this workspace and the archived T5 workspace: `references/backend.md:7` and `CLAUDE.md:134` (the one rule line). Nothing else.

## Wave: DELIVER / [REF] Closing record of Story 6538's skill trial

**Deferred check: OPEN.** It is not passed and not done. The trial above was run on a feature that was already delivered, by an agent without the nWave method loaded, on one story, and its output is scenarios on paper: the reviewer's owed items (alone and shuffled, the deliberate break, a test run) could not be measured because no code was written. What would settle it is the next feature's DISTILL, which writes "skill loaded: yes/no" and the deviations from the skill's rules into its own `feature-delta.md`; the maintainer reads that after its DELIVER and rules on whether the skill changed what was written. Until then the status stays open, and no step or Story may cite the trial as evidence that the skill works.

What the trial did show, without settling anything: of the 20 items, 6 were followed, 1 followed with a caveat, 4 not followed (items 6 and 9 owed, items 16 and 17 unaddressed and disputed above), 8 not applicable to a backend story, and 1 not measured. Two rule gaps were found and taught (edits (a) and (b)); five further deviations (2, 3, 4, 5, 7 above) sit in the trial output and are recorded, not fixed, because the output is the trial's evidence and stays as the designer wrote it.

**Criterion 19, the unpushed range.** `git log origin/main..HEAD` after `git fetch` holds 7 commits (1484f4099, 8dd59f6fb, a25c8fdc2, 2fa7efeee, ee975e808, e7e78638b, dd07b8c70). `git log origin/main..HEAD --name-only --format= | sort -u` prints these paths, exactly:

```
.claude/skills/test-writing/SKILL.md
.claude/skills/test-writing/references/backend.md
.claude/skills/test-writing/references/frontend.md
CLAUDE.md
docs/ci-learnings.md
docs/feature/story-6536-t9-per-tier-ratchets/feature-delta.md
docs/feature/story-6538-t11-test-writing-guidance/feature-delta.md
docs/feature/story-6538-t11-test-writing-guidance/trial/story-6083-acceptance-scenarios.md
Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.cs
Scripts/test-timings/test-time-budget.json
```

**Criterion 19 is not met as worded**: two paths are outside docs/, .claude/ and CLAUDE.md. Both come from other sessions' commits: `Scripts/test-timings/test-time-budget.json` from a25c8fdc2, and `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostUseGuardTest.cs` from e7e78638b. Neither is this step's work. Whether to push the range, or to hold it back, is the orchestrator's call; nothing was pushed.
