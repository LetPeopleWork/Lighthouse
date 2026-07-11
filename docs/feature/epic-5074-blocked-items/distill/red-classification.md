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

---

# Enhancement batch (2026-07-07) — slices 06-08 RED classification

Gate: pre-DELIVER fail-for-the-right-reason. Every scenario RED-ready (skip/pending markers, ADR-025); when enabled fails on `MISSING_FUNCTIONALITY`, not import/fixture/setup.

## Execution evidence

- `dotnet build Lighthouse.Backend.Tests` → **Build succeeded, 0 errors** (8 pre-existing NU1903 SQLite advisory warnings). `Slice08BlockedDrilldownScenarios/Specifications.cs` compile against today's types → RED-eligible, not BROKEN. Per the project policy, backend black-box HTTP ATs need **no `__SCAFFOLD__` stub** — the missing endpoint 404s / SPA-HTML-falls-through and the `ParseReferenceIds` guard converts that into a clean assertion.
- Frontend: `blockedTrend.ts` + `blockedMaxAgeRag.ts` scaffolds present with `__SCAFFOLD__` (FE modules MUST exist for import resolution). `pnpm biome check` clean; `pnpm tsc -b` exit 0; `pnpm vitest run blockedTrend.test.ts blockedMaxAgeRag.test.ts` → **10 skipped, 0 errored** (imports resolve → RED-ready, not BROKEN).

## Per-scenario classification

| # | Slice | Scenario | Tags | Classification | RED reason |
|---|---|---|---|---|---|
| 22 | 08 | Items_blocked_at_a_past_date_are_reconstructed_from_transition_intervals | @driving_port @us-eb1 | RED | `blockedItemsAtDate` endpoint absent (SPA fallback → clean assertion) |
| 23 | 08 | The_latest_date_reconstructs_from_the_live_blocked_set | @driving_port @us-eb1 | RED | endpoint absent |
| 24 | 08 | A_date_with_no_blocked_items_returns_an_empty_dialog | @edge @us-eb1 | RED | endpoint absent |
| 25 | 08 | The_reconstructed_membership_count_reconciles_with_the_snapshot_count | @invariant @us-eb1 | RED | endpoint absent |
| 26 | 08 | A_date_before_capture_started_is_served_as_a_partial_set | @edge @us-eb1 | RED | endpoint absent |
| 27 | 07 | computeBlockedMaxAgeRag — RED past threshold | @us-eb2 | RED | scaffold throws (max-age RAG unimplemented) |
| 28 | 07 | computeBlockedMaxAgeRag — AMBER aging band | @us-eb2 | RED | scaffold throws |
| 29 | 07 | computeBlockedMaxAgeRag — GREEN none aging | @us-eb2 | RED | scaffold throws |
| 30 | 07 | computeBlockedMaxAgeRag — GREEN when no blocked items | @edge @us-eb2 | RED | scaffold throws |
| 31 | 07 | computeBlockedMaxAgeRag — none when threshold 0 | @edge @us-eb2 | RED | scaffold throws |
| 32 | 06 | computeBlockedTrend — up when current > prior boundary | @us-eb3 | RED | scaffold throws |
| 33 | 06 | computeBlockedTrend — down when current < prior boundary | @us-eb3 | RED | scaffold throws |
| 34 | 06 | computeBlockedTrend — flat when equal | @us-eb3 | RED | scaffold throws |
| 35 | 06 | computeBlockedTrend — undefined when no boundary snapshot | @edge @us-eb3 | RED | scaffold throws |
| 36 | 06 | computeBlockedTrend — undefined for empty/null history | @edge @us-eb3 | RED | scaffold throws |

## Summary (enhancement batch)

- 15 scenarios: **15 RED (MISSING_FUNCTIONALITY)**, **0 BROKEN**, 0 GREEN (foundation walking skeleton already shipped slices 01-04 — these extend it, no new skeleton needed).
- Error/edge/invariant (non-happy-path) = 8/15 ≈ **53%** (target ≥40%). ✓
- Authored in DELIVER (against EXISTING chrome, not new scaffolds): B1 chart `onItemClick` → `WorkItemsDialog` (Vitest); widget-level assertions on existing test-ids `rag-status` (B2), `widget-trend-*` (B3); one Playwright drill-through walking skeleton (demo-data POM).
- Gate verdict: **PASS — RED is genuine.** DELIVER may proceed one scenario at a time (backend: remove `[Ignore]`; frontend: `describe.skip` → `describe`, implement, remove `__SCAFFOLD__`).

