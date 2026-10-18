# ADR-254: A published Release forecast says "more than 5 years" instead of a date

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; NMD-4, option a).
**Feature**: `epic-6311-responsive-during-updates` (Story #6309, slice 03)
**Extends**: [ADR-178](./adr-178-version-publishing-is-a-connector-capability-not-a-write-back-field-update.md).

## Context

A Delivery that broadcasts its forecast writes a block into its Jira Release description. The block has an opening
line, one "70%: yyyy-MM-dd" line per percentile shown, and a "Target …" line (`DeliveryForecastBlockRenderer.Render`).
This is a second place where Lighthouse writes forecast dates into a tracker. Forecast write-back writes nothing for a
beyond percentile (NMD-2), but the block is prose, not a typed field. Finding the previous block anchors on the opening
line and the "Target" line, not on the percentile lines.

## Decision

A percentile beyond the horizon is written as "85%: more than 5 years", with the years from the same constant
(ADR-251). Percentiles inside the horizon keep their date. The block is still published when every percentile is beyond.

## Alternatives considered

- **Write the stand-in date** (today + horizon): a real-looking date nobody forecast, in a place stakeholders read.
- **Drop the line**: a Delivery with every percentile beyond would publish nothing (the block must carry at least one
  percentile), leaving the last block standing as if current.
- **Write nothing for the whole block** (as for "no forecast"): hides a true answer.

## Consequences

**Positive**
- The Release says what the Lighthouse screen says.

**Negative / accepted**
- A board that parses the block's lines for dates meets a non-date line.

## Enforcement

- Renderer tests: a beyond percentile renders the wording; a block written with one merges over an older block without
  leaving text outside the markers.
