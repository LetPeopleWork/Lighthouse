# Slice 04 — Auto-save general portfolio settings + RBAC parity

Story: US-5077-04 | job: job-commit-intent-no-button | Should Have

## Goal
The portfolio (`ModifyProjectSettings`) settings form gets the same auto-save behavior as Slice 1,
respecting `canUpdatePortfolioData` RBAC/licensing gating.

## IN scope
- Portfolio general-settings auto-save via Slice-1 mechanism; remove the Save button.
- Suppress auto-save where `canUpdatePortfolioData` is false (parity with `disableSave={!canUpdatePortfolioData}`, line 276).
- Portfolio state-mappings auto-save + auto-refresh (portfolio embeds `StateMappingsEditor`) — reuse Slice 2.

## OUT of scope
- Forecasts (5/6).

## Learning hypothesis
**Confirms** the Slice-1 mechanism generalizes cleanly to the portfolio form (parallel
`useModifySettings` consumer) with only the RBAC-gate wiring differing — proving the mechanism is
genuinely reusable, not team-specific.
**Disproves** reusability if the portfolio form needs a divergent save-state or gate path — a
signal the mechanism leaked team-specific assumptions.

## Acceptance criteria
- [ ] Valid portfolio-settings edit auto-saves (Slice 1 mechanism), no Save button.
- [ ] No auto-save where `canUpdatePortfolioData` is false (RBAC/licensing parity).
- [ ] Portfolio state mappings auto-save + auto-refresh (Slice 2 reuse).
- [ ] Invalid portfolio form fires no save; inline error shown.

## Dependencies
Slice 1 (mechanism); Slice 2 (portfolio state-mapping auto-refresh half).

## Effort estimate
~1 day.

## Reference class
`ModifyProjectSettings.tsx` (Save at line 273-278, `disableSave={!canUpdatePortfolioData}` line 276);
mirrors Slice 1 on the portfolio consumer of `useModifySettings`.

## Cross-cutting
- RBAC: suppress auto-save where `canUpdatePortfolioData` false (ModifyProjectSettings:276). No new authz.
- Clients: N/A (no API change; reuses portfolio PUT).
- Website: N/A.

## Dogfood moment
Edit a portfolio's settings locally; confirm auto-save + RBAC parity (try as a user who lacks the right).
