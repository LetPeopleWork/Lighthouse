# Slice 02 — SLE Risk is one number over the team's configured history

**ADO**: User Story #6037 · **Story**: US-R2-02 · **Job**: `job-flow-coach-act-before-sle-breach`
**Estimate**: ≤1 day · **Reference class**: round 1's slice 01b (the guard) plus 01 (the read), both one day each; this is their reversal in one file.

## Goal

One risk per item, always computed over the team's configured history and target, always a number — and the same number in Lighthouse and on the board.

## Learning hypothesis

**Disproved if** a real team's dialog and its written-back field still disagree after the change. That would mean the window was never the whole cause, and the disagreement is somewhere neither this story nor DISCUSS has looked.

**Confirmed if** the observed pair — 27% in the dialog, 18 on the board — becomes one integer, and moving the date picker leaves it where it is.

## IN scope

Four changes in `SleRiskCalculator` and its callers, travelling together because each alone leaves the others incoherent:

1. **One window.** Both the display path and `WriteBackTriggerService` read `team.GetThroughputSettings(clock.Today)` and age items to today. Re-key the metrics cache off history and target rather than the browser range. The column description gains *"across the team's configured history"*. `WriteBackTriggerService.cs:107`'s doc comment currently promises the opposite and is corrected.
2. **Past the target is certainty.** The certainty check moves ahead of the sample check: `ageInDays > targetRangeInDays` returns 100. An age exactly on the target stays computed — an item at 4 days against a 4-day target can still close today and meet "4 days or less".
3. **`MinimumComparableItems` is deleted.**
4. **A 0/0 inside the target reads 0.**

**What falls out** — `null` survives only for a closed item and for a team with no published target, both honest "not applicable". So `SLE_RISK_BEYOND_HISTORY_LABEL` and `SLE_RISK_NOT_ENOUGH_HISTORY_LABEL` go, `labelFor` always returns a percentage, and `sleRiskSortValue` loses its sentinel special-casing. **Check whether `SleRiskDto.comparableItems` still has a consumer** once the two silences are gone — it exists only to tell them apart.

**Docs** — `docs/metrics/flow-metrics.md` L148-149 (the two sentinel bullets) and `docs/settings/worktrackingsystems.md` L90-93 (the "fewer than ten" caveat, and whether an item still gets no write at all). Carry the cliff consequence into the docs: a thin history reads 0% up to the target and 100% the day after, because there is nothing to build a gradient from.

**ADR-192** — superseded on both the window and the certainty rule. The *Architectural Enforcement* row *"Beyond history is `null`, never `0`, `100` or an omitted entry"* is reversed and must be rewritten, not annotated.

## Acceptance criteria

*Added at the final review gate. The delta pointed here for "full ACs" and this file carried only a scope list — a promise the file did not keep, which is exactly the shape of gap that reaches DELIVER as an improvised decision.*

