# Mutation testing — Epic 5733 slice 02 (#5835), the unprompted ask

Run 2026-09-13. Both stacks clear the 80% bar.

| Stack | Score | Killed | Survived | Uncovered |
|---|---|---|---|---|
| Backend (Stryker.NET 4.16) | **96.34%** | 79 | 2 | 0 |
| Frontend (StrykerJS 9.6.1) | **86.60%** | 168 | 20 | 6 |

Configs: `stryker.5835.backend.json`, `stryker.5835.frontend.json`, `vitest.stryker.5835.ts`.
Backend runs from `Lighthouse.Backend.Tests/`; running it from the project directory fails with
"No .csproj or .fsproj file found" because `test-projects` resolves against the working directory.

## Frontend, per file — the headline number is not the slice's own code

| File | Score | Survived |
|---|---|---|
| `services/UsageData/promptSession.ts` | 100% | 0 |
| `services/UsageData/usageDataAskMarker.ts` | 100% | 0 |
| `hooks/useUsageDataConsent.tsx` | 98.28% | 1 |
| `services/UsageData/usageDataAskEligibility.ts` | 97.06% | 1 |
| `components/UsageData/UsageDataAsk.tsx` | 94.12% | 1 |
| `components/SurveyNudge/SurveyNudge.tsx` | **61.67%** | 17 |

`SurveyNudge.tsx` is in the mutate list because this slice does modify it — it now claims and
yields the session's one prompt slot. **Those mutants were killed.** Its 17 survivors are the
component's pre-existing surface from an earlier slice: MUI `sx` object literals, layout strings,
copy, and optional chaining in its loader. Killing them means tests that pin styling and wording,
which is brittle and buys nothing, so they are left alone deliberately.

Narrowing the mutate list to the files this slice owns would report ~98% and would be flattering by
construction. The split is recorded instead.

## What the runs actually found

Three defects, none of which the score would have told you about on its own.

**A whole file had no coverage.** `PruneNowAsync` was written pull-able so a test could run one pass
at a chosen moment, and no test ever did. Nothing said the service counts backwards from now rather
than forwards — and counting forwards would make every row look newer than both thresholds and
quietly forget nothing, for ever, which from outside is indistinguishable from a pass that works.
Nothing said it consults the licence either.

**Three tests were vacuous — they could not fail against the defect they named.**

1. `PostAsked_WithTheBrowsersOwnToken_StopsItBeingDueStraightAway` recorded a refusal, called the
   endpoint, and asserted the browser was not due. A browser that refused a moment ago is not due
   either way, so it passed whether or not the endpoint wrote anything; two mutants deleting the
   write outright survived it. It now backdates the row so the browser is genuinely due, asserts
   that it is, and only then asks what the call changed.
2. The eligibility guards for an unreadable and a future-dated marker were **dead code**. `NaN` and
   a negative elapsed time are each never greater than the window, so both guards returned `false`
   exactly where the fallthrough already did. Stryker deleted them and no test noticed. Worse, the
   behaviour was wrong: an unusable marker silenced that browser permanently, because the only
   thing that rewrites the marker is an ask that can no longer happen. Both now ask once, which
   re-stamps the marker from a working clock.
3. The test written to cover the `alreadyAsked` ref never reached it. Changing a mock does not
   re-run an effect whose provider only re-fetches hourly, so the state never changed. It now
   declines through the real button — which refreshes state and genuinely re-runs the decision while
   the server still says the browser is due — and was verified by removing the guard and watching it
   fail.

**The pruning loop had nothing holding it to waiting a day.** Delete the `Task.Delay` and it runs
the prune query as fast as the database answers. Every other test still passed, because they all
advance the clock before looking for the next pass.

## Surviving mutants, and why

Backend, both equivalent: early returns from cancellation that the loop condition reaches on its own
turn. Logging mutants are suppressed with justifications in the form this codebase already uses —
what a line says to an operator is not behaviour a test should pin.

Frontend: one per slice-owned file, all string or object literals whose value no assertion depends
on, plus `SurveyNudge.tsx`'s pre-existing surface as above.

## Two process notes worth keeping

**The anonymous rate limiter is process-wide and `[SetUp]` does not reset it.** Adding tests pushed
`UsageDataAskEndpointsTests` past 20 requests a minute; the failures arrived as a JSON parse error
on an empty body and named nothing. That class now raises the limit for itself — the limiter's own
behaviour has a dedicated fixture and nothing else needs to compete with it.

**`FakeTimeProvider.Advance` races a service that has not reached its delay yet.** A timer set
against a clock that has already moved never fires, so the test hangs rather than fails. The wait
helper nudges the clock while polling instead.
