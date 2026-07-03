# Epic 5074 — DISTILL Pre-DELIVER RED Classification

Feature-id: `epic-5074-blocked-items` | Wave: DISTILL | Date: 2026-07-03 | Scope: slices 01–04 (slice-05 SKIPPED — pre-slice-05 SPIKE gate, ADR-071).

Gate procedure: build the acceptance suite (confirm it COMPILES → RED-eligible, not BROKEN), run it once, classify each scenario's status. Two representative RED scenarios were temporarily un-ignored and executed to confirm they fail for the **right reason** (missing functionality — a clean assertion), then re-`[Ignore]`d.

## Execution evidence

- `dotnet build Lighthouse.Backend.Tests` → **Build succeeded, 0 errors** (8 pre-existing NU1903 SQLite package-advisory warnings, unrelated to this suite). The whole suite compiles against today's production types → every scenario is **RED-eligible, not BROKEN**. This is the C#/.NET fulfilment of Mandate 7 (RED-ready): the project's `atdd-infrastructure-policy.md` explicitly excludes the Python `__SCAFFOLD__` stub mechanism; black-box HTTP/JSON ATs compile against the existing `Program` + DTOs and fail on missing behaviour, so **no production scaffold files are needed or created**.
- `dotnet test --filter BlockedItems` → **Passed: 1, Skipped: 20, Failed: 0, Total: 21**. The single non-ignored scenario is the walking skeleton (GREEN). All others are `[Ignore]`-pending (enable one-at-a-time in DELIVER).
- Spot-check (un-ignored, run, re-ignored):
  - `Slice04…The_blocked_staleness_threshold_is_saved_and_read_back` → FAIL, clean assertion: *"Settings payload must carry blockedStalenessThresholdDays … Expected: True But was: False"* → **MISSING_FUNCTIONALITY (correct RED)**.
  - `Slice03…The_blocked_count_trend_is_available_over_time` → initially failed via a raw `JsonReaderException` (the missing endpoint falls through to the SPA `<!doctype html>` fallback). Hardened the step to assert `body.StartsWith("[")` first, so it now fails as a clean assertion: *"The blockedCountHistory endpoint must return a JSON array, not HTML/other — the endpoint appears unimplemented. Body starts: <!doctype html>"* → **MISSING_FUNCTIONALITY (correct RED)**.
