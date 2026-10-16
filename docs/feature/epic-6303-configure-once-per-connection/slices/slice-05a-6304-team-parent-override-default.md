# Slice 05a — Story 6304 — The Teams' Parent Override Field is set once; saving it says how many follow

**ADO**: Story #6304 (Epic #6303) · **User story**: US-04 (first half) · **Estimate**: ~1 day · **Depends on**: finished #6312 (order rule, D19)
**Release point**: no — #6304 releases after slice 06. Alone, this slice leaves `main` releasable: no default
exists until an admin sets one, so the upgrade changes nothing except repairing values that pointed at a deleted
field (AC-4.4) (L8). Between 05a and 05b an older client's round trip could still turn a follower into *Own*, but only once an admin has set a default; 05b closes it, and no release is cut between the two slices.
**Locked decisions**: D7, D8, D9, D10 (as amended), D11, D12 (as amended), D14, D15, D21, D22

## Goal
A System Admin sets "Parent Override Field for Teams" on the connection. A Team in *Connection default* mode reads
it as its own; a Team with its own value — including a deliberate None — is unaffected. Saving the default shows, in
the save dialog, how many Teams follow it, and the log entry names them.

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
- **The save dialog (plain variant: Cancel / Save)** counts the followers of each changed default, from the
  connection list read; no dialog when nobody follows; one Information log entry per save names them
  (AC-F.1 – AC-F.4). After the save the page navigates to the Overview as today.
- Team Advanced section: the Parent Override dropdown's first entry "Connection default (…)" with helper text
  "Set on the <tracker> connection." (`AdvancedInputs.tsx:28-52`, UI-2); the followed display name travels on the
  Team settings DTO.
- **Until slice 05b**, deleting an Additional Field a default references behaves as deleting an owner-referenced
  field does today (P5, DESIGN states what that is); the refusal arrives in 05b.

## OUT
- D17's delete refusal and the older-payload PUT rule with its round trip (slice 05b, G-R1 (b)).
- Immediate update on a default change and the dialog's update question (slice 06 — until then the next scheduled
  cycle picks it up through the fingerprint). Portfolio grain (Story #6313, slice 08). Slice 07's follow list and
  bulk follow are dropped (UI-4).

## Learning hypothesis
Disproves "rendering the effective value keeps every stored fingerprint stable across the upgrade" if any existing
Team's fingerprint changes in the upgrade test (AC-4.4), other than a Team whose setting pointed at a deleted field —
that would mean a full refetch of every Team on release day.

## Production data / dogfood
Dev instance's Jira connection with its real Teams: before/after fingerprints equal; count Teams empty today (R2);
count the Teams whose Parent Override pointed at a deleted field before the upgrade; set a default, read the dialog's
count and the log entry, create one new Team and see it follow. Count the saves it took against the saves N Teams
would need (`OUT-6303-set-once`). Demo data for the E2E walking skeleton.

## UI
Approved by the maintainer (UI-1, UI-2, UI-3): defaults section, save dialog variant (plain), dropdown entry;
loading / could-not-load look from DESIGN.

## Acceptance criteria
AC-4.1 – AC-4.7 (AC-4.4 as amended); AC-F.1 – AC-F.4 as amended.

## Release notes (finalize)
"Teams whose Parent Override Field pointed at an Additional Field that had been deleted now read the tracker's own
value again, from their next update." (configurable terms)

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12): the after-save notice is replaced by the pre-save dialog count and one
  log entry; values pointing at a deleted field are repaired at the upgrade.
