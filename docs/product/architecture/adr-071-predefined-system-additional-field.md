# ADR-071: Predefined (System-Owned) Additional Field — Additive `IsPredefined` Flag, Auto-Registered for Jira, Excluded From User CRUD + Slot Limits; PRE-SLICE SPIKE REQUIRED

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (Slice 05 — Jira flagged via a predefined additional field)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: depends on ADR-067 (rule engine as the single blocked definition + `additionalField.{id}` as first-class rule keys). Resolves DISCUSS D-FLAGGED and R3 (the SPIKE question).

---

## Context

Today the Jira "Flagged" flag is a HACK: `IssueFactory` (L32–40) injects a synthetic `JiraFieldNames.FlaggedName = "Flagged"` LABEL into the item's `Labels` when the connection-resolved flagged custom field is non-empty; the user then adds "Flagged" to `BlockedTags`. The flagged value is NEVER written to `AdditionalFieldValues`.

Slice 05 replaces this with a **predefined / system-owned additional field**: auto-registered for Jira connections, referenceable as a rule field key (`additionalField.{id}`), fetched on sync, but EXCLUDED from the user-editable additional-fields list + slot limits, not user-deletable/editable. Designed generically (flag is the only instance now).

Verified code reality (the SPIKE-decision evidence):
- `AdditionalFieldDefinition { Id, DisplayName, Reference, WorkTrackingSystemConnectionId }` — NO `IsPredefined`/`IsSystem`/`IsReadOnly` flag exists.
- The list `WorkTrackingSystemConnection.AdditionalFieldDefinitions` is a flat user-managed `List` with NO ordering/positional constraints.
- **Slot limit** is a license gate at the API boundary: `SupportsAdditionalFields()` = `CanUsePremiumFeatures() || count < 2` (non-premium: max 1 user field). NO hard model max.
- CRUD is ONE controller helper `UpdateAdditionalFieldDefinitions()` (reconcile-by-id: add/update/remove). ANY field is deletable — no protection.
- Connectors (Jira + ADO) iterate the FULL list unfiltered to build fetch references and populate `AdditionalFieldValues : Dictionary<int,string?>` (on `WorkItemBase`, so Feature too) — a GENERIC id-keyed path, NO per-field special-casing.
- The rule schema is built from the connection's `AdditionalFieldDefinitions` (each → `additionalField.{Id}`); the field provider resolves by id from `AdditionalFieldValues` — GENERIC.
- A `WriteBackMappingDefinition` model can reference additional fields (a coupling surface — see SPIKE).

The two cruxes the explore confirmed: (A) the list IS deeply user-CRUD-coupled (controller reconcile, license count gate, DTO projection, two connectors, two schema builders, rule provider) — but the coupling is BROAD not DEEP (each surface treats the list homogeneously); (B) a field's value reaches the WorkItem and the rule provider through a fully GENERIC id-keyed path with zero special-casing.

---

## Decision

### 1. Additive `IsPredefined` flag on `AdditionalFieldDefinition` (generic, flag is only instance)

```csharp
public bool IsPredefined { get; set; } = false;   // system-owned, not user-created
```

Predefined fields live in the SAME `AdditionalFieldDefinitions` list (so the generic fetch/value/rule-schema paths work unchanged — the GENERIC id-keyed path is the whole reason this is cheap) but are tagged `IsPredefined = true`. Generic by design: nothing hard-codes "Flagged"; the flag is data.

### 2. Auto-registration for Jira connections, idempotent

On Jira connection sync/setup, Lighthouse ensures a predefined `AdditionalFieldDefinition { DisplayName = "Flagged", Reference = <the connection-resolved flagged custom-field reference>, IsPredefined = true }` exists (get-or-create by `(connectionId, IsPredefined, Reference)` — idempotent, NOT a `=true` sentinel). The connection already resolves the flagged custom-field reference per sync (the existing `FieldNames[...][FlaggedName]` resolution) — auto-registration reuses that resolution to populate `Reference`, so NO per-connector special logic beyond the single Jira registration hook. The flagged value then flows through the EXISTING `PopulateAdditionalFieldValues` generic path into `AdditionalFieldValues[predefinedId]`.

### 3. Exclusion from user CRUD + slot limits (the three threading points)

