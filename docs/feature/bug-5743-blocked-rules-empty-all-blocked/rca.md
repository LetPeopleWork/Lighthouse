# Bug #5743 — blocked rules read as empty, every item shows blocked

Reported 2026-08-12 with two screenshots: a rule editor holding `State equals ""`,
`State equals "On Hold"` and `Tags contains ""` under Match ANY (OR), with the warning
"Please complete all rule fields before saving" that no editing clears — and a work item
list where every row carries the blocked marker.

## Root cause

`20260713183553` / `20260713183603_BackfillBlockedRuleSetJson` translated the legacy
`BlockedStates` and `BlockedTags` columns into the rule set format one array entry at a
time, with no filter on the entry itself. A team whose legacy list held an empty string
got a condition comparing against `""`.

The shape in the screenshot is that migration's signature: mode `or`, every state entry
first as `workitem.state equals <entry>`, then every tag entry as `workitem.tags contains
<entry>`. A rule set built by hand starts in `and` mode and would not order itself that
way.

`RuleEvaluator.EvaluateTagsCondition` then read `tags contains ""` as
`tags.Any(t => t.Contains(""))`. "Contains nothing" is true of every string, so the
condition matched every work item carrying at least one tag, and in OR mode that alone
marked the whole list blocked.

## Why it survived every guard

Three separate places should have caught this and none did:

- **The rule editor's warning is decoration.** `DeliveryRuleBuilder` flags an incomplete
  row but nothing consumes that flag. The autosave gate (`ModifyTeamSettings`) passes a
  permission flag as `canSave`, not a validity flag.
- **Adding a row writes it.** `handleAddRule` emits `{fieldKey, operator, value: ""}` and
  calls `onChange` immediately, so a row abandoned mid-edit was persisted 300 ms later —
  a second, still-live way to produce exactly the reported state without any migration.
- **Server-side validation was a no-op.** `TeamController` and `PortfolioController`
  deserialized the incoming rule set with no serializer options. Case-sensitive binding
  meant the camelCase `conditions` never bound, every rule set looked empty, and the
  "nothing to validate" early return fired for anything at all.

## What was ruled out

- **A dangling `additionalField.<id>`** (the leading theory before the screenshots). It
  explains an always-blocked board, but the Field dropdowns in the screenshot render
  populated — a field key missing from the schema renders the select blank.
- **`State equals ""`** as the cause of the blocked flood. It matches only items whose
  state is empty, which no synced item is. It is junk on screen, not a blocking rule.

## Fix

1. A `contains` / `notContains` condition with a blank value matches nothing, instead of
   everything (and nothing, respectively). `equals` / `notEquals` keep their meaning —
   `equals ""` legitimately selects items whose field is empty and rule-based deliveries
   depend on it.
2. `RepairIncompleteBlockedRuleConditions` strips value-requiring conditions stored
   without a value from every `Teams` and `Portfolios` blocked rule set, and NULLs a set
   left with nothing. Verified against SQLite and Postgres 17 through
   `ef database update`, seeded with the reported shape, an all-junk set, a healthy set,
   an `isEmpty` set and a non-JSON value.
3. The settings component holds the rows being edited, so an unfinished row stays on
   screen while only complete conditions are serialized into the payload.
4. Both controllers deserialize case-insensitively, putting the existing validation back
   in the save path.

## The same hole in the other rule surfaces

Blocked rules are one of four columns driven by the same engine and the same builder, so
the fix was carried across:

- **The exclusion rules** (a team's forecast filter, "Exclude items where…") wrote a
  half-typed row into the settings payload exactly like the blocked editor did. A blank
  contains value there would have excluded every item carrying anything in that field from
  the throughput a forecast runs on. Both editors now share one hook for the rows being
  edited.
- **Delivery rule definitions** were serialized with no options while every other column
  used camelCase. Writer and reader only agreed because *both* omitted them — an accident,
  not a decision, and any camelCase definition would have selected no features while
  reading as "no rules configured". One serializer now owns the format for all four
  columns: it reads either casing so old rows still load, and writes camelCase.
- **The evaluator guard is shared**, so a blank contains value is inert for exclusion and
  delivery rules too, not just blocked ones.

## Known follow-ups

- **Live validation is stricter than stored data.** With validation running again, a
  stored rule referencing a deleted additional field now fails `IsValid` and rejects the
  whole settings save with 400. `WorkTrackingSystemConnectionController.UpdateAdditionalFieldDefinitions`
  deletes and re-adds non-predefined fields with new ids, so such rows can exist. Not
  addressed here.
- **Migrated state rules compare against the raw state**, while the legacy
  `BlockedStates` held mapped names. Teams using state mappings lost their blocked signal
  in the same backfill.
