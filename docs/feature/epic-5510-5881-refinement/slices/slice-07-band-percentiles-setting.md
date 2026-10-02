# Slice 07 — Band percentiles as a Team setting

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-07 ·
**Estimate**: ~½d · **Tier**: Community

## Goal

A Team admin sets the band's low and high percentiles. The defaults are 50 and 85 (D28, DD-2).

## IN

- Two inputs in Settings → Refinement, with validation: 1–99 and low < high, and an error message that names both values.
- Expand-only storage.
- The verdict and the line use the stored values.

## OUT

Per-Refinement overrides.

## Learning hypothesis

**This disproves "50/85 suits most Teams"** if every dogfood Team changes the band in its first week. The defaults
would then be wrong and would need revisiting before the docs are written.

## Data and dogfood moment

- Demo: defaults.
- Dogfood: leave the dev Team on the defaults for two Refinements, then ask the coach whether the band matched their
  intuition.

## Acceptance criteria

AC-7.1 … AC-7.3 (US-07).

## Dependencies

Slice 05.
