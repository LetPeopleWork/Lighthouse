# ADR-252: A beyond-the-horizon percentile travels as an additive flag beside a kept date

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; NMD-3, option a).
**Feature**: `epic-6311-responsive-during-updates` (Story #6309, slice 03; Lighthouse-Clients minor at finalize)
**Refines**: the feature's NMD-1 c (additive flag plus a clients release). "Gated on server version" becomes "keyed on
whether the field is present". That is a refinement, not a reversal.

## Context

Every completion and start percentile goes out as `WhenForecastDto` with a non-nullable `ExpectedDate`. It reaches:
- Feature `forecasts[]`;
- `startForecast.percentiles[]`;
- the per-Team start and completion percentiles;
- Delivery and Feature-likelihood `completionDates[]`;
- manual `whenForecasts[]`.

Delivery snapshots and pinned closure records store `WhenDistributionPointDto(Probability, ExpectedDate)` as JSON text.
Released Lighthouse-Clients read `expectedDate` as a required day, and the web app coerces it with `z.coerce.date()`. A
null date would break both.

## Decision

1. `WhenForecastDto` gains `beyondHorizon: boolean` and `horizonYears: number`. `WhenDistributionPointDto` gains
   `beyondHorizon: boolean`. The same flag is carried internally by `DeliveryWhenPercentile` and
   `DeliveryForecastBlockPercentile`.
2. For a beyond percentile, `expectedDate` = **today + the horizon's simulated days, in calendar arithmetic**. It is
   O(1), never a working-day projection, so a 100,000-day horizon can never cost 100,000 steps per percentile. Released
   clients therefore show a date about 5 years out.
3. Snapshot and closure-record JSON gain the property. No migration; JSON written earlier parses as `false`.
4. The web app's `WhenForecast` model carries the flag and the years, and one shared formatter renders
   "more than {horizonYears} years" on every surface. The two places that rebuild a `WhenForecast` from probability and
   date carry the flag through.
5. Lighthouse-Clients render the flag when it is present and `true`. Its absence (an older server) keeps today's
   rendering, so no version comparison is needed.
6. Blackout-window sizing that reads the 95th percentile (`ForecastController.EstimateForecastWindowEnd`,
   `FeatureForecastWindow.EndFor`) uses the horizon when that percentile is beyond. Otherwise the window would shrink
   and the earlier percentiles would be projected over a calendar with blackouts missing.

## Alternatives considered

- **`ExpectedDate` null**: breaking for released clients and for the web's date coercion.
- **`beyondHorizon` only, `horizonYears` once on an instance endpoint**: every client needs a second call and a cache.
- **`beyondHorizonYears: number | null`**: one field, but a less obvious contract than a boolean.
- **A server-version gate in the clients**: the field's presence already says what the version would.

## Consequences

**Positive**
- Additive and expand-only. The web app and the clients render the same wording from one constant.

**Negative / accepted**
- Until they upgrade, released clients show a real-looking date about 5 years out. That gap already exists today for a
  forecast that hits the cap.
- `horizonYears` repeats on every percentile.

## Enforcement

- Backend DTO tests: the flag is set exactly when the percentile read says beyond; `expectedDate` is today + horizon days.
- Snapshot and closure-record JSON round-trips with and without the property.
- Clients: a schema test against a recorded server payload, with and without the field.
