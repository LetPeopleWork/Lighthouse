# Mutation testing — Epic 5733 slice 03 (ADO #5836), the administrator's veto

Run 2026-09-13. Gate is 80 % per stack. **Both stacks pass.** Backend and frontend were run
sequentially, never together: overlapping them at concurrency 6 once produced a run whose mutants
were 100 % `Timeout`, which is not a result.

| Stack | Score | Tested | Killed | Survived | Config |
|---|---|---|---|---|---|
| Backend | **91.67 %** | 156 | 143 | 13 | `stryker.5836.backend.json` |
| Frontend | **93.75 %** | 32 | 30 | 2 | `stryker.5836.frontend.json` + `vitest.stryker.5836.ts` |

Every surviving mutant on both stacks is accounted for below. None is a coverage gap that was left
open.

## What the run found, and what it changed

Mutation testing earned its place twice here rather than confirming a number.

**AC-06.6 was never asserted.** The criterion asks the settings copy to state, in plain words, that
stopping usage data suspends what people already answered and that turning it back off resumes
without asking them again. The DISTILL delta said it would be "checked by eye at DELIVER" — the
silent skip the standing rule forbids. Emptying the whole description to `""` killed nothing. Five
survivors on the seeded Name and Description are now five assertions, and the criterion is
executable.

**The provider's seam was untested.** `useUsageDataConsent.tsx:120`, `if
(state.administratorDisabled)` → `if (false)`, **survived**, and its body reported as never
covered. Both ends of that wire were tested — the endpoint returns the field, the dialog takes it
as a prop — and nothing exercised the piece between them. With the branch dead the footer falls
back to plain not-sending and quietly stops naming the administrator, which is the whole of
AC-06.7. Four scenarios were added to `useUsageDataConsent.test.tsx`. This one is worth remembering
as a shape: a seam with a tested component on each side is the easiest place in a codebase to have
no coverage at all.

## Backend survivors (13) — all accounted for

Scope was verified from `mutation-report.json` before the score was believed, per the standing rule.
Tested mutants per file: `UsageDataGate.cs` 68, `OptionalFeatureSeeder.cs` 38,
`UsageDataConsentService.cs` 35, `OptionalFeaturesController.cs` 12, `UsageDataMasterSwitch.cs` 3.

| Where | Mutant | Verdict |
|---|---|---|
| `OptionalFeatureSeeder.cs` 15, 25, 45, 113, 123 | `Statement → ;` and `String → ""` on `logger.Log*` calls | **Equivalent for purpose.** What the tests pin about seeding is the rows it produces, not the sentences it narrates doing it. Killing these means asserting log text, which pins wording nobody reads. |
| `OptionalFeatureSeeder.cs` 42 | `toRemove.Count > 0` → `>= 0` | **Equivalent.** The guard only decides whether a log line is written; `RemoveRange` on an empty list is a no-op either way. |
| `OptionalFeatureSeeder.cs` 68 | `Enabled = false` → `true` on the `FeatureOrdering` row | **Dead literal.** That key takes the carry-across branch, which overwrites `Enabled` from `ThisInstanceAlreadyOwnedTheFeatureOrder()` before the row is added. The literal cannot reach the store. Story #5876's row, not this slice's. |
| `UsageDataGate.cs` 152 | `TurnTheDayOverIfItHas();` → `;` in `GiveBackWhatCouldNotBeSent` | **Pre-existing, out of scope.** Slice 01c code. Killing it needs control of the host's clock, which this harness does not have; the consequence is a returned allowance attributed to the wrong day's tally. Recorded rather than chased. |

The mutant that mattered most — `Enabled = false` → `true` on the **UsageData** row, which would
ship the veto engaged and stop every instance on upgrade — was **killed** on the first run.

### One survivor was a weak assertion of this slice's own making

The first attempt at the AC-06.6 assertion read `Does.Contain("nobody is asked")`. The description
also ends with a promise that nobody is asked *again*, so the phrase matched that instead, and the
mutant emptying the clause it was written for survived. Tightened to `"nobody is asked about it"`,
which appears once.

That kill is verified by construction rather than by a fourth ten-minute run: the phrase occurs
exactly once in the seeded description, so emptying that string literal necessarily fails the
assertion. Recorded this way rather than claimed as measured.

It is the same defect class this slice spent the day removing from other people's tests, written
fresh into its own an hour earlier. A substring assertion is satisfied by any sentence that happens
to contain it, and long copy usually contains it twice.

## Frontend survivors (2) — both equivalent

`UsageDataDialog.tsx` 119 and 130: `ObjectLiteral → {}` on `sx={{ mx: 3, mb: 1 }}` for the two
alerts. Margin values. Every behavioural mutant in the slice's changed lines was killed.

## Two config traps hit on the way, both silent

**The frontend must be scoped to the lines the slice changed.** A whole-file run over
`UsageDataDialog.tsx` scored 50 %, and all nine survivors were `sx` style objects belonging to
slice 01's copy. Whole-file scores the pre-existing suite, not this slice; it reported 72.92 %
overall and described almost nothing this slice wrote.

**StrykerJS does not accept comma-separated line spans.** `UsageDataDialog.tsx:21-30,118-152`
matched **nothing** — no error, no warning, the file simply vanished from the report and the run
printed a clean 100 %. The same family as Stryker.NET's ignored line spans: the config excludes
quietly and the number still looks good. One span per entry, and always check the per-file table
for a file you expected to see.

## Reproducing

Backend, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```
cp ../../docs/feature/epic-5733-opt-in-usage-data/mutation/stryker.5836.backend.json stryker-config-5836.json
dotnet stryker -f stryker-config-5836.json
```

Frontend, from `Lighthouse.Frontend/` (commit first — `inPlace` mutates the real working tree):

```
cp ../docs/feature/epic-5733-opt-in-usage-data/mutation/vitest.stryker.5836.ts vitest.stryker.mutation.ts
cp ../docs/feature/epic-5733-opt-in-usage-data/mutation/stryker.5836.frontend.json stryker-config-5836.json
pnpm exec stryker run stryker-config-5836.json
```

Triage with `triage_5836.py <report>` (backend) and `triage_5836_frontend.py <report>` (frontend);
both group by file so scope can be checked before the score is read.
