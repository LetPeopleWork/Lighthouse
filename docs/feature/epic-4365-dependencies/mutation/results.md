# Mutation testing — Epic 4365

## Slice 03 (Jira and Linear read their own dependency links)

Run 2026-08-21 against `main` @ `761661d6d`. Gate is 80 % kill rate on each stack touched. The frontend
is untouched by this slice, so StrykerJS was not run — N/A, not skipped.

### Backend (Stryker.NET) — **83.87 %** on the lines this slice wrote (52 killed / 62 run)

| file | killed | survived | score |
| --- | --- | --- | --- |
| `Jira/IssueExtensions.cs` | 34 | 8 | 81.0 % |
| `Jira/JiraWorkTrackingConnector.cs` | 9 | 2 | 81.8 % |
| `Linear/LinearWorkTrackingConnector.cs` | 7 | 0 | 100 % |
| `Models/Feature.cs`, `Jira/JiraFieldNames.cs` | 2 | 0 | 100 % |

**Stryker's own headline number for the same run is 41.92 %, and it does not describe this slice.**
The `mutate` filter is ignored in this repository — slice 02 recorded that, and it was re-confirmed here
twice, with and without the `solution` key, both times producing 16 175 mutants over the whole project.
What does work is `--since`, which narrows execution to files changed against a target: 15 204 mutants
skipped, **971 tested**, 35 minutes. But `--since` scopes to whole *files*, and two of the files this
slice touched are ~2 600 lines of connector it never opened, so 41.92 % is mostly a verdict on
pre-existing code.

The slice number above comes from `score_the_slice.py`, which intersects the report's per-mutant line
numbers with the diff. That is the only line-level scoping available here: Stryker's line-span syntax
(`File.cs{120..180}`) rides on the same ignored `mutate` key. `--since` also needs a branch or tag
rather than a bare SHA, hence the throwaway `slice03-baseline` ref.

### Every survivor, and why eight of them cannot be killed

Ten mutants survived on this slice's lines. Eight are equivalent mutants:

- **`||` → `&&` in the four JSON guards** (`InwardNameOf`, `KeyOf`). When `TryGetProperty` returns
  false the out-parameter is `default(JsonElement)`, whose `ValueKind` is `Undefined` and therefore
  already fails the second half of the test. Both forms return empty for the same inputs.
- **`string.Empty` → a junk string in `InwardNameOf`.** A name that is not `is blocked by` is skipped
  whatever it says, so no caller can tell the difference.
- **`?? string.Empty` on both tails.** Unreachable: the `ValueKind != String` guard above already
  excludes JSON null, so `GetString()` cannot return null there. Dead defensive code, and Stryker's
  `NoCoverage` label for it is the honest one.

**Two were real, and both were about the sentence an operator reads** — the same class slice 02's
frontend run turned up. `.Order()` → `.OrderDescending()` and the `", "` separator → `""` both survived
because the test asserted each link name appeared *somewhere* in the warning; "is halted bywaits for"
passes that just as happily. It now asserts the rendered list, `is halted by, waits for`, which pins
order and separator together. Not re-scored — the run is 35 minutes and the gate was already met.

## Slice 02 (what exactly, and what Lighthouse cannot act on)

Run 2026-08-19 against `main` @ `15a0a2942`. Gate is 80 % kill rate on each stack touched.

### Frontend (StrykerJS) — **100.00 %**, 52 mutants, 0 survivors

| file | mutants | killed |
| --- | --- | --- |
| `utils/dependencies/dependencySentences.ts` | 29 | 29 |
| `FeatureListDataGrid/WarningsIndicator.tsx` (the all-clear decision, the warning kind, the sentence) | 22 | 22 |
| `DependencyDialog.tsx` (whether an entry says why) | 1 | 1 |

Config: `stryker.4365.slice02.frontend.mjs`, `vitest.4365.slice02.ts`, report
`stryker-4365-slice02-frontend.json`.

**The first run scored 68 %, and the gap was worth having.** Of 30 survivors, two kinds:

- **Sentences nobody had pinned against a literal.** Blanking `DONE_WITH_REMAINING_WORK_TOOLTIP` to an
  empty string survived, because the test asserted only that an `aria-label` attribute existed. Same for
  the default-size sentence. This is the self-satisfying copy test the roadmap warned about, and it was
  already there before this slice — now both are pinned to their exact wording.
- **A branch that was reachable and untested.** `reasonSentence(null, …)` returning `""` survived: a
  withheld entry with nothing wrong with it is a real case (the reader may not see the Feature, and the
  dependency is fine) and no test covered it. It has one now.

**Excluded from the run, and why.** `sx` objects, `size` props and the block that computes React keys.
A key never reaches the DOM and a style prop can only be pinned by asserting how a component looks, so
those survivors can only be killed by tests that pin the styling rather than the behaviour. Scoping the
run to the decisions is what makes the number mean something; the words themselves are mutated whole.

Re-run after the UX rework (the count and its dialog became a named list on the row): **87.88 %**,
all three files at or above the gate — `WarningsIndicator.tsx` 100 %, `columns.tsx` 80 %,
`dependencySentences.ts` 84 %. Two more real findings came out of it and are fixed: `withheldTitle` had
become dead code when the dialog went, and the "no link" test asserted the absence of a link *role*,
which an anchor without an href does not have anyway - so the branch survived being switched off. It
now asserts there is no anchor at all.

### Backend (Stryker.NET) — not run for slice 02

**Stryker.NET 4.16 ignores the `mutate` filter in this repository, in the config file and on the command
line alike: both attempts produced 16 108 mutants over the whole project rather than the eight files
named.** Slice 01 hit the same wall from the other side and recorded it as line-spans widening to whole
files; this is the same defect one level up. A full-project run is hours of wall clock and its score
would describe the whole backend rather than this slice, so it was stopped rather than left running.