- **AC-02.1** Given a team with a configured history window and a published target, when the coach reads an in-flight item's risk in the dialog and then reads the field written to that item on the board, then both are the same integer for the same item on the same day. *(The observed failure is 27% in the dialog against 18 on the board.)*
- **AC-02.2** Given the same item, when the coach changes the metrics date range and re-reads the dialog, then the risk is unchanged — it is a claim about now, not about the window on screen.
- **AC-02.3** Given an item whose age is strictly greater than the target range, then its risk is 100 regardless of how much history exists, including when no finished item ever ran that long.
- **AC-02.4** Given an item whose age is exactly equal to the target range, then its risk is computed from the history and is **not** forced to 100 — an item at 4 days against a 4-day target can still close today and meet "4 days or less".
- **AC-02.5** Given an item at or below the target for which no finished item is comparable (`comparableItems == 0`), then its risk is 0, not null and not an error.
- **AC-02.6** Given any in-flight item on a team with a published target, then the response carries a number — `SLE_RISK_BEYOND_HISTORY_LABEL` and `SLE_RISK_NOT_ENOUGH_HISTORY_LABEL` no longer appear in any surface, and `sleRiskSortValue` no longer special-cases a sentinel.
- **AC-02.7** Given a team whose SLE target or history window is changed in settings, when the metrics are next read, then the risk reflects the new setting — the cache key carries history and target, not the browser range. *(Round 1's cache key omitted the target and an independent reviewer, not the author, caught it. The same class of error is available here.)*
- **AC-02.8** Given a **closed** item, or a team with **no published target**, then the risk is null and no write-back occurs — the two remaining honest "not applicable" cases.
- **AC-02.9** `SleRiskDto.comparableItems` either still has a consumer or is removed. It exists only to tell the two retired silences apart; a field that survives its only purpose is the artefact this AC exists to catch.

## OUT of scope

- The at-risk threshold and the widget — slice 04, which this slice blocks.
- Dialog width and column plumbing — slice 03.
- Re-taking `sle_risk_column.png` — deferred to slice 03, after both text changes have landed, so there is one re-take rather than two.

## The reversal this slice owes an argument for

`OUT-4127-risk-stability` measured the volatility and concluded **"something must gate it"**. This slice deletes the gate. The argument is that changes 2 and 4 leave no division at the ages where the volatility was measured — past the target the answer is a flat 100 with no denominator, and below it `n(a)` is large. **DESIGN must write that argument into the ADR amendment rather than let the deletion stand on the story description alone.** If it does not hold, the guard should come back with a lower threshold, not disappear.

Round 1 chose `MinimumComparableItems = 10` as an explicit product judgement about how wrong a number is allowed to look. Reversing a judgement is fine; reversing it silently is not.

### ADR-192 amendment outline — a sketch, so DESIGN does not start blank

Not a draft to adopt as written; the shape of the argument DESIGN has to either complete or refute.

> **The guard was protecting a range the certainty rule now owns outright.** The measured volatility
> (`OUT-4127-risk-stability`: up to `100/n(a)` points overnight, 25 points observed on 602 real closed
> items) lives entirely where `n(a) = count(T >= a)` has thinned — the high ages. That is precisely the
> region where the certainty rule now answers a flat 100 with no denominator and therefore no
> volatility at all. Below the target, `n(a)` is at its largest, because `n(a)` is monotonically
> non-increasing in `a` and `n(1)` is the whole closed population; the number there is stable by
> construction. Between the two, the only remaining case is `comparableItems == 0` at or below the
> target, which resolves to 0 rather than to a share of nothing.
>
> **What this costs.** A thin history now reads as a cliff rather than a curve — 0% up to the target,
> 100% the day after — because there is nothing to build a gradient from. That is a worse-looking
> answer than a blank, and a more honest one: the old guard said "not enough history" at ages where
> the item had plainly already missed.
>
> **What would refute it.** A team whose target sits *above* the age where its own `n(a)` thins —
> a long target over a short-tailed distribution. There the certainty rule does not reach the volatile
> region and the guard was doing real work. DESIGN should either show that shape cannot occur, or
> bring the guard back with a threshold chosen for it.

The ADR-192 *Architectural Enforcement* row *"Beyond history is `null`, never `0`, `100` or an omitted entry"* is rewritten, not annotated — it is the row this slice reverses.

## Dependencies

**Upstream: slice 01 must be done.** `Zones()` breaks its age walk on `For()` returning null (`SleRiskCalculator.cs:101-119`). Changes 2, 3 and 4 make null unreachable, so every band's geometry would silently change — in code slice 01 deletes. `CertainRisk` also lives in the zones half; this slice reintroduces the concept where `For` can reach it.

**Downstream: slice 04 is blocked by this.** The widget's "at risk ≥ 70%" rule counts items that today return null and tomorrow return 100.

## Watch-outs

- **`Program.cs` and the write-back path.** If the change reaches `Program.cs`, the full backend Integration suite runs and the live-connector flake exposure goes up. Keep the edit out of it if at all possible.
- **The cache re-key is the kind of thing round 1 got caught on.** The independent reviewer, not the author, found that the old key omitted the target. A key that now carries history *and* target needs a test that a settings change invalidates it.
- **`clock.Today` and the UTC anchor.** The display path currently ages to the range end; moving it to today puts it on the same anchor the write-back uses. Check it against the backend UTC-anchor work rather than assuming.