- **CRUD protection**: `UpdateAdditionalFieldDefinitions()` excludes `IsPredefined` fields from the reconcile — they are NOT in the user-editable set, so they are never in `toRemove` and never user-updated. A user PUT that omits them does NOT delete them; a user PUT that includes one is rejected/ignored for that entry.
- **Slot-limit exclusion**: `SupportsAdditionalFields()` counts only `where !IsPredefined` — predefined fields do not consume a user slot.
- **DTO surfacing**: the settings/connection DTO surfaces predefined fields as READ-ONLY entries DISTINCT from user-managed ones (a new `IsPredefined` flag on `AdditionalFieldDefinitionDto`). They appear in the rule field-key picker (so a rule can reference them) but NOT in the user's add/edit/delete list. The FE filters the editable list by `!isPredefined` and includes ALL fields (predefined + user) in the rule field picker.

### 4. Remove the synthetic-label HACK

Delete the `IssueFactory` synthetic-`"Flagged"`-label injection (L32–40) and the `JiraFieldNames.FlaggedName`-based label wiring. The flag flows ONLY through the predefined additional field. A grep test asserts no synthetic-label injection remains (slice-05 AC3).

### 5. **PRE-SLICE SPIKE REQUIRED (timeboxed, ~half-day) — verdict: SPIKE, not a thin additive slice**

The DISCUSS weight-note (R3) asked: thin additive flag (≤1 day, no spike) OR needs a SPIKE? **VERDICT: a timeboxed SPIKE is required before committing slice 05.** Evidence-based rationale (the coupling is BROAD across surfaces and there is NO existing precedent for a system-registered field):

- The flag itself is trivially additive AND the value/fetch/rule paths are GENERIC (favourable). BUT the exclusion threads through FOUR surfaces that today treat the list homogeneously: the CRUD reconcile (`UpdateAdditionalFieldDefinitions`), the license slot gate (`SupportsAdditionalFields`), the DTO projection (user-list vs rule-picker split), and the connector auto-registration (no precedent for system-creating a field).
- Open design questions the SPIKE must answer BEFORE the slice is sized: (a) can a predefined field be excluded from the user reconcile WITHOUT a user PUT silently deleting it (the reconcile removes anything not in the incoming set — predefined fields must be merged back)? (b) does the license slot count split cleanly into user-count vs total-count? (c) can `WriteBackMappingDefinition` reference a predefined field, and if so must its `Reference` be immutable? (d) is auto-registration a single idempotent Jira hook, or does it leak per-connector logic? (e) does the DTO split (user-editable list vs rule-picker-visible list) ripple to the FE additional-fields settings surface?
- The SPIKE's success criterion (the slice-05 learning hypothesis): "a predefined additional field can be excluded from the user-editable list + slot limits WITHOUT per-connector special logic, and the flag value maps to the existing operators." If the SPIKE finds the reconcile/DTO split or write-back coupling needs more than the four scoped touch-points above, slice 05 is re-sized or re-sliced BEFORE commitment.
- **SPIKE deliverable checklist (concrete artifacts/probes the SPIKE MUST produce — not just a yes/no)**:
  1. A throwaway proof-of-concept branch demonstrating the FOUR-surface change end-to-end: (a) the `UpdateAdditionalFieldDefinitions` reconcile MERGES predefined fields back so a user PUT omitting them does NOT delete them; (b) `SupportsAdditionalFields` counts only `where !IsPredefined`; (c) the DTO surfaces predefined fields read-only, split user-editable-list vs rule-field-picker; (d) a single idempotent Jira auto-registration hook.
  2. An integration test for EACH threading point (a–d above) — concrete tests, not "open questions": a user-PUT-omits-predefined test, a slot-count test, a DTO-projection test, a re-sync-idempotency test.
  3. A `WriteBackMappingDefinition` compatibility finding: does any write-back mapping reference an additional field by id, and if so MUST a predefined field's `Reference` be immutable after registration? Record the answer with the code pointer.
  4. A go/re-slice verdict: if any coupling exceeds the four touch-points (e.g. ordering, a second connector hook, a write-back constraint), record the revised ADR-071 OR-clause and re-size slice 05 BEFORE commitment.
- **SPIKE completion gate + re-scope trigger**: the SPIKE MUST complete with an explicit go/no-go verdict BEFORE slice 05 is committed to DELIVER. If the SPIKE finds coupling that exceeds the four threading points above (e.g. ordering, a second connector hook, a write-back immutability constraint), slice 05 is re-scoped or deferred — MoSCoW **Could** permits deferral — rather than carrying the discovered debt forward into code. The SPIKE start, end, and verdict are recorded in `distill/upstream-changes.md`.
- This SPIKE does NOT block slices 01–04 (slice 05 is MoSCoW Could, last by priority). It is a pre-slice-05 gate only.