What that leaves uncovered is smaller than it sounds: the decision itself (`DependencyHonourPolicy`,
`DependencyCycleDetector`, `DependencyFacts`) is unchanged since slice 01's run and is covered by
unit tests plus three standing architecture rules, and every branch of the read path is asserted by the
acceptance scenarios over the real route. The gap is worth closing deliberately - either by pinning a
Stryker.NET version whose filter works, or by moving the dependency types into their own project so the
filter is not needed.

---

# Mutation testing — Epic 4365 slice 01 (Show Feature Dependencies)

Run 2026-08-18 against `main` @ `f9b0cc19b`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **81.46 %** | 167 | 143 | 22 | 2 | 20 m 49 s |
| Frontend (StrykerJS 9.6.1) | **100.00 %** | 14 | 14 | 0 | 0 | 17 s |

Configs: `stryker.4365.backend.json`, `stryker.4365.frontend.json`, `vitest.stryker.mutation.ts`.

## The number that matters is not 81.46 %

**Every one of the 24 backend survivors is in pre-existing code that merely shares a file with this
slice's changes. Not one is in a line this slice wrote.** Checked by mapping each survivor's line
against `git diff -U0 3041e0b13..HEAD` per file.

On this slice's own lines the kill rate is **100 %**:

| file | tested | survived | this slice's lines |
| --- | --- | --- | --- |
| `DependencyReconciler.cs` | 2 | 0 | whole file is new |
| `FeatureDependencyReference.cs` | 1 | 0 | whole file is new |
| `FeatureRepository.cs` | 14 | 0 | the new projection read |
| `WorkItemExtensions.cs` — `ExtractDependencyReferences` (44-79) | 6 killed, 4 ignored | 0 | new |
| `FeatureDto.cs` / `FeaturesController.cs` / `Feature.cs` | — | 0 in changed lines | additive count path |

The design named one component where a surviving mutant would be unacceptable — the reconciler, because
a wrong verdict there is a wrong warning today and a wrong forecast date once the premium epic reads the
same data. It has **no survivors**.

`DependencySource.cs` produced no mutants: an enum with two members has nothing to mutate.

## Backend — accepted survivors

All 24 are pre-existing behaviour, untouched by this slice, and killing them means writing tests for
other features. Listed rather than absorbed into a percentage, so a future pass can attack them
deliberately.

**`FeatureDto.cs` (7)** — `namedCycleTimes ?? []` null-coalescing (both directions), the
`TeamsWithoutForecast` ordering and its statement, `Forecast?.CreateForecastDtos` null-coalescing, and
the two `RemainingWork[…] +=` / `TotalWork[…] +=` accumulations flipping to `-=`. The last pair is the
most interesting of the set: nothing asserts the *accumulation* across multiple work rows for one team.

**`FeaturesController.cs` (10)** — the parent-reference `AsEnumerable()`, the `w.Team != null &&` blocked
guard (both the logical and equality mutants), the `Ok()`/`NotFound()` conditional at `:154`, two boolean
flips, `feature.Portfolios.Any` → `All` at `:233`, a statement removal at `:244`, and the readable-portfolio
conditional and null-coalescing at `:269`/`:272`.

**`WorkItemExtensions.cs` (5)** — the parent-relation walk's block removal and its
`Attributes.TryGetValue("name", …)` logical mutant, a stack-rank string, and two timeouts in the
backlog-priority and created-date extractors. All belong to the parent path this slice deliberately left
alone; routing it through the new URL guard would have changed behaviour rather than refactored it.

**`Feature.cs` (2)** — a string mutant at `:84` and `FeatureWork.FirstOrDefault()` → `First()` at `:187`.

## Not mutated, and why

Three changed files were excluded because this slice's change is a rounding error inside them, and
Stryker.NET **ignores line-span `mutate` patterns** — a span silently widens to the whole file, so
including them would bury this slice's score under a thousand untouched lines:

| file | changed lines / total |
| --- | --- |
| `AzureDevOpsWorkTrackingConnector.cs` | 88 / 1327 |
| `WorkItemService.cs` | 23 / 1161 |
| `LighthouseAppContext.cs` | 28 / 661 |

Their behaviour is covered instead by the acceptance harness under
`Lighthouse.Backend.Tests/API/Integration/Dependencies/`, which drives a real refresh through the real
ASP.NET host and real EF with only the connector faked — the relation read, the parent-override fork and
the reconcile-on-both-branches wiring each have a scenario that fails when perturbed.

`JiraWorkTrackingConnector.cs` is also in the diff but belongs to an unrelated concurrency fix
(`930cc4142`), not this slice.

## Frontend

`columns.tsx:136-153` — the `renderDependsOnCount` helper and `createDependsOnColumn`, which is the whole
of this slice's frontend surface. Line ranges **do** work on the JS side, so this score describes only
the new code. 14 mutants, all killed, including both branches of the blank-versus-count decision and
every field of the column definition.

## Two traps this run hit

- **The vitest config must sit in `Lighthouse.Frontend/` at run time.** Its own
  `setupFiles: ["./setupTests.ts"]` resolves relative to itself, so pointing Stryker at the copy under
  `docs/` fails with `[UNRESOLVED_ENTRY] Cannot resolve entry module`. Committed here for the record;
  copy it into the frontend root to run, and delete the copy afterwards.
- **`inPlace: true` leaves residue on a failed exit.** The first attempt stranded
  `stryker-setup-0.js` and `stryker-setup-1.js` in the frontend root. `git status` after every frontend
  run, as the skill says.
