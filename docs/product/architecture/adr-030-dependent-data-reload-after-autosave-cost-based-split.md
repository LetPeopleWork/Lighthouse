# ADR-030: Dependent-data Reload After Auto-save — Cost-based Auto vs One-click Split (D-RELOAD)

**Status**: Accepted
**Date**: 2026-05-29
**Feature**: remove-action-buttons (ADO #5077)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE
**Relates to**: ADR-029 (auto-save mechanism)

---

## Context

Once auto-save removes the Save click, two settings surfaces still have a side effect: the saved
change invalidates dependent data that must be recomputed.

- **State Mappings** (`StateMappingsEditor.tsx`): regrouping Doing/Done states changes how metrics are
  bucketed. Today a stopgap `Alert` (lines 109-111) tells the user "After saving, a data reload is
  needed for these changes to take effect."
- **Forecast Filter** (`ForecastSettingsComponent.tsx` `PremiumGatedForecastFilter`): changing the
  throughput-exclusion rule set requires recomputing throughput, which (per ADR-014 / the
  filter-forecast-throughput delta) feeds every forecast surface. Today a stopgap `Alert`
  (`data-testid="forecast-filter-takeeffect-hint"`, lines 100-109, commit 53e6287e) tells the user to
  "save these settings and then refresh throughput data on the team page."

D-RELOAD (user-locked) mandates: **never instruct the user to navigate elsewhere and refresh
manually.** The in-place alert must carry a one-click action where a reload is needed, OR the reload
must happen automatically — and both stopgap Alerts must be deleted.

The open design question D-RELOAD defers to DESIGN: *which* surfaces auto-refresh and which surface a
one-click action. The deciding axis is the **cost of the recompute**.

---

## Decision

**Split by recompute cost: cheap recompute → silent auto-refresh; expensive recompute → one-click
"Reload now" embedded in the in-place success affordance. Never a navigate-away instruction.**

| Surface | Recompute cost | Behaviour after auto-save succeeds |
|---|---|---|
| State Mappings (Slice 2) | Cheap — client re-fetch of already-computed metrics for the open team | **Silent auto-refresh**: on `saved`, the dependent metrics view re-fetches automatically; the "must reload" Alert is deleted. |
| Forecast Filter throughput (Slice 3) | Expensive — backend throughput recompute that fans out to all forecasts | **One-click "Reload throughput now"** rendered in-place next to the `saved` indicator; the `forecast-filter-takeeffect-hint` Alert is deleted. |

- The reload action is a thin call into the **existing** data-refresh path each surface already owns
  (State Mappings → the team metrics fetch; Forecast Filter → the throughput recompute trigger that the
  team page already invokes). No new endpoint, no new service method — D-6 / the cross-cutting checklist
  confirms no API/DTO change.
- The one-click action and the auto-refresh trigger are both driven off the **`saved`** terminal state
  of the ADR-029 machine, so a reload can only happen after a confirmed persist (never on a half-typed
  or failed save).
- **The expensive one-click path never auto-fires (OQ-2 resolved 2026-05-29, user).** On the Forecast
  Filter, a valid rule edit auto-saves silently but the throughput recompute waits for the user to
  click "Reload throughput now" — including the first `saved` of a session. This is the "expensive ⇒
  intentional" rule: the user controls when the heavy recompute runs while they are still tuning rules.
- Fallback (D-RELOAD invariant under failure): if an auto-refresh *fails*, the surface degrades to the
  one-click "Reload now" affordance rather than a navigate-away instruction. The user is never sent
  elsewhere.

### Where the reload affordance is rendered

A **small presentational `ReloadDependentDataAction` component** (a labelled inline action shown beside
`SaveStateIndicator`) is created for the expensive case, rather than extending the generic MUI `Alert`.
Rationale: the retired Alerts were *instructional text*; the replacement is an *action affordance* tied
to the save machine's `saved` state. Reusing `Alert` would re-introduce a text-first, action-second
shape — the very pattern D-RELOAD retires. The cheap (auto) case renders only a transient "Reloading…"
status via the existing `SaveStateIndicator` label set, no new component.

---

## Alternatives Considered

### Option A: Cost-based split — auto where cheap, one-click where expensive (selected)

**Accepted because**:
- Honours the user's emotional arc: cheap refresh disappears entirely (zero ceremony); an expensive
  recompute that would otherwise spin the UI is gated behind an intentional, single, in-place click —
  no surprise long operation, no navigation.
- Matches each surface's real cost; State Mappings metrics are already a client re-fetch, throughput
  is a known-expensive backend recompute fanning out to forecasts (ADR-014).

### Option B: Always auto-refresh both (rejected)

**Rejected because**:
- A silent expensive throughput recompute on every keystroke-debounced filter edit would trigger
  repeated heavy backend work and a flickering forecast surface — poor performance efficiency and a
  jarring UX. The user explicitly called the throughput recompute "expensive" — the locked decision
  text records it.

### Option C: Always one-click both (rejected)

**Rejected because**:
- Re-introduces ceremony where none is warranted: State Mappings metrics are cheap to re-fetch, so
  forcing a click there is gratuitous friction — it would only half-deliver the feature's North Star
  (valid-input-acts-immediately).

### Option D: Keep the Alerts, just add an in-Alert button (rejected)

**Rejected because**:
- D-RELOAD explicitly retires both stopgap Alerts (KPI 2 baseline 2 → target 0). The instructional-text
  shape is the debt being removed; bolting a button onto it keeps the text-first framing.

---

## Consequences

**Positive**:
- Both stopgap Alerts deleted (KPI 2 → 0), verifiable by grep + test.
- Reload always in-place; D-RELOAD invariant ("never navigate away") holds even on the failure path.
- No backend change — reuses each surface's existing refresh path.

**Negative**:
- Two slightly different post-save behaviours (auto vs one-click) across the settings surfaces. Accepted:
  the difference is intentional and cost-justified, and the *save* mechanism (ADR-029) remains uniform —
  only the downstream reload differs, by documented cost.

**Quality attribute impact**:
- Performance efficiency: improved — expensive recompute is no longer triggered silently/repeatedly.
- Usability: improved — cheap path is zero-ceremony; expensive path is a single intentional click.
- No-regression: preserved — reload reuses existing data paths; no new contract.
