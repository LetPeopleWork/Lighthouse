# Slice 05 — Jira flagged via a predefined (system) additional field

**Job**: `job-config-admin-define-blocked-rules` (this slice removes a special-case under the same
foundation job — it is config debt-retirement enabled by slice 01, not a new value job)
**Persona**: config-admin | **MoSCoW**: Could | **Est**: ~1 day (see weight note) | **Premium**: No

## Goal (one line)
Introduce a **predefined, system-owned additional field**, auto-registered for Jira connections, that the
Jira "Flagged" flag flows through — so the flag becomes "just another additional field" (referenceable by
any rule, fetched on sync) with **no custom/special-case connector logic**, replacing the synthetic
`"Flagged"` label HACK in `IssueFactory`.

## Why it's last
It depends on slice 01's rule engine being the single blocked definition AND on the
`additionalField.{id}` condition being a first-class rule key. Once a custom field is a first-class rule
condition, the right move is to make the flag a *predefined* additional field rather than re-express the
synthetic-label hack as a hand-configured field. Doing it before slice 01 would mean re-plumbing the hack
rather than deleting it.

## Learning hypothesis
**Disproves "a predefined system additional field cleanly subsumes the Jira flagged special case without
custom connector logic" if**: the predefined-field concept can't be excluded from the user-editable
additional-fields list (it leaks into the user's add/edit/delete surface or consumes a user slot/limit),
OR auto-registration needs per-connector special logic anyway (so we've just moved the special case, not
removed it), OR the flag value semantics don't map to the existing operators (so a predefined field plus
the six operators can't reproduce the flagged read).

## In scope
- **Net-new predefined / system additional-field concept** (verified: no `IsPredefined`/`IsSystem` flag
  exists today; additional fields are a user-managed `List<AdditionalFieldDefinition>` on
  `WorkTrackingSystemConnection`). Predefined fields are system-owned, not user-created. **Design the
  concept generically** (a reusable predefined-field mechanism), even though the Jira "Flagged" field is
  its only instance for now — future predefined fields (e.g. connector-specific equivalents) are
  anticipated but none are imminent, so do NOT hard-code the mechanism to the flag.
- **Auto-registration** of the Jira "Flagged" field (`JiraFieldNames.FlaggedName = "Flagged"` + the
  connection-specific flagged custom-field reference the connector already resolves) as this predefined
  additional field for Jira connections — generic, no per-item special-case logic.
- **Exclusion from the user-customizable list + limits**: the predefined field does NOT appear in the
  user's editable/addable additional-fields list, is NOT user-deletable or user-editable, and does NOT
  consume any user additional-field slot/limit. It is system-managed.
- It **is** available as a rule field key (the blocked rule — and any rule consumer — can reference it
  like any additional field) and is **fetched on sync** like any additional field.
- **Removal of the synthetic label**: delete the synthetic `"Flagged"` label injection in `IssueFactory`
  and the `JiraFieldNames.FlaggedName`-based label wiring; the flag now flows only through the predefined
  additional field.

## Out of scope
- Any change to non-Jira connectors (no predefined field auto-registered for them in this slice).
- New rule operators (the existing six suffice). User-managed additional-field CRUD behaviour is
  unchanged (predefined fields sit alongside it, read-only).

## Production-data AC (drive via demo data + real connector)
- AC1: A Jira flagged item reads blocked via a blocked rule that references the **predefined flagged
  additional field** — WITHOUT any synthetic `"Flagged"` label existing on the item's tags.
- AC2: The predefined flagged field is **NOT listed among the user-addable additional fields** and
  **cannot be deleted or edited by the user**, and it does not consume a user additional-field slot/limit.
- AC3: No code path injects a synthetic `"Flagged"` label after this slice; grep confirms the
  `JiraFieldNames.FlaggedName`-based label injection is gone.
- AC4: The predefined flagged field is fetched on sync like any additional field and is offered as a
  selectable rule field key.

## Dogfood moment
Flag an item in real Jira; with the predefined flagged field referenced in a blocked rule, confirm it
reads blocked in Lighthouse, the field appears in the rule field picker but NOT in the user-editable
additional-fields list, and no synthetic label appears on the item.

## Weight note (carpaccio taste test)
Introducing a **net-new predefined/system additional-field concept** (a new flag on the field-definition
model + threading it through fetch/sync, the rule field-key list, and the settings read surface, while
excluding it from the user-editable list and limits) raises this slice's weight above the other four. It
is still plausibly ≤1 day IF the predefined concept is a thin additive flag and auto-registration is a
single Jira-connection hook. **If brownfield exploration shows the additional-field list is deeply
user-CRUD-coupled (limits, ordering, persistence) such that a read-only system entry needs threading
through many surfaces, this becomes a candidate for a pre-slice SPIKE** ("can a predefined additional
field be excluded from the user-editable list + limits without per-connector special logic?") before
committing slice 05. Flagged in the brief / Risks.

## Cross-cutting
- **RBAC**: no new surface (blocked-rule config gate from slice 01); predefined fields are read-only in
  the settings UI, so no new write authorization.
- **Clients**: the **additional-field / settings contract changes** — predefined (system) fields now
  surface on the settings DTO as read-only entries distinct from user-managed ones. Clients that read or
  wrap additional-field config must distinguish predefined from user-editable fields; **version-gate**
  the changed contract (old server lacks the predefined flag) — pre-check server version, fail with a
  clear "upgrade Lighthouse" error, pin `FEATURE_REQUIRES_SERVER_NEWER_THAN` strictly-newer-than the last
  released version. See feature-delta System Constraints.
- **Website**: N/A (non-premium).

## Dependencies
Slice 01 (rule engine as the single blocked definition, `additionalField.{id}` conditions as first-class
rule keys).
