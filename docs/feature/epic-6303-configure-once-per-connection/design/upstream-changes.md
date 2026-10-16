# Upstream changes from DESIGN — epic-6303-configure-once-per-connection

**From**: DESIGN pass 2 (2026-10-16), the maintainer's decisions on P-5, P-12, P-14 and CA-6, and their UI decisions
UI-1 … UI-6 (`maintainer-decisions-2026-10-16.md`).
**To**: the product owner, who rewrites the DISCUSS text in `../feature-delta.md`, the slice briefs in `../slices/`,
the journey and `jobs.yaml`.
**Why**: three maintainer decisions change things DISCUSS had locked.

1. **No save notice (P-5).** After a connection save the page still navigates to `/` (6307's S3 stands). Instead:
   - before the save completes, the reused 6307 dialog shows how many Teams / Portfolios follow the changed defaults —
     and opens only when at least one owner follows a changed default (or an Additional Field changed, as today);
   - the save writes one Information log entry naming the connection, the changed defaults and their followers by name (Settings → System Info → Logs, `SystemAdmin` only);
   - the follower updates appear in Task Manager → Activity, as today.
2. **Values pointing at a deleted Additional Field are repaired at the upgrade (P-12).** They are treated as empty and move to *Connection default*.
3. **Slice 07 is dropped completely (UI-4).** No who-follows / who-differs list, no bulk follow, no follower count on
   the connection page. Story #6304 is slices 05a, 05b and 06. Owners hand-set to the default's value stay *Own*; an
   admin switches each one on its own settings page.

Smaller changes:

4. **CSV Feature Owner default (P-14).** The Feature Owner default is offered on CSV.
5. **ADO Related (CA-6).** D2 stands on Azure DevOps, so the Related examples must change.
6. **The refusal names owners that use the field as their own (UI-6).**

DESIGN rationale is in `../feature-delta.md` → "Wave: DESIGN / [REF] Changed Assumptions" (CA-4, CA-6, CA-10 … CA-13).

Each entry gives the current text, the replacement wording and where it lives. Copy stays open for the DISTILL sketch review.

---

## The save notice → pre-save dialog + log entry (P-5)

**U-1 — D12** (Locked Decisions)
- Now: "Saving a default shows a save notice naming who now follows it (maintainer requirement). Informational, never a confirmation. AC-F below; every default save, first set or changed, for every setting."
- Replace with: "**D12 — A default save says, before it completes, how many owners follow the changed default; the save records them by name in the log.** When at least one Team or Portfolio follows a default that this save sets or changes, the connection's save dialog (Story 6307's) opens and says how many Teams and Portfolios follow it; names are not shown there. When nobody follows, no dialog appears and the save works as today. After the save the page navigates to the Overview as it does today, with no notice. Every save that sets or changes a default writes one log entry naming the followers (or saying none follow), readable by a System Admin in Settings → System Info. The updates it starts appear in Task Manager → Activity. AC-F below. (Amended at DESIGN pass 2, maintainer 2026-10-16.)"

**U-2 — AC-F.1**
- Replace with: "When a System Admin saves a connection on which a default was set for the first time or changed, and at least one owner follows it, the save dialog shows, before the save completes, how many Teams and Portfolios follow it (for example 'Update 3 Teams and 0 Portfolios?'). Every owner in *Connection default* mode counts, including those that follow because their setting was empty at the upgrade. The same save writes one log entry naming them."

**U-3 — AC-F.2**
- Replace with: "The dialog never asks to confirm the default itself. Cancel leaves the connection unsaved, as it does today. Whether to start updates is asked only when the save would start any (AC-F.5)."

**U-4 — AC-F.3**
- Replace with: "When no owner follows the changed default, no dialog appears and the save works as today; the log entry says none follow. When no default changed, no log entry is written, and the dialog appears only if an Additional Field changed, as today."

**U-5 — AC-F.4**
- Replace with: "Counts in the dialog and names in the log entry use the configurable Team / Portfolio terms. One dialog and one log entry per save, covering every default that changed in it."

**U-6 — AC-F.5, AC-5.3 and US-05**
- AC-F.5 becomes: "From slice 06 on, when a changed default is read at fetch time and has followers, the dialog asks Save only or Save and update, with the follower count. That is the same question Story 6307 asks for a field change, asked once. When only an Estimation default with followers changed, nothing needs updating and the dialog offers only Save ('Change the Estimation default?'). Before slice 06 a dialog for a changed default always offers only Save."
- AC-5.3 becomes: "Before the save, the dialog says how many followers will update — 9 in the first scenario, none in the second. After *Save only* none update, and they pick the change up at their next scheduled update."
- US-05 Elevator Pitch "After": "save the connection with 'Parent Link' as the new default → the dialog says 9 Teams follow it and will update; choose Save and update → 9 rows in Task Manager → Activity."
- US-05 scenario "Only the followers are updated": change its last step to "And the save dialog said 9 Teams follow the default".
- US-05 scenario "Nobody follows": change its last step to "Then no dialog appears, the save completes as today, no update starts, and the log entry says no Team follows the default".
- US-05 scenario "Saving without updating": change its last step to "Then no update starts and the log entry still names the followers".

**U-9 — US-04 (Story #6304)**
- Elevator Pitch "After": "… set **Parent Override Field for Teams** to 'Caused by', save → the dialog says 3 Teams follow it; save → Team settings → Advanced on Fraud Signals shows 'Connection default (Caused by)', and Settings → System Info → Logs names Fraud Signals, Ops Tooling and Mobile Wallet." The "Decision enabled" line is unchanged.
- Domain example 1: "… → follows; counted in the dialog and named in the log entry."
- Domain example 3: "… → follows and is named in the log entry; its admin picks **None** → *Own* + empty."
- UAT "Saving the default says who now follows it": its last two steps become "Then the save dialog says 3 Teams follow 'Caused by' / And the log entry names Fraud Signals, Ops Tooling and Mobile Wallet and not Ledger".
- Acceptance criteria header: "(05a: AC-4.1 – 4.7 and AC-F.1 – F.4 …)" stays; the AC-F text changes as above.

**U-10 — US-07 … US-11 (Story #6313)**
- AC-7.1: "Saving the Portfolios' default shows, before the save completes, how many Portfolios follow it, and the log entry names them."
- US-07 scenario "Saving the Portfolios' default names the Portfolios that follow it": change its last step to "Then the save dialog counts 'Merchant Onboarding' among the followers and the log entry names it, not 'Card Issuing'".
- AC-8.4, AC-9.4, AC-10.4 (first sentence): "The save dialog counts the followers and the log entry names them (AC-F.1 – F.5)."
- AC-11.4: "The save dialog counts the followers and says no update is needed (plain Save); the log entry names them."
- US-07 … US-10 Elevator Pitches: replace "sees the save notice naming …" with "the dialog says how many follow it".
- US-11's Elevator Pitch: replace "sees the save notice naming the followers and that no update is needed" with "the dialog says how many follow it and that nothing needs updating".

**U-11 — KPI `OUT-6303-no-surprise`**
- Target: "100% of default saves with followers show the follower count before the save completes; 100% of default saves write the log entry naming the followers (or saying none follow)".
- Measured by: "AC-F test coverage; dogfood".
- Baseline: "no count, no entry".

**U-12 — R2 (Risks)**
- "The save notice is the mitigation" becomes "The save dialog's follower count and the log entry naming the followers are the mitigation".

**U-15 — Journey** `docs/product/journeys/epic-6303-configure-once-per-connection.yaml`
- `set-it-once-on-the-connection` → step 1 `sees`: "before saving, the dialog counts the Teams / Portfolios that follow each changed default (including those that follow because their setting was empty at the upgrade) and, from slice 06, asks whether to update them now; after saving, Settings → System Info → Logs names them, and their updates appear in Task Manager → Activity".
- `emotional_arc.middle`: "Informed - 'before I saved, it told me 3 Teams follow it; the log names them, including three that had nothing set.'"
- `error_paths` "deliberately chosen None": "… the log entry names it, so the admin can set it back to None".
- Shared artifact "followers of a default", consumed by: "the save dialog's counts, the log entry, the connection list read (counts only, never shown on the page) and the update queue".

**U-16 — D22, DISCUSS checklist, Out of Scope, Still Open**
- D22: "05a: the mechanism end to end for the Teams' Parent Override Field, with the follower count in the save dialog and the log entry."
- DISCUSS checklist, UI sketches: "the save notice (with the 6307 updating count folded in)" becomes "the save dialog's variants — with and without the update question — and the log entry's wording".
- Out of Scope row "Distinguishing, in the save notice, …": replace "save notice" with "dialog or log entry".
- Still Open Q1: replace "save notice" with "log entry".

---

## Values pointing at a deleted Additional Field (P-12)

**U-7 — D10**
- Now: "At the upgrade, empty becomes *Connection default*; a value stays *Own* (OD-12, changed). No default exists on release day, so nothing changes then. Accepted consequence: an owner that was deliberately None follows a default once one is set. D12 makes that visible."
- Replace with: "**D10 — At the upgrade, empty becomes *Connection default*; a value naming one of the connection's Additional Fields stays *Own*; a value naming a field the connection no longer has is treated as empty and becomes *Connection default* (maintainer, DESIGN pass 2).**
  - No default exists on release day. Owners with an empty value read exactly what they read before.
  - Owners whose setting pointed at a deleted field go back to the tracker's own value: its own parent, its default dependency link, or no size / owner / estimate. That is a deliberate fix, called out in the release notes. Each of them downloads everything once, at its next scheduled update; no update is started by the upgrade itself.
  - Accepted consequence: an owner that was deliberately None follows a default once one is set. D12 makes that visible."

**U-8 — AC-4.4 and its UAT**
- AC-4.4 becomes: "After the upgrade, Teams that had no Parent Override Field follow the connection default, which is empty on release day, so nothing changes for them. Teams that had one own it. No Team reads anything different or downloads everything again because of the upgrade, except Teams whose Parent Override Field pointed at an Additional Field that had been deleted: those follow the default too, read the tracker's own parent again, and download everything once at their next scheduled update."
- UAT "The upgrade changes nothing on its own" becomes:
  ```gherkin
  Scenario: The upgrade changes nothing on its own, except settings that pointed at nothing
    Given no connection has a default
    And Ops Tooling's Parent Override Field pointed at an Additional Field that had been deleted
    When the release is installed
    Then every other Team reads exactly what it read before, and its next update is not a full download caused by the upgrade
    And Ops Tooling follows the connection default, reads its parents from the tracker's own parent again, and its next scheduled update downloads everything
  ```
- The same holds at Portfolio grain for C1–C5 in slices 08–12: each slice's AC list gains "AC-4.4 at Portfolio grain, for this setting".

---

## Smaller changes

**U-13 — ADO Related is non-directional (CA-6, D2 kept)**
- US-03 domain example 2: replace "`System.LinkTypes.Related-Forward` named by a Services team that records waits as Related → read from those" with "`System.LinkTypes.Related` named by a Services team that records waits as Related → no dependencies, and the warning says Related has no direction, so it cannot say who waits on whom".
- Add example 2b: "`System.LinkTypes.Dependency-Forward` (Successor) named → each Feature waits on its successors, the reverse of the default."
- Slice 04 → Production data / dogfood: replace "`System.LinkTypes.Related-Forward` will do as a test vehicle" with "a directional relation type that slice 03's spike recorded as present on the dev organisation (`System.LinkTypes.Dependency-Forward` at the least)".
- Slice 03 → Production data / dogfood: add "Record which directional relation types the organisation lists; slice 04's test vehicle is chosen from them."

**U-17 — CSV Feature Owner default (P-14, D21 table)**
- D21 table, row CSV, column C4: "✓ — the extrapolation reads the Feature Owner Field from the Additional Field values CSV fills (`WorkItemService.cs:756-772`); the owning Team shown on a Feature stays Jira / ADO only".
- The sentence under the table becomes: "five settings on Jira and Azure DevOps, Feature Owner and Estimation on CSV, nothing on Linear or ServiceNow (the group is hidden)".
- ServiceNow row: "N/A — no Portfolio fetch, no Additional Field values read (`ServiceNowWorkTrackingConnector.cs:758-775`)".
- US-10 technical notes and slice 11 IN: "Shown on Jira, Azure DevOps and CSV connections."

---

## Slice 07 dropped (UI-4)

**U-18 — US-06 and everything that carries slice 07**
- US-06 ("See who follows and who differs") and AC-6.1 – AC-6.6: mark **DROPPED (maintainer, 2026-10-16, UI-4)**, keep the text struck through for the record. Add one line: "An owner hand-set to the default's value stays *Own*; its admin switches it to *Connection default* on its own settings page."
- `slices/slice-07-6304-matching-owners-follow-the-default.md`: status line "DROPPED (maintainer, 2026-10-16, UI-4) — not delivered"; keep the brief for the record.
- D14: drop the G-07 sentence ("Slice 07's bulk follow needs `SystemAdmin` only …"). G-07 in the gates list: "moot — slice 07 dropped".
- DISCUSS Driving Ports: remove the row "Move matching owners to follow".
- Story map: row 07 struck, "dropped (UI-4)".
- Scope Assessment: `#6304 | US-04, US-05 | 05a, 05b, 06 | ~2d`.
- Release points: "#6304 after 06" (06 becomes #6304's release point). Remove "and the bulk follow in 07".
- Prioritisation item 3: "#6304 (05a, 05b, 06)"; drop the sentence on why 07 belongs ("on hand-configured installations the default does nothing for existing Teams …") and replace it with "On a hand-configured installation the default reaches existing owners only when their admins switch them to *Connection default*, one by one."
- Carpaccio / learning-hypothesis rows for 07: struck.
- Slice 06 brief "Release point": "#6304 is releasable after this slice."
- Slices 08 – 12 "Depends on": "finished #6304 (05a – 06)" for "(05a – 07)".
- #6313 intro: drop the reference to "the US-06 list" (AC-6.5).
- Journey `epic-6303-configure-once-per-connection.yaml`: remove step 4 (bring matching owners along) and the error path "another admin saved one of the owners being moved"; the shared artifact "the connection list" loses "who follows / who differs" and keeps "follower counts for the save dialog".
- Persona line for `config-admin` ("reads who follows …") and the job story at the top ("I want to see who follows the connection's setting and who carries its own …"): reword to "knows, before saving, how many follow; finds their names in the log".
- DISCUSS checklist P4 ("loading and could-not-load looks for … the slice 07 list") and the UI-sketch item ("slice 07 list (`DataGridBase` or a plain list)"): drop the slice 07 part.
- DoR rows 3 and 4 ("US-06 3"): drop US-06.

**U-19 — KPI `OUT-6303-followers-share` (product question, open)**
- Now: "≥ 80% after slice 07 on a hand-configured connection". Slice 07 was the lever; without it, hand-configured owners move only one by one.
- Product owner to choose: restate the target (for example "≥ 80% of owners **created after** a default was set follow it", which D11 delivers on its own) or drop the KPI. DESIGN does not decide this.

**U-20 — `docs/product/jobs.yaml`**
- `job-config-admin-see-which-owners-differ-from-the-connection`: mark `status: deferred` with the reason "slice 07 dropped by the maintainer (UI-4), 2026-10-16; no story serves it". Do not delete it; the job is still real. Its two other mentions follow: `docs/product/personas/config-admin.yaml:44` (annotate "deferred, slice 07 dropped") and the journey's job list (`epic-6303-configure-once-per-connection.yaml:78`, remove). `jobs.yaml` is over 512 KB and was not searched here; the product owner confirms the entry exists before editing.

**U-21 — The refusal names owners that use the field as their own (UI-6)**
- AC-4.8, US-04 domain example 5 and the UAT for removing a field in use: "naming the default and the Teams that follow it" becomes "naming the default and the Teams that use it as their own".
- Copy (approved): "✕ "Caused by" cannot be removed. It is the Teams' Parent Override Field default, and Ledger uses it as its own. Nothing was saved."

**Other UI decisions to carry into the DISCUSS text** (UI-1, UI-2, UI-5)
- The connection page section is titled "Defaults for Teams and Portfolios", below Additional Fields, with Teams / Portfolios sub-headings, one row per supported setting, hidden on Linear and ServiceNow; no follower count, no "Show who".
- The owner dropdown's first entry reads "Connection default (<value>)" with helper text "Set on the <tracker> connection."; picking any other entry, None included, makes the value *Own*.
- Estimation defaults (slice 12) reuse the owner's Estimation editor inline, collapsed until a field is chosen, once for Teams and once for Portfolios; the owner page shows "Connection default (Story Points, Points)".

---

## Slice briefs

| Brief | Change |
|---|---|
| `slice-04-…` | U-13 dogfood vehicle |
| `slice-03-…` | U-13 record the directional types |
| `slice-05a-…` | Goal: replace "Saving the default shows the save notice naming which Teams now follow it" with "Saving the default shows, in the save dialog, how many Teams follow it, and the log entry names them". IN: replace the "save notice" bullet with "The save dialog (plain variant: Cancel / Save) counts the followers of each changed default, from the connection list read; one Information log entry per save names them". Migration bullet: add "a value naming a field the connection no longer has becomes *Connection default* and is cleared (D10 as amended)". Learning hypothesis: "… if any existing Team's fingerprint changes in the upgrade test, other than a Team whose setting pointed at a deleted field". Dogfood: add "count the Teams whose Parent Override pointed at a deleted field before the upgrade". UI: "save dialog variant" for "save notice". AC: AC-4.1 – AC-4.7 (AC-4.4 as amended); AC-F.1 – AC-F.4 as amended |
| `slice-05b-…` | Why: "Deleting the field it names leaves followers reading nothing" stands; add "the upgrade already repaired values that pointed at nothing (05a)" |
| `slice-06-…` | Goal: replace "and slice 05a's save notice says how many of them are updating (AC-F.5)" with "and the save dialog asks whether to update the followers now, saying how many (AC-F.5)". IN: replace "The updating count folded into the one save notice — never a second message" with "The dialog's update-question variant for a changed fetch-time default with followers; the plain variant for an Estimation-only change with followers; no dialog when nobody follows". Replace "the save notice still names the followers" with "the log entry still names the followers". Release point: #6304 is releasable after this slice (U-18) |
| `slice-07-…` | **DROPPED** (UI-4) — status line only, see U-18 |
| `slice-08-…` … `slice-11-…` | Goal and AC lines: "save notice" becomes "save dialog count and log entry". Migration bullet: add "D10 as amended (a value pointing at a deleted field becomes *Connection default*)". AC: add "AC-4.4 at Portfolio grain". Depends on: "finished #6304 (05a – 06)". Slice 11 also gets U-17 (CSV) |
| `slice-12-…` | Goal: "When owners follow it, saving it shows, in the save dialog, how many follow it and that no update is needed (plain Save); the log entry names them." Migration bullet: D10 as amended. Shown on Jira, Azure DevOps and CSV. UI: the owner's Estimation editor reused inline per grain, collapsed until a field is chosen (UI-5). Depends on: "finished #6304 (05a – 06)" |

## Release notes (finalize)

For each slice whose migration repairs values that pointed at a deleted field (05a, 08–12), the release notes say, in the configurable terms: "Teams [Portfolios] whose Parent Override Field [Dependency Field, Size Estimate Field, Feature Owner Field, Estimation Field] pointed at an Additional Field that had been deleted now read the tracker's own value again, from their next update."
