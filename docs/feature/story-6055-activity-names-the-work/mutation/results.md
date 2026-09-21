# Mutation run — story-6055-activity-names-the-work

Run 2026-09-21 against frozen code at `e6ddbc7df`, after the L1-L6 refactor pass and after the
adversarial review's findings were closed. Project gate: **≥ 80% kill rate**.

## Frontend — StrykerJS

`stryker.6055.frontend.json` + `vitest.stryker.6055.config.ts`, mutating
`src/components/App/Header/TaskManager/ActivitySection.tsx`.

| | |
|---|---|
| **Score** | **83.33% total, 88.71% covered** — gate met |
| Killed | 55 |
| Survived | 7 |
| No coverage | 4 |
| Timeouts / errors | 0 |
| Duration | 1m 07s, 11.83 tests per mutant |

**Every mutant in the code this story wrote was killed.** `WORK_VERBS`, `WORK_NOUNS`,
`WORK_SUBJECT_TERMS`, `describeWait` and the `isSameEntity` branch have no survivors. The eleven
non-killed mutants are styling, one whitespace literal, and code this story did not touch.

### Survivor triage — one line each

| Line | Mutant | Verdict |
|---|---|---|
| 196 | `sx={{ display: "flex", … }}` → `{}` | **Equivalent for test purposes.** MUI layout props. No test asserts row layout and none should — an assertion on flex direction pins a design decision, not a behaviour. |
| 196 | two `StringLiteral` inside that same `sx` | Same. |
| 203 | `sx={{ flexGrow: 1 }}` → `{}` | Same. |
| 205 | the JSX `{" "}` before the em-dash → `""` | **Equivalent in practice.** React Testing Library's `toHaveTextContent` normalises whitespace, so a missing single space between the name and the separator is invisible to every assertion. Killing it needs a full-row string comparison, which would pin the separator's exact spacing and break on any future re-layout. Accepted. |
| 209 | `Tooltip title={`Stop refreshing ${task.name}`}` → `` | **A real gap, and not this story's.** The stop control and its tooltip are epic 5511 slice 04's; nothing asserts the tooltip text. Worth a test, in that Epic's file rather than here. |
| 222 | `event.currentTarget.closest(…)?.focus()` → drop the `?.` | **A real gap, and not this story's.** Also 5511 slice 04. The optional chain guards the case where the row element has gone between render and click; no test stages that. |

### No-coverage triage

All four are the *finished* state: the `default:` arms of `activityOf` (99), `describeState` (129) and
`RowProgress` (156), plus `return "finished"` (100).

The Activity list is a read over work whose status is `Queued` or `InProgress` — the controller filters
to those two before the response is built — so a finished row cannot reach this component through the
product. The branches exist as defensive handling of a `UpdateProgress` the type permits. Reaching them
in a test means constructing a task the endpoint cannot emit, which would pin a shape rather than a
behaviour. Left uncovered deliberately.

## Backend — Stryker.NET

`stryker.6055.backend.json`, mutating `UpdateController.cs`, `UpdateEntityKinds.cs` and
`UpdateTaskNaming.cs`.

**Note on the test-case filter, because it is not the project's usual one.** The precedent configs
exclude `API.Integration`, since those fixtures each boot a web host and take a full-solution run from
three minutes to over eighty. That exclusion cannot be used here: `UpdateControllerTest.cs` covers only
`GetUpdateStatus`, and there is no unit test at all for `UpdateTaskNaming` or `UpdateEntityKinds` — so
**every test covering this story's backend logic is an acceptance test in `API.Integration`.** Running
with the usual filter would have scored these three files near zero and the number would have described
the filter rather than the suite.

The filter is therefore narrowed to the area instead of the layer: `FullyQualifiedName~TaskManager`
plus `UpdateControllerTest`. That subset runs in about 45 seconds, so the eighty-minute problem does
not arise — it was never the acceptance tests as such, it was running all of them.

| | |
|---|---|
| **Score** | **97.30%** — gate met |
| Tested | 37 |
| Killed | 36 |
| Survived | 1 |
| Timeouts / errors | 0 |
| Duration | 8m 59s |

19 374 mutants were skipped by the project's existing `// Stryker disable` annotations elsewhere in the
solution; 37 in the three mutated files were tested.

### Survivor triage

| Line | Mutant | Verdict |
|---|---|---|
| `UpdateController.cs:130` | `elapsed < TimeSpan.Zero` → `elapsed <= TimeSpan.Zero` | **Genuinely equivalent — unkillable by any test.** When `elapsed` is exactly zero, `<` is false and the expression returns `(long)elapsed.TotalMilliseconds`, which is 0; `<=` is true and returns the literal `0`. Both branches produce the same value, so no input distinguishes them. |

That is 36 of 37 killed and one that cannot be killed: **every killable mutant in these three files died.**

The adversarial review predicted this survivor before the run and pre-classified it as equivalent,
explicitly so it would not be re-litigated here. It also named the meaningful neighbours that *are*
killed, which is the part that matters: `>` is killed by slice 03's duration scenarios, and returning
`null` instead of `0` is killed by the scenario about a replica whose clock runs behind. The clamp is
pinned; only the boundary that makes no difference survives.

The review's other prediction for this file — `FirstOrDefault` → `LastOrDefault` on the lane-holder
selection — did not appear as a survivor.

## What mutation could not have told us

The adversarial review that ran before this found a hole mutation is structurally unable to see: the
`holdingTheLane.Id == work.Id` conjunct of the sameness test was unprotected, because the only scenario
with two same-kind different-id rows asserted the holder's *name* and never looked at `isSameEntity`.
Dropping a conjunct is not a mutation Stryker.NET emits — its visible neighbours (`&&`→`||`, `==`→`!=`)
are all killed — so the score would have read as fully covered over the one gap that mattered.

Recorded here because a mutation score is evidence about the mutations a tool can express, and it is
easy to read it as evidence about the suite.