---

## Alternatives Considered

**Option A (chosen): additive `IsPredefined` flag in the SAME list, generic, + a pre-slice SPIKE.**
- Pros: reuses the entirely generic fetch/value/rule-schema/provider path (the expensive part is already generic); the flag is data not code; one Jira registration hook; the SPIKE de-risks the four threading points before sizing.
- Cons: threads exclusion through four surfaces; no system-registered-field precedent ⇒ SPIKE needed. Honest.

**Option B: a SEPARATE `PredefinedAdditionalField` collection/table distinct from the user list.**
- Cons: the generic fetch/value/rule-schema paths all read the ONE `AdditionalFieldDefinitions` list — a separate collection would force every generic consumer (two connectors, two schema builders, the provider) to union two lists, multiplying the touch-points the chosen option avoids. Rejected — the same-list-plus-flag keeps the generic path generic.

**Option C: keep the synthetic-label hack, just make it a first-class user field.**
- Cons: the DISCUSS problem statement (US-05) explicitly rejects this — it moves the hack (user must know the custom-field id, eats a slot). Rejected by product decision.

**Option D: ship slice 05 as a thin additive flag with no SPIKE.**
- Cons: the four threading points + no precedent risk a mid-slice discovery that the reconcile/DTO/write-back coupling is deeper than additive. Rejected — the evidence (broad coupling, no precedent) crosses the R3 SPIKE threshold. A half-day SPIKE is cheaper than a mid-slice re-slice.

---

## Consequences

**Positive**:
- The flag becomes "just another additional field" through the generic id-keyed path — no custom connector logic at evaluation time; the synthetic-label hack is deleted.
- Generic predefined-field mechanism for future system fields (none imminent).
- The SPIKE de-risks the slice before commitment (R3 honoured with evidence).

**Negative**:
- Exclusion threads through four surfaces; a SPIKE is the entry cost.
- A migration adds `IsPredefined` (additive bool, default false — safe; real-provider migration test).

**Neutral**:
- Predefined fields are read-only in the user UI; no new write authorization (RBAC: blocked-rule config gate from ADR-067).

---

## Earned Trust — probing the predefined field

- **Generic-path probe**: a flagged Jira item's flagged value lands in `AdditionalFieldValues[predefinedId]` via the existing `PopulateAdditionalFieldValues` (no special-case); a rule `additionalField.{predefinedId} isnotempty` ⇒ `IsBlocked` true, with NO "Flagged" label on tags (slice-05 AC1).
- **Exclusion probe**: a user settings PUT that omits the predefined field does NOT delete it (the reconcile merges it back); the field is absent from the user-editable list and consumes no slot (slice-05 AC2).
- **Auto-registration idempotency probe**: re-sync ⇒ one predefined field per connection (get-or-create, no duplicate, no sentinel).
- **Hack-removal probe**: grep asserts no synthetic-label injection remains (slice-05 AC3).
- **SPIKE gate probe**: the SPIKE empirically confirms the reconcile-merge, slot-count split, write-back compatibility, single-hook registration, and DTO split BEFORE the slice is sized.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Predefined fields excluded from user CRUD (no silent delete on PUT) | NUnit on `UpdateAdditionalFieldDefinitions`: user PUT omitting a predefined field preserves it |
| Predefined fields do not consume a user slot | NUnit on `SupportsAdditionalFields`: count excludes `IsPredefined` |
| Predefined field auto-registered once per Jira connection (idempotent) | NUnit: re-sync ⇒ one predefined field; get-or-create, no sentinel |
| Value/fetch/rule paths unchanged (generic id-keyed) | Integration: flagged value in `AdditionalFieldValues`; rule resolves it; ArchUnitNET: no per-field special-casing |
| Synthetic "Flagged" label injection removed | Grep test asserts no `FlaggedName` label wiring (slice-05 AC3) |
| Pre-slice SPIKE completed before slice-05 commitment | DELIVER gate: SPIKE findings recorded; slice re-sized if coupling exceeds the four touch-points |

---

## Cross-feature impact

- ADR-067: predefined field is referenced as an `additionalField.{id}` rule key in the single blocked definition.
- Connectors: a single Jira auto-registration hook; ADO/others unchanged.
- Lighthouse-Clients: the additional-field/settings contract gains an `IsPredefined` read-only distinction — version-gated (ADR-072 §predefined).
- No new bounded context; extends the existing additional-field mechanism.
