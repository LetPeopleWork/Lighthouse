# Slice 05a — Story 6304 — The Teams' Parent Override Field is set once; saving it says who follows

**ADO**: Story #6304 (Epic #6303) · **User story**: US-04 (first half) · **Estimate**: ~1 day · **Depends on**: finished #6312 (order rule, D19)
**Release point**: no — #6304 releases after slice 07. Alone, this slice leaves `main` releasable: no default
exists until an admin sets one, so the upgrade changes nothing (AC-4.4) (L8). Between 05a and 05b an older client's round trip could still turn a follower into *Own*, but only once an admin has set a default; 05b closes it, and no release is cut between the two slices.
**Locked decisions**: D7, D8, D9, D10, D11, D12, D14, D15, D21, D22

## Goal
A System Admin sets "Parent Override Field for Teams" on the connection. A Team in *Connection default* mode reads
it as its own; a Team with its own value — including a deliberate None — is unaffected. Saving the default shows
the save notice naming which Teams now follow it.

## IN
- Expand-only migration (`CreateMigration`, all providers): a nullable default FK on the connection; a mode column
  on Team, back-filled *Connection default* where the Parent Override is empty and *Own* where it is set (D10).
  `null` keeps meaning None.
- One effective-value resolver used by **both** connectors, `FetchFingerprint.For` and
  `WorkItemRelatedSettingsChanged` (L3). With no default, an empty follower's effective value is `null`, so every
  stored fingerprint is byte-identical after the upgrade.
- New Teams start on *Connection default* (D11).
- Connection edit page: a "Defaults for Teams and Portfolios" group (likely `InputGroup` + `FormSelectField`)
  after Additional Fields (`ModifyConnectionSettings.tsx:793-818`), shown only on connections whose tracker honours
  the setting — Jira and Azure DevOps for this one; hidden on CSV and Linear; ServiceNow per DESIGN (D21, AC-4.7).
- **The save notice (AC-F.1 – AC-F.4)**: names how many Teams now follow the default and which, including those
  that follow because they were empty at the upgrade. Informational; the save is already stored.
- Team Advanced section: the Parent Override dropdown's first entry "Connection default (…)"
  (`AdvancedInputs.tsx:28-52`); the followed display name travels on the Team settings DTO.
- **Until slice 05b**, deleting an Additional Field a default references behaves as deleting an owner-referenced
  field does today (P5, DESIGN states what that is); the refusal arrives in 05b.

## OUT
- D17's delete refusal and the older-payload PUT rule with its round trip (slice 05b, G-R1 (b)).
- Immediate update on a default change (slice 06 — until then the next scheduled cycle picks it up through the
  fingerprint). Owners with an equal own value following in one step (slice 07). Portfolio grain (Story #6313,
  slice 08).

## Learning hypothesis
Disproves "rendering the effective value keeps every stored fingerprint stable across the upgrade" if any existing
Team's fingerprint changes in the upgrade test (AC-4.4) — that would mean a full refetch of every Team on release
day.

## Production data / dogfood
Dev instance's Jira connection with its real Teams: before/after fingerprints equal; count Teams empty today (R2);
set a default, read the save notice, create one new Team and see it follow. Count the saves it took against the
saves N Teams would need (`OUT-6303-set-once`). Demo data for the E2E walking skeleton.

## UI
Sketch at the start of DISTILL (defaults group, save notice, dropdown entry); loading / could-not-load look from
DESIGN.

## Acceptance criteria
AC-4.1 – AC-4.7; AC-F.1 – AC-F.4.

## Changed assumptions
(filled during DELIVER)
