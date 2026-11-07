# Bug 6575 — Team Admins and Portfolio Admins can delete their own unit through the API

## Wave: DELIVER / [REF] Maintainer decisions

- Root cause in `rca.md` confirmed (2026-11-07).
- Fix: a dedicated `CanDeleteUnit` requirement, not the existing `SystemAdmin` one, which skips the enforcement gate.
- Fixed on nWave 3.22.2, before the v4 upgrade.
- Rigor profile `thorough`: crafter Opus, reviewer Sonnet, double review.
- It goes ahead of Epic 6577, whose Admin on all would otherwise reach every unit.
