# Slice 04 — Standalone mode explains why OAuth is unavailable

**Feature**: work-tracking-oauth-authentication
**ADO stories rolled in**: #4970
**Effort estimate**: 2–3 hours of crafter dispatch
**Reference class**: frontend-only guard with tooltip + docs link

## Goal
A standalone (Tauri desktop) Lighthouse user opens the work-tracking-system connector form, sees the OAuth option rendered disabled with a clear tooltip explaining *why*, and follows the docs link to learn about server vs. standalone.

## IN scope
- Frontend gating: the `AuthMethodDropdown` reads the existing runtime-mode flag and renders any `*.oauth` entries as disabled when `mode === 'standalone'`.
- Tooltip: *"OAuth authentication requires the server version of Lighthouse. Learn more →"* with a link to a new docs page.
- Docs page: *Server vs. standalone — when OAuth is available* (a short explainer; reuses the existing standalone/server overview page if it exists).
- Vitest unit test on `AuthMethodDropdown` covering both modes.
- Playwright E2E that starts the standalone build and asserts the OAuth entry is disabled (uses the existing standalone-mode test harness).

## OUT scope
- Any backend change. (AC #3 is the load-bearing invariant.)
- Implementing OAuth for standalone mode — that is a separate epic if it ever happens (would require a local-redirect-listener scheme; explicitly out of scope per D2).
- Feature-flag plumbing for "OAuth eligibility" beyond what is already needed for premium gating.

## Learning hypothesis
**Disproves if it fails**: "the explanation alone is enough — standalone users are not lost or angry about the absence of OAuth." Failure mode: docs-site analytics show high bounce or support tickets keep arriving asking "where is OAuth?". (Measured in the 30 days after release; not a CI gate.)

**Confirms if it succeeds**: "we can scope features to server-mode without misleading standalone users."

## Acceptance criteria
See US-04 AC #1–3 in `feature-delta.md`. **AC #3 is load-bearing**: this slice must touch only frontend code — no backend route, no DB migration, no DI registration is altered.

## Production-data requirement
None — this is pure UI guard. Synthetic test fixtures are honest here because there is no real-data path through this slice.

## Dogfood moment
Same-day: launch the locally-built standalone app, open a connector form, confirm the tooltip and the docs link land on a real published page (not a 404).

## Dependencies
- Slices 01 and 03 must have landed (otherwise there is no OAuth entry to disable in the dropdown).
- Existing standalone-vs-server runtime flag (already used by the OIDC gating per `docs/product/architecture/brief.md`).

## Pre-slice SPIKE
None.

## Carpaccio taste tests
- *4+ new components?* Zero — extends one existing component (`AuthMethodDropdown`) and adds one docs page. PASS.
- *Every slice depends on a new abstraction?* No.
- *Disproves a pre-commitment?* Yes — disproves "users will understand the absence" claim, measured in support tickets / docs analytics.
- *Synthetic data only?* Acceptable — no real-data path.
- *Identical-at-scale to another slice?* No.

**Verdict**: PASS (synthetic data acknowledged and justified).