- Note: `SqliteException: no such table: Features` lines appear in stack traces during teardown — this is background hosted-service noise after `Database.EnsureDeleted()`, identical to the existing `TimeInStateReadApiIntegrationTest` precedent. It is **not** the classification of any scenario (each scenario's Error Message is its own assertion).

## Per-scenario classification

Legend: **GREEN** = passes now (walking skeleton). **RED** = fails on missing functionality (correct outer-loop signal). **PASS-WHEN-ENABLED** = would pass immediately because it validates a pre-existing cross-cutting guard that extends to the new contract (kept pending to honour one-at-a-time; classified honestly).

| # | Slice | Scenario | Tags | Classification | RED reason |
|---|---|---|---|---|---|
| 1 | 01 | An_admin_saves_a_teams_blocked_definition_and_reads_it_back | @walking_skeleton @driving_port @real-io @us-01 | **GREEN** (verified) | — (proves config-write → persist → settings-read wiring the epic extends) |
| 2 | 01 | Existing_blocked_config_is_preserved_as_equivalent_rules | @driving_port @us-01 @migration | RED | `blockedRuleSetJson` + auto-migration absent |
| 3 | 01 | A_custom_field_condition_makes_an_item_read_blocked | @driving_port @us-01 | RED | rule-based `IsBlocked` (additionalField condition) absent |
| 4 | 01 | Saved_blocked_rules_persist_across_reload | @driving_port @us-01 | RED | `blockedRuleSetJson` round-trip absent |
| 5 | 01 | An_item_matching_the_blocked_rules_reads_blocked_everywhere | @property @driving_port @us-01 | RED | single rule-based `IsBlocked` absent |
| 6 | 01 | A_team_with_no_blocked_config_blocks_nothing | @edge @us-01 | RED | empty-migration `blockedRuleSetJson` absent |
| 7 | 01 | A_blocked_rule_set_exceeding_the_maximum_conditions_is_rejected | @error @us-01 | RED | MaxRules validation on blocked rule set absent (unknown member ignored → 200) |
| 8 | 01 | A_blocked_rule_referencing_an_unknown_field_is_rejected | @error @us-01 | RED | unknown-field validation absent |
| 9 | 01 | A_non_admin_cannot_change_the_blocked_definition | @error @rbac @us-01 | PASS-WHEN-ENABLED | pre-existing TeamWrite guard already returns 403; validates it extends to the blocked-rule write |
| 10 | 02 | A_newly_blocked_item_shows_how_long_it_has_been_blocked | @driving_port @us-02 | RED | `blockedSince` read field / capture absent |
| 11 | 02 | An_unblocked_item_no_longer_shows_a_blocked_duration | @edge @us-02 | RED | `blockedSince` field absent (contract must expose it, even null) |
| 12 | 02 | A_first_observation_blocked_item_shows_no_duration_until_a_baseline_exists | @edge @us-02 | RED | `blockedSince` field absent |
| 13 | 02 | An_item_that_does_not_match_the_blocked_rules_has_no_blocked_duration | @property @us-02 | RED | `blockedSince` field absent |
| 14 | 03 | The_blocked_count_trend_is_available_over_time | @driving_port @us-03 | RED (verified) | `blockedCountHistory` endpoint absent (SPA fallback) |
| 15 | 03 | A_new_team_sees_an_honest_empty_trend | @edge @us-03 | RED | `blockedCountHistory` endpoint absent |
| 16 | 03 | The_blocked_trend_can_be_filtered_to_a_single_work_item_type | @deferred @us-03 | RED / **DEFERRED (UC-2)** | per-TYPE historical filtering is an additive follow-up; total-count snapshot is forward-only. See `upstream-issues.md`. |
| 17 | 04 | The_blocked_staleness_threshold_is_saved_and_read_back | @driving_port @us-04 | RED (verified) | `blockedStalenessThresholdDays` settings field absent |
| 18 | 04 | A_new_team_defaults_the_blocked_staleness_threshold_to_zero | @edge @us-04 | RED | field absent |
| 19 | 04 | A_blocked_staleness_threshold_below_range_is_rejected | @error @us-04 | RED | range validation absent (unknown member ignored → 200) |
| 20 | 04 | A_blocked_staleness_threshold_above_range_is_rejected | @error @us-04 | RED | range validation absent |
| 21 | 04 | A_non_admin_cannot_change_the_blocked_staleness_threshold | @error @rbac @us-04 | PASS-WHEN-ENABLED | pre-existing TeamWrite guard already returns 403 |

## Summary

- 21 backend acceptance scenarios. **1 GREEN** (walking skeleton), **18 RED (MISSING_FUNCTIONALITY)**, **2 PASS-WHEN-ENABLED** (pre-existing RBAC guard), **1 DEFERRED** (UC-2, counted within the 18 RED — enable only after the additive per-type snapshot column ships).
- **0 BROKEN.** Zero `ImportError`/compile/fixture-setup failures — the whole suite compiles and each RED fires on a business assertion.
- Error/edge/property/deferred (non-happy-path) = 14/21 ≈ **67%** (target ≥40%). ✓
- Gate verdict: **PASS — RED is genuine.** DELIVER may proceed one scenario at a time from slice 01.

## FE / E2E coverage authored in DELIVER (not backend ATs)

These observables are FE-derived (the `deriveStaleness` selector, MUI-X charts, badge copy) and are authored as Vitest / Playwright tests during DELIVER, not as backend HTTP ATs:

- Slice-02: the `blocked <N>d` badge copy + the "Approximate — based on sync cadence" tooltip (Vitest on the badge component; backend supplies `blockedSince`).
- Slice-03: the chart placement in the Flow Metrics chart area + the "builds forward from today — no snapshots yet" empty-state copy (Vitest on the chart component; backend supplies `blockedCountHistory`).
- Slice-04: the stale RENDERING — stale-once with a blocked-duration **driver** reason plus a time-in-state **context** reason (UC-1) — computed by `deriveStaleness` (Vitest on the selector; backend supplies `blockedStalenessThresholdDays` + `blockedSince`).
- Walking-skeleton E2E: ONE Playwright demo-data POM scenario (config-admin saves a blocked rule → an item reads blocked in the overview widget) is planned for DELIVER; the backend walking skeleton above is the single DISTILL green skeleton.
