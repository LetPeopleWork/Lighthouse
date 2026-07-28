# Slice 04 — Release notes and concept docs explain why the number moved

**Story**: US-04 · **ADO**: #5587 · **Job**: `job-delivery-likelihood-covers-every-feature` · **Effort**: ≤ 1 day
**Blocked by**: slices 01–03 (the notes must describe what actually shipped, including slice 02's
visible delta). **Release-bound to**: slices 01, 02, 03 (D9).

## Goal

Make the upgrade legible. A forecaster whose delivery badge drops, whose percentile dates move outward
and whose recorded trend steps on the same day must be able to attribute all three to the method
change — from the release notes and the concept page alone (D3: **no** in-app messaging).

## Why docs-only (D3)

Matches how ADR-110 D5 handled Epic 5459's own one-time step. No dismissible-notice mechanism exists
today and building one for a single-release message is disproportionate. No in-app banner, no
dismissible notice, no trend-chart annotation.

## IN scope — release notes

A **lead item written as positioning** (customer pain → outcome win, house style), covering all three
visible consequences:

1. **The number drops.** A delivery's likelihood previously reflected only its governing feature; it
   now reflects all of them.
2. **The percentile dates move outward too.** `delivery.completionDates` came from the governing
   feature's histogram and now comes from the joint histogram. This is the most under-communicated
   consequence — a reader watching only the badge will be surprised by the dates.
3. **The recorded trend shows a one-time step that cannot be backfilled.**
   `DeliveryMetricSnapshot` is forward-only (ADR-048/049) and stores percentile *dates*, not per-team
   histograms; recomputation would need per-snapshot historical throughput that is not retained. Same
   situation as ADR-110 D5, one level up.

Plus a **separate bullet** for slice 02's sufficiency change: the "not enough data" warning now covers
every contributing feature rather than the least likely one, so some deliveries gain an indicator they
never had.

## IN scope — `docs/concepts/howlighthouseforecasts.md`

Extend one level up, to deliveries. The page already teaches the multi-team coin analogy post-5459 and
has the shape to hang this on: "Example Scenarios → 2 Teams - 1 Feature → Doing it by hand → When a
Team Cannot Be Forecast".

- A delivery-level worked example in the same coin framing, reproducible in a spreadsheet from a
  reader's own per-team rows.
- **Teach the grain (D5)**: the decomposition is per team per feature; a feature worked by two teams
  contributes one row to each team's bucket, which is why a shared feature is not penalised twice.
- **Show the equality case (D1 Constraint B)**: use the three-way fixture — `A/F1 = 0.90`,
  `B/F1 = 0.80`, `B/F2 = 0.95` ⇒ delivery `0.90 × min(0.80, 0.95) = 0.720`, rows F1 = 0.72 and
  F2 = 0.95. The delivery number **equals** F1's row. The prose must not claim the delivery is always
  lower than every feature.
- **Restate independence at delivery grain (D4)**: teams are assumed independent; shared people or a
  hand-off make reality **worse** than the maths suggests. The page already says this at feature grain
  in exactly these terms — extend, do not re-argue.

## OUT of scope

- Any in-app messaging (D3).
- An in-product independence caveat or a shared-people detector (D4). Grounding, recorded so it is not
  re-litigated: `ctx_search "Assignee" --include=*.cs` over the backend returns **zero hits**.
  Lighthouse stores no person or assignee data at all, so shared-people correlation is not merely
  undetected — it is **underivable from what is persisted**.
- Backfilling recorded history — impossible from stored data.
- The blog post in the letpeople.work Monte Carlo style. Epic 5459 already deferred one; if this
  material makes that post better, note it and keep deferring — it is not a release gate.

## Website surface

The marketing pages are **N/A** — no new capability, screen or headline claim. But `docs/` is hot-linked
from `Lighthouse@main/docs/` via jsDelivr, so the concept-page edit is **live on letpeople.work the
moment it merges**. It must be complete and self-consistent at merge time, not finished later.

## Learning hypothesis

**Confirms** that the upgrade shock is a communication problem, closed by notes plus one docs section.

**Disproves** it if the worked example cannot be made reproducible by hand — if a reader following the
page arrives at a different number from the one on screen, then the displayed value depends on
something the docs cannot expose (rounding, the largest-remainder residue, the day→date blackout
translation), and the honest response is to say so on the page rather than to publish a walkthrough
that does not reproduce.

## Acceptance criteria

Full text in `feature-delta.md` US-04. Summary: AC-04.1 lead item covering number + dates + trend step ·
04.2 separate sufficiency bullet · 04.3 delivery-level worked example on the concept page · 04.4 the
per-team-per-feature grain taught explicitly · 04.5 independence restated at delivery grain, docs-only ·
04.6 the equality case shown, no "always lower" claim · 04.7 no in-app messaging added.

## Gates before commit

1. The maintainer walks the worked example end to end against the running demo instance and reaches
   the displayed rounded percentage. A mismatch blocks the release (this is also an outcome KPI).
2. Docs prose reviewed against DIVIO/Diataxis — this is *explanation*, not how-to; it must not drift
   into a tutorial.
3. Per-feature discipline: docs land at feature finalization, not deferred to `/release`.
