# Slice 04 — The two dependency settings a Portfolio owns (free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-04, US-10 ·
**Estimate**: ~7h
**Reference class**: `ParentOverrideAdditionalFieldDefinitionId` end to end — the setting on
`IWorkItemQueryOwner:27`, its selector in the Portfolio settings form, its carry-through in
`PortfolioExtensions.cs:31` and `FetchFingerprint.cs:38`, and its read path at
`AzureDevOpsWorkTrackingConnector.cs:1012-1018` and `:1095-1106`. This slice is that mechanism a second
time, with one difference (D15).

## Goal

A Portfolio whose teams record dependencies in a custom field rather than the tracker's built-in link
gets the whole feature — column, dialog, warnings, and whatever the forecast does with them once
Epic #5792 ships — by naming that field once. And a Portfolio that wants to try a plan without its
dependencies can set them aside without hiding or deleting a single one.

Two settings, one form, one migration, one permission. **~7h, which is over the ≤6h dispatch target**
and is a stated exception rather than an oversight: splitting them ships two controls to the same
settings page in two releases. If the slice runs long, US-10 is the clean cut — it depends on slice 02
and on nothing in US-04.

## IN scope

- `DependencyOverrideAdditionalFieldDefinitionId`, nullable, on `IWorkItemQueryOwner`, beside the
  parent override. Additive migration via `CreateMigration`, expand-only.
- The read path: when set, **skip the relation fetch entirely** and read the value from
  `AdditionalFieldValues`. This copies the early return at `:1014-1018` deliberately — the parent
  override's comment ("no need to load stuff if we have an override anyway") is the behaviour, not an
  optimisation to reconsider.
- **List parsing (D15)** — the one genuine difference from the parent override, which returns 0..1.
  Split on comma or semicolon, trim, resolve each entry, skip entries that resolve to nothing while
  keeping the rest. Entries are references in the connector's own form — Jira keys on Jira, work item
  ids on ADO, identifiers on Linear — which is `ReferenceId` space, so no normalisation layer is owed
  beyond Linear's lower-casing. Unit-testable in isolation, and where most of this slice's tests live.
- **Replace, not union**: while the override is set the native link is not read at all for that
  Portfolio.
- The selector in Portfolio settings, offered only for additional fields defined on that Portfolio's
  connection, with the same permission the parent override requires.
- `FetchFingerprint` gains the new setting, so changing it triggers a refetch exactly as changing the
  parent override does (`FetchFingerprint.cs:38`, `:81`). Missing this means the setting appears to do
  nothing until an unrelated change forces a refresh.
- Free on every instance (D9) — it feeds detection, and detection is free.

### US-10 — Ignore dependencies (D16)

- `IgnoreDependencies` on `Portfolio`, non-null, default false, in the same additive migration.
- **A field of the honour policy's input**, exactly where the premium licence already sits (SA-14,
  SA-17). Not a branch around ingestion, not a check in the forecast: Epic #5792 consumes the honoured
  set and never learns this setting exists. Edges keep being read, stored and shown; they are simply
  never honoured.
- `IgnoredByPortfolio`, a fifth member of `NotHonouredReason`, which **takes precedence** over the
  three data reasons when more than one applies. `NotLicensed` stays outermost — it describes the
  instance, not the plan — and is unreachable in this epic anyway (D9). Cycle detection keeps running regardless — the verdict a Feature carries the
  moment the switch goes back off must be the one it would have had all along.
- **No warning** while it is on. A deliberate choice is not a broken link, and a warning on every
  Feature in the Portfolio teaches the reader to stop reading the column slice 02 exists to fill.
- The many-to-many rule: an edge is un-honoured by the switch only when **every** Portfolio containing
  both of its ends has it set (D16). Otherwise one Portfolio's what-if would rewrite another's plan.
- The switch beside the dependency-field selector, same permission, effective on the next read — no
  refresh, no re-download.
- **`FetchFingerprint` deliberately does NOT learn this one.** Its two siblings on this form belong
  there, so leaving it out looks like the bug; it is the opposite. Nothing about what is fetched
  depends on it, and registering it would re-download the whole Portfolio on every toggle of a
  what-if. Asserted directly (AC-10.6).