---

# Slice 05 (Story 5269) — Jira flagged via a predefined (system) additional field — RED classification

Feature-id: `epic-5074-blocked-items` | Wave: DISTILL | Date: 2026-07-11 | Design authority: ADR-071 (amended — SPIKE WAIVED; `GetPredefinedAdditionalFields` connector port method). Scope: slice-05 (previously deferred behind the pre-slice-05 SPIKE gate, now in scope).

Gate procedure identical to slices 01–04: build → confirm COMPILES (RED-eligible, not BROKEN) → run → classify → spot-check two representative RED scenarios for a clean assertion, then re-`[Ignore]`.

## Execution evidence

- `dotnet build Lighthouse.Backend.Tests` → **Build succeeded, 0 errors** (8 pre-existing NU1903 SQLite advisory warnings, unrelated). `Slice05PredefinedFieldScenarios/Specifications.cs`, `Slice05SyntheticLabelRemovalTests.cs`, and `Integration/Containers/PredefinedAdditionalFieldMigrationTests.cs` all compile against **today's** production types → every scenario is **RED-eligible, not BROKEN**. Per the project's `atdd-infrastructure-policy.md`, black-box HTTP/JSON ATs need **no `__SCAFFOLD__` stub**. The new observables that do not exist yet — the additional-field DTO's `isPredefined` flag, the auto-registered predefined field, the `IsPredefined` model/column, and the `GetPredefinedAdditionalFields` port method — are **never referenced as typed members**; every predefined-specific step asserts on served JSON (or reads the column via raw SQL), keeping the suite compiling black-box.
- `dotnet test --filter Slice05` → **Passed: 1 (walking skeleton), Skipped: 10, Failed: 0** (with all `[Ignore]`s in place; the migration `[Ignore]`d Testcontainers pair is excluded from the fast filter).
- Spot-check (un-ignored, run, re-ignored):
  - `Slice05SyntheticLabelRemovalTests.A_flagged_jira_issue_is_built_without_a_synthetic_flagged_label` → FAIL, clean assertion: *"IssueFactory must not inject a synthetic \"Flagged\" label … Expected: not some item equal to \"Flagged\" But was: < \"Lagunitas\", \"Flagged\" >"* → **MISSING_FUNCTIONALITY (correct RED)**. This is the behavioural equivalent of the ADR-071 "grep asserts no `FlaggedName` label wiring" enforcement rule (AC3).
  - `Slice05…A_settings_save_that_omits_the_predefined_field_preserves_it` → FAIL, clean assertion: *"A settings save that omits the predefined field must NOT delete it (reconcile merge-back). The predefined field must still be served after the save. … Expected: not <empty> But was: <empty>"* → **MISSING_FUNCTIONALITY (correct RED)**. The served body confirms `additionalFieldDefinitions:[{"id":1,"displayName":"Team","reference":"customfield_10050"}]` — no `isPredefined` member, no auto-registered field.
- Frontend: `pnpm vitest run AdditionalFieldsEditor.predefined.test.tsx` → **1 passed (control), 3 skipped**. Spot-check un-skipping the pending `describe` → **3 failed on clean assertions** (`expect(queryByText("Flagged")).not.toBeInTheDocument()` — *found <span>Flagged</span>*; edit/delete controls present; free-plan Add button disabled) → **MISSING_FUNCTIONALITY**, then reverted to `describe.skip`. `pnpm biome check` clean.

## Per-scenario classification

