# Slice 05a — Story 6304 — The Teams' Parent Override Field is set once; saving it names the followers in the log

**ADO**: Story #6304 (Epic #6303) · **User story**: US-04 (first half) · **Estimate**: ~1 day · **Depends on**: finished #6312 (order rule, D19)
**Release point**: no — #6304 releases after slice 06. Alone, this slice leaves `main` releasable: no default
exists until an admin sets one, so the upgrade changes nothing except repairing values that pointed at a deleted
field (AC-4.4) (L8). Between 05a and 05b an older client's round trip could still turn a follower into *Own*, but only once an admin has set a default; 05b closes it, and no release is cut between the two slices.
**Locked decisions**: D7, D8, D9, D10 (as amended), D11, D12 (as amended), D14, D15, D21, D22

## Goal
A System Admin sets "Parent Override Field for Teams" on the connection. A Team in *Connection default* mode reads
it as its own; a Team with its own value — including a deliberate None — is unaffected. Saving the default works
exactly as today, with no dialog, and the log entry names the Teams that follow it.

## IN
- Expand-only migration (`CreateMigration`, all providers): a nullable default FK on the connection; a mode column
  on Team, back-filled *Connection default* where the Parent Override is empty and *Own* where it is set (D10). A
  value naming a field the connection no longer has becomes *Connection default* and is cleared (D10 as amended).
  `null` keeps meaning None.
- One effective-value resolver used by **both** connectors, `FetchFingerprint.For` and
  `WorkItemRelatedSettingsChanged` (L3). With no default, an empty follower's effective value is `null`, so every
  stored fingerprint is byte-identical after the upgrade, except for the repaired deleted-field Teams.
- New Teams start on *Connection default* (D11).
- Connection edit page: a "Defaults for Teams and Portfolios" section below Additional Fields
  (`ModifyConnectionSettings.tsx:793-818`), with *Teams* / *Portfolios* sub-headings and one row per setting the
  tracker honours; hidden on Linear and ServiceNow; no follower count, no "Show who" (D21, UI-1, AC-4.7).
- **No save dialog for a changed default in this slice**: the save works exactly as today (a dialog opens only for
  an Additional Field change, as today). One Information log entry per save names the followers of each changed
  default, or says none follow (the log-entry part of AC-F.1 – AC-F.4). After the save the page navigates to the
  Overview as today.
- Team Advanced section: the Parent Override dropdown's first entry "Connection default (…)" with helper text
  "Set on the <tracker> connection." (`AdvancedInputs.tsx:28-52`, UI-2); the followed display name travels on the
  Team settings DTO.
- **Until slice 05b**, deleting an Additional Field a default references behaves as deleting an owner-referenced
  field does today (P5, DESIGN states what that is); the refusal arrives in 05b.

## OUT
- D17's delete refusal and the older-payload PUT rule with its round trip (slice 05b, G-R1 (b)).
- The save dialog for a changed default — its follower count and update question — and the immediate update (slice
  06; until then the next scheduled cycle picks the change up through the fingerprint). The Estimation-only plain
  Save variant (slice 12). Portfolio grain (Story #6313, slice 08). Slice 07's follow list and
  bulk follow are dropped (UI-4).

## Learning hypothesis
Disproves "rendering the effective value keeps every stored fingerprint stable across the upgrade" if any existing
Team's fingerprint changes in the upgrade test (AC-4.4), other than a Team whose setting pointed at a deleted field —
that would mean a full refetch of every Team on release day.

## Production data / dogfood
Dev instance's Jira connection with its real Teams: before/after fingerprints equal; count Teams empty today (R2);
count the Teams whose Parent Override pointed at a deleted field before the upgrade; set a default, read the log
entry, create one new Team and see it follow. Count the saves it took against the saves N Teams
would need (`OUT-6303-set-once`). No E2E here: the walking skeleton lands with slice 12.

Dev database before the upgrade (2026-10-17, read-only count, step 05-02): 3 Teams. 3 name no Parent Override
Field and follow with no value change; 0 name a deleted field; 0 name another connection's field; 0 own a valid
field. The repair clears no value, so no Team's fingerprint moves on this instance.

**Dogfood result (2026-10-17, slice end).** Run on a copy of the dev database, its key ring and a separate port, so
the maintainer's workspace was not touched. The instance has 3 Teams on 3 connections (Jira, two Azure DevOps).
- The upgrade applied on start: all 3 Teams follow, no value was cleared, and the 3 stored fingerprints are
  byte-equal before and after.
- One save on the Jira connection set the Teams' default to "is caused by". The log carried exactly one entry:
  "Connection letpeoplework.atlassian.net: the Teams' default Parent Override Field changed from none to is caused
  by; 1 Teams follow it: Demo Dependencies Team". Saves needed: 1, against 1 per following Team before
  (`OUT-6303-set-once`; a single follower on this instance, so the saving shows only with more Teams).
- The follower's settings read the default's field, source `ConnectionDefault`, name "is caused by", tracker Jira.
- A new Team created with no Parent Override Field follows the default from the start.
- **Found:** a new Team created from a copy of a follower's settings, which carries the effective field id but no
  source, becomes *Own* with that field, although the value equals the default. Under the round-trip rule (a value
  that differs from the effective one makes the owner *Own*, an equal one changes nothing) it should follow. Slice
  05b applies that rule to the Team PUT; it must apply it to creation too.
- **Copy nit:** "1 Teams follow it" should read "1 Team follows it".

## UI
Approved by the maintainer (UI-1, UI-2): defaults section, dropdown entry; loading / could-not-load look from
DESIGN. No dialog for a changed default: the approved UI-3 dialog arrives with slice 06, so no unapproved dialog copy
is built.

## Acceptance criteria
AC-4.1 – AC-4.7 (AC-4.4 as amended); the log-entry part of AC-F.1 – AC-F.4 as amended (no dialog in this slice).

## Release notes (finalize)
"Teams whose Parent Override Field pointed at an Additional Field that had been deleted now read the tracker's own
value again, from their next update." (configurable terms)

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12): the after-save notice is replaced by the pre-save dialog count and one
  log entry; values pointing at a deleted field are repaired at the upgrade.
- 2026-10-16 (review, maintainer decision): this slice ships no save dialog for a changed default; the UI-3 dialog,
  both variants, arrives with slice 06, and #6304 releases only after 06.