## OUT of scope

- Any per-Feature authoring of a dependency inside Lighthouse. Rejected under D4 and not in this epic
  in any form.
- A configurable separator (D15 fixes comma and semicolon).
- The same override on a Team. Features are fetched per Portfolio, so the Team owner has no consumer.
- A global or per-Feature "ignore dependencies". A global one has no owner and no place to be seen
  from; a per-Feature one is a suppression, which is Lighthouse authoring dependency data by the back
  door (D4).
- Any forecast consequence of ignoring. There is none to have in this epic; the setting starts paying
  the day Epic #5792 ships, and needs nothing there.
- Jira and Linear override support beyond what falls out of the shared port — their standard links
  land in slice 03, and the override is connector-agnostic by construction.

## Learning hypothesis

**Disproves** "a hand-maintained field yields resolvable references" **if** what people actually put in
such a column is not the tracker's reference form. The intended contract is settled (D15: Jira keys,
ADO ids, Linear identifiers), but `ParentReferenceId` holds one canonical id because a *connector*
wrote it, whereas this column is typed by a person — so full URLs, prose, and titles instead of keys
are all plausible in the same column across one Portfolio.

If it fails, the slice needs a normalisation step — parse a URL down to its id, strip a prefix — which
is a second slice, not a bigger version of this one.

**Confirms**, if it holds, that a third override of this shape is a copy of a known pattern.

**US-10's own hypothesis** — **disproves** "ignoring is a read-time verdict" **if** any consumer turns
out to need the edge set itself to be empty rather than un-honoured. If it fails, the setting has to
move into ingestion after all, and everything the delta rejected on 2026-08-18 (deleting stored edges,
a fingerprint entry, a re-download per toggle, a column that reads the same as an instance with no
dependencies at all) comes back with it. Cheapest place to find out is here, one epic before anything
honours a dependency.

## Why this slice exists (and what it is NOT for)

It serves ADO, Jira and Linear instances that record dependencies in a custom field rather than the
tracker's native link type. **It does not bring ServiceNow or CSV into the feature**, however tempting
that argument is: every connector supports additional fields, so the mechanism looks available to
them, but `ServiceNowWorkTrackingConnector.GetFeaturesForProject` throws `NotSupportedException`
(`:751-757`) — ServiceNow has no Features, so there is nothing for a dependency to be between. This
was checked during DISCUSS and is written down so the argument is not re-made mid-slice.

## Verify the premise first (30 min, before the migration)

Ask for one real example of such a field from an instance that uses one, and read what is actually in
it. This is the only slice in the epic serving a population the dogfood instance does not contain, so
the premise cannot be checked against `:5169` — it has to be checked against a real user's data or it
is not checked at all. If no example is available, say so in the verdict below rather than proceeding
as though it were confirmed.

## Acceptance criteria

AC-4.1 … AC-4.7 verbatim from `feature-delta.md`. The three that carry the slice:

- A field reading `1234;5678` yields two edges that appear in the count and the dialog (AC-4.1).
- With the override set, the connector performs **no** relation fetch (AC-4.2).
- With the override unset, behaviour is byte-identical to slices 01-03 (AC-4.5).

AC-10.1 … AC-10.8 verbatim from `feature-delta.md`. The three that carry US-10:

- With the switch on, the count and the dialog are identical to the switch being off, and every entry
  reads *ignored for this Portfolio* (AC-10.1).
- The switch takes effect on the next read; nothing is deleted, nothing re-downloaded, and toggling
  back restores every verdict (AC-10.3, AC-10.4).
- An edge whose ends share a Portfolio that has **not** set the switch keeps the verdict it had
  (AC-10.5).

## Dependencies

Slices 01-03 — this slice changes where edges come from and nothing about what happens to them.
One additional field defined on the dogfood ADO connection carrying a dependency list, created by hand
for the manual confirmation.

## Dogfood moment

Same day: on `:5169`, define an additional field on the ADO connection, put a dependency list in it for
two Features, point one Portfolio at it, refresh, and confirm the column matches what the standard
links produced for the same pair. That comparison is the strongest evidence available without a real
user's instance.

