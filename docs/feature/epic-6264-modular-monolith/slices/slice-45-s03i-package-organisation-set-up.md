# Slice 45 — S03i Package Organisation set-up

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M–L** (carpaccio at its DESIGN) · added 2026-11-01 from ADR-270

## Goal

Package the **Organisation set-up** context found by the S01 Event Storming: the units (today's Teams and
Portfolios as owners of work), their settings and way-of-working overrides, Terminology, and the mapping defaults
(today partly connection defaults). Behaviour-free: routes, DTOs and responses unchanged.

## IN

- Move Team and Portfolio (as units) with their settings into `Modules/Organisation…/` per the S02-settled spelling.
- Terminology, connection-default inheritance (`SettingSource`, `FollowsConnectionDefault`), blackouts, refresh
  cadence and Feature ordering policy (the org-wide settings).
- Ratchet candidates: `TeamDeleted` is published from a controller (`TeamController.cs:115`); Portfolio deletion
  has no event and cleans up inline (`PortfolioController.cs:77-89`).

## OUT

- Levels above the Feature and org-default-plus-override for SLE, staleness etc. (target language: Epic 6455 or a
  feature of its own). Deliveries (S03d, Promises). Feature WIP and throughput window (S03c, Forecasting).

## Dependencies

Hard: S02 (gate). Soft: S03d (the split of today's Portfolio/Delivery module), S05c/S05d (role-specific ports for
Team and Portfolio make this move smaller).

## Notes for DESIGN

Keep the context to structure and defaults, with no behaviour: ADR-270 names the god-context risk. Read ADR-270
Decisions 1, 2 (the acknowledged cycle with Tracker integration and how it is broken) and 8.