Legend as above. Several slice-05 REDs depend on the *auto-registered, served* predefined field (see UC-5 in `upstream-issues.md`): they are genuinely RED now (no predefined field is surfaced by today's code) and become GREEN in DELIVER once auto-registration is wired to fire in the WAF host.

| # | Scenario | Tags | Classification | RED reason |
|---|---|---|---|---|
| 37 | A_connection_round_trips_its_additional_field_configuration | @walking_skeleton @driving_port @real-io @us-05 | **GREEN** (verified) | — (proves the connection additional-field read/write round-trip wiring slice-05 extends) |
| 38 | A_flagged_item_reads_blocked_through_the_flagged_field_without_a_synthetic_label | @driving_port @us-05 | RED | rule-based `IsBlocked` over the flagged field absent (generic id-keyed path); no synthetic label on tags |
| 39 | The_flagged_field_value_drives_blocked_through_the_generic_field_path (flagged=true) | @driving_port @property @us-05 | RED | rule-based `IsBlocked` absent |
| 40 | The_flagged_field_value_drives_blocked_through_the_generic_field_path (flagged=false) | @driving_port @property @us-05 | RED | rule-based `IsBlocked` absent (negative case) |
| 41 | A_settings_save_that_omits_the_predefined_field_preserves_it | @error @edge @us-05 | RED (verified) | auto-registration + reconcile merge-back absent (predefined field not surfaced; today's reconcile removes anything not in the incoming set) |
| 42 | A_predefined_field_does_not_consume_a_user_field_slot_on_a_non_premium_connection | @edge @us-05 | RED | `SupportsAdditionalFields` slot-count exclusion (`where !IsPredefined`) + auto-registration absent |
| 43 | Only_a_jira_connection_contributes_a_predefined_flagged_field (Jira) | @property @us-05 | RED | `GetPredefinedAdditionalFields` port method absent — Jira must contribute `[Flagged]` |
| 44 | Only_a_jira_connection_contributes_a_predefined_flagged_field (ADO/Linear/Csv) | @property @us-05 | RED / PASS-WHEN-ENABLED | others must contribute `[]`; passes trivially today (none surfaced), pins the port default |
| 45 | A_jira_connection_surfaces_exactly_one_predefined_field_stably | @edge @us-05 | RED | idempotent get-or-create auto-registration absent (no duplicate, no `=true` sentinel) |
| 46 | A_predefined_field_is_inbound_only | @error @us-05 | RED | `Reference` immutability + write-back-target exclusion absent (predefined field not surfaced) |
| 47 | A_flagged_jira_issue_is_built_without_a_synthetic_flagged_label (AC3) | @error @us-05 | RED (verified) | `IssueFactory` L32–40 still injects the synthetic `FlaggedName` label |
| 48 | Migration_On{Sqlite,Postgres}_PersistsColumnAndDefaultsToFalse | @real-io @us-05 | RED | additive `IsPredefined` column + migration not yet authored (raw-SQL column read; Testcontainers/SQLite) |
| FE | AdditionalFieldsEditor.predefined — 3 pending (`describe.skip`) | @us-05 | RED (verified) | editable list must filter `!isPredefined`; predefined not editable/deletable; predefined consumes no free-plan slot |

## Summary (slice 05)

- 11 backend scenario cases (walking skeleton + 8 `[Ignore]`-pending HTTP incl. 2 parametrized TestCase families + AC3 IssueFactory + migration pair) + 3 FE pending Vitest tests + 1 FE GREEN control.
- **1 GREEN** (walking skeleton), rest **RED (MISSING_FUNCTIONALITY)**, **0 BROKEN** — the whole slice-05 suite compiles and each RED fires on a business assertion (or raw-SQL column absence, for the migration probe).
- Error/edge/property (non-happy-path) share among the slice-05 scenarios ≈ **80%** (target ≥40%). ✓
- Contract-shape (2026-05-15 mandate): walking skeleton = *unbounded-preservation* (round-trip, system otherwise unchanged); AC1/AC4 flagged-reads-blocked = *bounded-change* (one item's `isBlocked` flips, no synthetic label added); AC3 = *unbounded-preservation* (issue built with the flag consumed but **no** synthetic label appended). Expressed via assertions (no Gherkin-tag surface in this NUnit repo — see `upstream-issues.md`).
- **DELIVER wiring note (UC-5)**: the auto-registration-dependent REDs (#41, #42, #43, #45, #46) need a WAF DI seam so the predefined field is surfaced without a live Jira — mirror the existing `ILicenseService` fake. Documented in `upstream-issues.md`.
- Gate verdict: **PASS — RED is genuine.** DELIVER may proceed one scenario at a time from the walking skeleton (backend: remove `[Ignore]`; frontend: `describe.skip` → `describe`).
