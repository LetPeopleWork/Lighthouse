# Slice 02 — Name work by its label

**Goal**: A flow coach types `Change Request` in a team's Work Item Types and the team syncs change
requests, without having to learn that the column value is `change_request`.

**Story**: Story B (value).

## IN scope
- A **static, case-insensitive label→class map in source** for the stock ITSM task hierarchy (D8).
  Confirmed present on the PDI across the SPIKEs: `incident`, `problem`, `change_request`, `sc_task`,
  `task`. The rest of the hierarchy (`sc_req_item`, `change_task`, `incident_task`, `problem_task`) is
  a DESIGN call — OC-1.
- Normalisation applied in `ServiceNowReadScope.For` (`ServiceNowReadScope.cs:45-52`), which today only
  trims. One `Select` through the map, falling back to the entry itself.
- Class names keep working unchanged, including entries pre-filled by #5610's board picker.
- An entry that is neither a known class name nor a map key is refused **by name** at validation.
- An entry matching both resolves to the **class name** (D8b).

## OUT of scope
- **Any runtime lookup.** `sys_db_object` is measured unreadable for the accounts that matter, and 5611
  already took this exit for hierarchy knowledge (ADR-116 D4). The map behaves identically at every
  privilege level, which is the whole reason to prefer it.
- **Using the map for display.** The Type column stays on `sys_class_name.display_value` (slice 01,
  D5/D8a) — instance-correct even where a class was renamed. The map is input-only.
- **Exhaustiveness or localisation.** It is an alias for the stock set, not a translation table. A
  customised or non-English instance keeps working via class names and loses nothing it has today.
- Changing what #5610's picker pre-fills — OC-4, a conversation with #5610, not code here.

## Learning hypothesis
**Disproves** "the class-name requirement is a knowledge barrier, not a typing one" **if** a coach
handed the alias still cannot name their work — which would mean the barrier was never the vocabulary
but the field itself, and #5610's board picker is the only real answer.
**Confirms** that a coach can describe their work in their instance's own words with no round trip to
a ServiceNow administrator.

## Acceptance criteria
See Story B, AC-B1–AC-B6 in `feature-delta.md`.

## Dependencies
- **None blocking.** No instance dependency by construction (D8).
- **OC-4** — agree with #5610 whether the board picker pre-fills the label or the class name once this
  ships. #5610 is at DISTILL on `main`; two stories landing opposite answers is the twin-drift shape
  that Bug #5613 already cost a release.
- Slice 01 is not a prerequisite, but shipping it first means a coach who types `Change Request` also
  sees `Change Request` back in the Type column, which is the coherent story.

## Effort / reference class
≤half a day. A `FrozenDictionary` (or equivalent) plus one `Select` in a pure class that is already
unit-tested end to end, plus one validation message. Closest reference class is 5611's static
hierarchy set, which was hours.

## Pre-slice SPIKE
**None.** An earlier draft of this brief mandated one; it was wrong. See the Correction in
`feature-delta.md` — `sys_db_object` being unreadable is a design constraint that removes the lookup,
not a research question that motivates probing for another.

## Dogfood moment
Same day: create a ServiceNow team on `dev191338` typing `Incident` and `Change Request` — never the
class names — and confirm the team syncs both. Then re-open the team and confirm the saved values
round-trip without surprising the coach.