US-10 has no such weakness — it is dogfoodable directly. `:5169` already carries 11 real relations over
7 Features, including the epic split itself (#5792 → #4365). Turn the switch on, confirm all 11 stay
visible and every one reads as ignored, confirm the warnings column goes quiet, turn it off and confirm
every verdict comes back. No fixture, no hand-made field.

## Commit gate

Normal — the approval gate is Epic #5792's only (maintainer, 2026-08-16).

## Learning hypothesis verdict

**US-04 — not run.** No real example of such a field was available, and this is the one population the
dogfood instance does not contain, so the premise stands unchecked rather than confirmed. What shipped
assumes references in the connector's own form; if what people actually type is URLs or titles, the
normalisation step the hypothesis names is still owed and is still a slice of its own.

**US-10 — held.** Ignoring is a read-time verdict, and no consumer wanted the edge set itself to be
empty. `IDependencyHonourPolicy` gained one field of input and nothing else moved: the connectors, the
reconciler and the stored references are untouched by the switch, which is what the acceptance
scenarios assert by comparing the stored set byte for byte either side of it.

## Two corrections to this brief, settled during implementation

Both are recorded in `feature-delta.md` as F-3 and F-4 and are followed there rather than here.

- **The two settings sit on `Portfolio`, not on `IWorkItemQueryOwner`** (F-3). This brief's IN-scope
  list still says the interface. A Team would carry both as dead surface it has no consumer for, which
  is the argument `FetchFingerprint` already makes for the two portfolio-only references beside them.

- **The relations request is skipped only when BOTH overrides are set** (F-4), not when the dependency
  override alone is. Azure DevOps carries the parent link and the dependency links in one payload, so
  the "skip the relation fetch entirely" line above, taken literally, would lose the whole parent
  hierarchy for any Portfolio that reads its dependencies from a field but its parent from the tracker.
  AC-4.2 is met in the configuration it describes: both overrides set, no relations request.

Two lines in `acceptance/milestone-4` were corrected to match, both of which contradicted the delta
rather than the code: an unresolvable entry is **skipped** (AC-4.4) rather than listed as unresolved,
and the no-relations assertion now names the configuration it holds in.

### `NotLicensed` is still absent, and that is not an oversight

A review of this slice raised its absence as a blocker, reading the delta's component row, which names
five reason values. It is the third time the same row has been read that way, so it is written down
here as well as in slice 02's brief.

The reason set is closed at **four** values. The licence half of this feature left with Epic #5792 at
the split, and nothing in this epic may ask a licence question at all — so a fifth value would be one
no code path can produce, with no wording and no warning behaviour decided for it, in an enum whose
whole point is that widening it is somebody's deliberate decision. AC-10.8 says `NotLicensed` stays
outermost; that is the ordering Epic #5792 inherits when it adds the value and turns the flag on, not
a value this slice owes.

## The tracker's cycle guard does not come with the override

Azure DevOps refuses to store a dependency cycle — `TF201035`, transitively, measured 2026-08-18 (see
the slice-02 brief). Every edge this epic reads from ADO relations therefore arrives pre-validated, and
it is tempting to read that as "loops are somebody else's problem".

**This slice is where that stops being true.** The override field is free text a Portfolio owner types:
comma-separated references, no tracker validation of any kind, so `#A` can name `#B` while `#B` names
`#A`, or a Feature can name itself. Jira has no guard either (`blocks` links close loops happily), and a
reference resolving across two Portfolios can close a loop neither tracker sees whole.

So the detection written in slice 02 is not belt-and-braces for ADO — it is the *only* guard on this
path, and the one that keeps `while (GetRemainingItems() > 0)` terminating once Epic #5792 consumes the
verdict. Two consequences for this slice:

- The override's parse must feed the same `IDependencyHonourPolicy` as tracker links, not a shortcut
  around it. KPI-5's one-decision rule is what makes that automatic.
- A self-reference typed into the field must survive to the warning, which is why the dedup key is
  `(FeatureId, ReferenceId)` rather than "targets other than me".

Worth an explicit AC here: a loop typed into the override field warns on every member, and the
dogfood evidence AC-3.3 could not get from ADO lands on this path instead.
