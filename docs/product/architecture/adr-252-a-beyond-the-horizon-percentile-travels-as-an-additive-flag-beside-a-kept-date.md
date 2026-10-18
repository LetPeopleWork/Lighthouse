# ADR-252: A beyond-the-horizon percentile travels as an additive flag beside a kept date

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; NMD-3 option a, extended by NMD-12 at the review gate).
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

1. `WhenForecastDto` **and** `WhenDistributionPointDto` gain `beyondHorizon: boolean` and `horizonYears: number`
   (NMD-3, NMD-12). The same flag and years are carried internally by `DeliveryWhenPercentile` and
   `DeliveryForecastBlockPercentile`.
2. For a beyond percentile, `expectedDate` = **`today.AddYears(horizonYears)`**. It is a stand-in, computed in O(1) and
   never as a working-day projection. Released clients therefore show a date exactly 5 years out.
3. Snapshot and closure-record JSON gain both properties. No migration.
   - Points written before the change carry neither and read `beyondHorizon = false`: a pinned record keeps what it
     said when it was pinned.
   - A point written after the change keeps the horizon it was forecast with.
4. The web app's `WhenForecast` model and the history-point model carry the flag and the years.
   - One shared formatter renders "More than {horizonYears} years" on every text surface (NMD-9).
   - The timeline and the predictability chart follow NMD-10.
   - The two places that rebuild a `WhenForecast` from probability and date carry the flag through.
5. Lighthouse-Clients render the flag when it is present and `true`. Its absence (an older server) keeps today's
   rendering, so no version comparison is needed.
6. Blackout-window sizing that reads the 95th percentile (`ForecastController.EstimateForecastWindowEnd`,
   `FeatureForecastWindow.EndFor`) uses the horizon when that percentile is beyond. Otherwise the window would shrink
   and the earlier percentiles would be projected over a calendar with blackouts missing.

## Alternatives considered

- **`ExpectedDate` null**: breaking for released clients and for the web's date coercion.
- **`beyondHorizon` only, `horizonYears` once on an instance endpoint**: every client needs a second call and a cache.
- **`beyondHorizonYears: number | null`**: one field, but a less obvious contract than a boolean.
- **The stand-in as today + the horizon's simulated days**: within a day of the same date, but it ties the date to a day
  count readers never see.
- **A server-version gate in the clients**: the field's presence already says what the version would.

## Consequences

**Positive**
- Additive and expand-only. The web app and the clients render the same wording from one constant.

**Negative / accepted**
- Until they upgrade, released clients show a real-looking date 5 years out. That gap already exists today for a
  forecast that hits the cap.
- Old binaries read a beyond percentile as today's date until the next forecast run (NMD-16, ADR-250).
- `horizonYears` repeats on every percentile and point.

## Enforcement

- Backend DTO tests: the flag is set exactly when the percentile read says beyond, and `expectedDate` is
  `today.AddYears(horizonYears)`.
- Snapshot and closure-record JSON round-trips with and without the properties.
- Clients: a schema test against a recorded server payload, with and without the fields.
