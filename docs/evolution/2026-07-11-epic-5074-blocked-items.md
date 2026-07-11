# Epic 5074 — Blocked Items Improvements

Finalized: 2026-07-11 · Feature-id: `epic-5074-blocked-items` · Paradigm: OOP (ports-and-adapters) · Non-premium

## Summary

Replaced Lighthouse's hardcoded `BlockedStates` / `BlockedTags` blocked definition with a rule-engine-driven,
configurable blocked concept, then built a family of blocked-flow signals on that foundation: per-sync blocked-time
capture, a blocked-over-time chart, blocked→stale linkage, previous-period trend, max-blocked-age RAG status, and a
drill from the chart into the items blocked at a point in time. The Jira "Is Flagged" synthetic-label hack was removed
in favour of a net-new **predefined (system-owned) additional field** that Lighthouse auto-registers for Jira
connections — so the flag becomes "just another additional field," referenceable as a rule key but system-managed.

## Business context

Blocked was previously a fixed, non-configurable notion (state/tag lists baked into the backend). Flow coaches and RTEs
could not express their team's real blocked policy, had no history of how long items stayed blocked, and no way to see
blocked trend or drill into what was blocked when. The epic makes blocked a first-class, team-configurable signal built
on the existing `WorkItemRuleSet` / `RuleEvaluator<WorkItem>` engine (ADR-013) — no new engine, no premium gate.

## Work completed (8 slices)

| Slice | Story | Outcome |
|-------|-------|---------|
| 01 | 5265 | Rule-based blocked definition (walking skeleton) + auto-migration of `BlockedStates`/`BlockedTags` → OR'd rule conditions. Include semantics (matched = blocked). |
| 02 | 5266 | Per-sync blocked-time capture via new `WorkItemBlockedTransition` entity off the `WorkItemBlocked` event + net-new leave-detection. |
| 03 | 5267 | Blocked-over-time chart on the forward-only count-snapshot pattern (`BlockedCountSnapshot`). |
| 04 | 5268 | Blocked→stale linkage — distinct trigger keyed on blocked DURATION (`blockedStalenessThresholdDays`, 0 = disabled), OR'd with time-in-state staleness, distinct reasons. |
| 05 | 5269 | Jira flag via a **predefined additional field**: additive `IsPredefined` flag, `IWorkTrackingConnector.GetPredefinedAdditionalFields` port, GET-time idempotent auto-registration, inbound-only + slot-excluded + user-CRUD-excluded, synthetic label deleted. |
| 06 | — | Previous-period trend on the blocked overview widget (FE-only, reused WidgetShell trend chrome). |
| 07 | — | Max-blocked-age RAG status on the same widget (FE-only, re-drove the RAG call site from count to max age). |
| 08 | 5436 | Drill from a blocked-over-time bar into the items blocked at that date, reconstructed from `WorkItemBlockedTransition` intervals (ADR-099). |

## Key decisions

- **D-ENGINE / ADR-067**: Blocked reuses the existing rule engine as its third consumer (after DeliveryRule and
  ForecastFilter), with Include semantics. No new engine, no new operators.
- **D-CAPTURE / ADR-068**: Blocked-time is Lighthouse-side per-sync history (own `WorkItemBlockedTransition` entity),
  not `WorkItemStateTransition`; leave-detection is net-new.
- **D-CHART / ADR-069**: Blocked-over-time uses the forward-only count-snapshot pattern, not per-item reconstruction —
  except B1 (slice 08) point-in-time membership, which is genuinely new (ADR-099, interval-overlap reconstruction).
- **D-STALE / ADR-070**: Blocked staleness is a DISTINCT duration-keyed trigger amending ADR-026, OR'd with time-in-state.
- **D-FLAGGED / ADR-071**: The Jira flag is a predefined system-owned additional field, auto-registered inbound-only,
  immutable Reference, slot-excluded, not user-editable — no special-case connector logic. SPIKE waived (amended
  2026-07-11); the `GetPredefinedAdditionalFields` port seam is the driven-boundary contract.
- **ADR-072**: Blocked contract changes are client-version-gated (Lighthouse-Clients CLI/MCP handoff, separate repo).

## Lessons learned

- **Predefined-field dedup key must be uniform across writers.** The GET-time auto-registration and the Jira sync-path
  registration both get-or-create the predefined field; keying one on Reference (add-only) and the other on DisplayName
  diverged and risked a duplicate row on Reference drift. Fixed at slice-05 close-out to a shared `(IsPredefined, DisplayName)`
  key with in-place Reference update; mutation testing surfaced the latent duplicate and the fix is regression-locked.
- **Unpushed feature commits enter Sonar new-code all at once.** Slice-05's 10 commits were pushed together; the first
  main analysis evaluated the whole slice as new code and fired INFO smells (CA1859 return type, CA1869 JsonSerializerOptions
  caching, S107 ctor width in slice-08) that a green local `dotnet build` never shows. Pre-flight test files too.
- **`@screenshot` keeps the old PNG when the diff is <0.5%** — `rm` the PNG first to force a fresh write (slice docs).
- **Stryker line-range spans (`{l:c-l:c}`) silently match nothing in this setup** — scope feature mutation with whole-file
  `mutate` + a tight `test-case-filter` + post-filter the JSON report to the changed line ranges for the honest kill rate.

## Quality gates at finalize

- Backend build zero-warning; full backend suite 3436 passed / 3 skipped.
- Mutation (slice-05): backend 85.71% feature-scope, frontend 100% — ≥80% gate. Prior slices banked (01, 04, 08).
- Frontend tests green; Biome clean.

## Permanent artifacts

- ADRs: `docs/product/architecture/adr-067..072`, `adr-099` (already in the permanent architecture namespace).
- Journey SSOT: `docs/product/journeys/epic-5074-blocked-items.yaml`.
- Mutation + review records retained in the preserved workspace under `docs/feature/epic-5074-blocked-items/deliver/`.
