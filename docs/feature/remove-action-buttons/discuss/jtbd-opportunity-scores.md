# Opportunity Scores — remove-action-buttons (ADO #5077)

Scoring is deliberately honest: these flows already work. This is polish / friction
reduction, not a capability gap. Importance is moderate, not high.

Scale: importance 1-5, current_satisfaction 1-5, gap = importance − satisfaction.

| Job | Importance | Current satisfaction | Gap | Rationale |
|---|---|---|---|---|
| A — Commit intent without a ceremonial button | 3 | 2 | 1 | The save ceremony is a recurring papercut on flows used by every team/portfolio admin, made sharper by the two-step "save then refresh" on state-mappings and the forecast filter (the stopgap Alert in commit 53e6287e is explicit debt). But the flows do work today (satisfaction 2, not 1) — the user CAN save, just with friction. Gap is real but small; this is polish, and the convergence value (retiring the stopgap hints, one uniform behavior) is the multiplier, not raw importance. |
| B — See my forecast update as I shape its inputs | 3 | 2 | 1 | The tweak-click-read loop on new-item and backtest is friction, and the inconsistency with the auto-running How Many / When forecast ON THE SAME PAGE is a visible polish defect. Satisfaction is 2 (the forecasts work, and the proof-pattern already exists for the user to compare against), so the gap is small. The leverage is consistency: half the page already feels right; this makes the rest match. |

## Honest framing

- **This is not a high-opportunity feature by the ODI math (gaps of 1).** It is a
  coherence / craftsmanship investment: low individual gaps, but it (a) pays down
  explicit debt — the 53e6287e stopgap Alert and the StateMappings "must refresh"
  Alert both exist only because this story hasn't landed — and (b) makes the whole
  app obey one interaction law ("valid input is acted on; no ceremonial button").
- **Implication for prioritization (feeds Phase 4 / story-map):** because each job's
  gap is small, the slices should be sequenced by *learning + debt-retirement leverage*
  and *dogfood cadence*, not by raw opportunity score. The convergence slices (forecast
  filter, state mappings) that delete a visible stopgap hint deliver the most felt value
  per unit effort, even though their ODI gap equals the others.

## North-star outcome signal (preview for Phase 4 outcome-kpis.md)

Because Lighthouse customer instances do not phone home (self-hosted telemetry gap,
Epic 5015 — see project memory), classic adoption KPIs are not measurable cross-instance.
Honest, measurable proxies for this feature:

- **Output-debt retired (binary, measurable in-repo):** the `forecast-filter-takeeffect-hint`
  Alert and the StateMappings "After saving, a data reload is needed" Alert are removed;
  no settings/forecast surface retains a ceremonial Save/Run button. Verifiable by test +
  grep, not telemetry.
- **Interaction-consistency (binary):** all four surface groups exhibit the same
  valid-input-acts-immediately behavior as the already-shipped How Many / When forecast.
- A behavior-change KPI ("admins lose fewer unsaved edits", "forecasters explore more
  scenarios per session") is the true outcome but is NOT instrumentable today — record as
  aspirational, gated on Epic 5015, in Phase 4.
