# Slice 04 — Publish the forecast to the Release

**Goal**: the Lighthouse forecast for a bound Delivery appears on the Jira Release itself, so people who
never open Lighthouse can see it.

**Story**: US-05. **ADO Story: #4463** (absorbed per D0; carries the `Release Notes` tag).

## IN scope

- A **per-Delivery** switch, **off** by default (D8, granularity revised 2026-08-25 — see D8a). Opt-in is
  the first requirement, not a refinement.
- Write a delimited block into the Version's **description**. **Never `releaseDate`** (D8).
- **Block content (user, 2026-08-22)** — four things, all four required:
  1. unmistakable attribution that Lighthouse wrote it,
  2. the date it was written,
  3. the Delivery's three forecasts — **70%, 85%, 95%** — the same three the product itself renders
     (`DeliveryWithLikelihoodDto.cs:62`, `CalculateMetrics(today, blackoutPeriods, 70, 85, 95)`), so the
     Release and the Lighthouse screen can never disagree about which percentiles are on show,
  4. the likelihood of hitting the target date, with the target named.
- **A stable machine-detectable marker** bounding the block. This is the mechanism that finds a previous
  Lighthouse write, and it is what makes coexistence with the team's own description text possible.
- Order the block so the attribution and headline come first: `description` is a truncating column in the
  Releases list, so the preview must read as something true and attributable rather than a fragment.
- Idempotent replace in place: never a second block, never any change to text outside the markers, and a
  hand-edited block is replaced wholesale rather than merged.
- Create the description when the Release has none — the observed default on every Release on the demo
  instance.
- Only Deliveries bound to a **live** Release publish. Manual and Rule-based ones have no Release;
  **archived** ones would push a frozen closure forecast into a live Jira Release forever; **broken-source**
  ones (D6) point at a Version id that no longer resolves. The archived exclusion is HELD until #5698
  archiving ships (S15) — there is no field to test yet.
- A write that 404s raises the broken-source state (D6), not a refusal. A missing target and a denied one
  send the admin to fix different things.
- Runs on the same refresh cycle that produced the forecast, alongside the existing write-back staging in
  `PortfolioUpdater`.
- `bool SupportsDeliveryForecastPublishing(connection)` — a **separate** capability from the inbound one.

## OUT of scope

- Writing to the member issues. Explicitly rejected by D7 — the per-issue path already carries per-Feature
  numbers, and repeating one Delivery-level number across N issues is the noise `quiet-jira-writeback`
  spent an Epic removing.
- Writing `releaseDate` (D8) — Lighthouse would be overwriting the field it declares Jira owns, turning
  its own forecast into the target it is measured against.
- The refusal report — slice 05.
- Jira Data Center verification.

## Learning hypothesis

**Disproves D7/D8's surface choice** if the block does not survive contact with a real Version
description — markup mangled, length capped, or the Releases-list column truncating it into something
that reads as noise rather than as a Lighthouse statement. The fallback is a comment on the Release's
issues, designed then, not now.

**Disproves the marker approach** if a round-trip through the Jira UI rewrites or strips the delimiters,
because then a later write cannot find its own previous block and starts appending. That is the failure
that turns a helpful line into description spam, and it is worth an explicit test rather than an
assumption.

**Confirms** the Epic's outbound premise: that a Jira-native reader will act on a forecast they did not
have to go looking for.

## Acceptance criteria

AC-05.1 through AC-05.7 in `feature-delta.md` (twelve criteria — 05.3 gained b and c, 05.4 gained b and c
after the 2026-08-22 content decision; 05.5 and 05.6 after review). The three that carry the slice:

- The block carries all four required things: Lighthouse attribution, the write date, the 70/85/95
  forecasts, and the target likelihood. Dropping any one of them is a failed slice, not a trim.
- Re-writing replaces rather than appends, and leaves surrounding text untouched. A description that
  accumulates Lighthouse blocks is worse than no feature.
- The write reuses the existing suppression posture so publishing does not reintroduce watcher noise.

## Dependencies

Slice 00 Q3 and Q4. Slice 02 (there must be something worth publishing, kept current). Not blocked on
slice 03.

## Effort

~6 hours. The idempotent block replace is the fiddly part, not the HTTP call.

## Watch

This is a **new write class**, not another `WriteBackFieldUpdate` — that type names an issue and a field.
DESIGN decides whether the Version write joins the existing collector as a second staged type or sits
beside it. Do not force it into the existing shape just because the shape is there.
