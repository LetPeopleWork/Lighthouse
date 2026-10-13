# DISTILL upstream issues — epic-5878-baseline (2026-10-13)

Gaps DISTILL found in the DESIGN and DISCUSS decisions while writing the scenarios. None blocked the wave: each
one has a choice recorded here and pinned by a test, so DELIVER can build against it. Items marked **confirm** want
the maintainer's yes before the slice that first needs them; changing one means editing the named test and nothing
else.

## Contract

1. **The Reports list carries `thenRebuildable` per report** (DESIGN gap). DT1 puts a delete icon on every list row
   and DT7 warns "This Then window can no longer be rebuilt." when Then reaches back past the kept history, but
   DESIGN's list response (`{ id, name, templateKey, ownerKind, ownerId, createdOn }`) does not say so; only the
   single read does. The list row gains `thenRebuildable`, computed the same way as the read's. Pinned by
   `Slice08DeleteAndCommunityCapScenarios` and `ReportsTab.test.tsx`.
   **DESIGN answer (2026-10-13, DD24): adopted, at `thenAndNow.thenRebuildable` on the row** — the same path as in the
   read, by the rule that template-specific members live in the template's object. Both pinning tests move the path.
2. **The metric sides are named `then` and `now` on the wire, as DESIGN has them, and renamed in the browser.** An
   object literal with a `then` member is a Biome error (`lint/suspicious/noThenProperty`) and has the shape of a
   promise to anything that awaits it, so the frontend model calls the sides `atThen` / `atNow` and
   `ReportService` maps them on the way in. **Confirm**, or rename the wire members instead (for example
   `thenValues` / `nowValues`): the backend tests read them in one helper, `ReportsAcceptanceTest.ValueIn`.
   **DESIGN answer (2026-10-13, DD27): the wire members are renamed to `atThen` / `atNow`.** `ThenValueIn` /
   `NowValueIn` pass `"atThen"` / `"atNow"`; `ReportService` drops its mapping; the frontend model stays as it is.
3. **Member names inside a metric side, pinned by DISTILL** (DESIGN named the stored captures, not the read):
   `p85`, `p70`, `p50` ({Cycle Time}); `total`, `perDay` ({Throughput}); `average`, `min`, `max` ({WIP});
   `totalWindowAverage`, `totalLastDay`, `averageWindowAverage`, `averageLastDay` ({Work Item Age}); each one
   `{ value, sampleSize, absentReason }`. `beyondThenLimits` is `{ above, below }` of the same shape. Windows are
   `{ start, end, lengthDays }` with `yyyy-MM-dd` dates. A metric or member the report never captured is missing or
   null.
   **DESIGN answer (2026-10-13): adopted unchanged** into the read contract (feature-delta → Driving ports, ADR-221).
4. **Refusal codes, a closed set pinned by DISTILL**: `then-too-short`, `then-ends-in-the-future`,
   `then-before-cutoff`, `now-too-short`, `now-before-cutoff`, `name-missing`, `no-metric-shown`, `unknown-metric`,
   `metric-not-captured`, `template-not-supported`, `report-cap-reached`. Validation refusals are 400. The status of
   the cap refusal and of opening a report of a type this version cannot show are left open (any non-success with
   the code), as is the status of an edit that brings Then fields along — only that Then stays as it was is pinned.
   **DESIGN answer (2026-10-13, DD28, DD25): the set is adopted unchanged.** Cap: 403 (as every premium refusal).
   Opening a stored report of an unknown type: 422. An edit carrying Then members: 200, the members ignored, Then
   unchanged. All within what the tests leave open; no test changes.

## Frontend

5. **`CreateWizardShell` cannot host the report steps as it is** (DT3). Its props are the Create {Team} /
   {Portfolio} wizard's (connections, states). DELIVER either extracts the stepper chrome or builds the report wizard
   on the same MUI stepper look. The tests drive the wizard by its dialog, its Next / Back / Create / Save / Cancel
   buttons and its labelled fields, never by the component, so either way passes.
   **DESIGN answer (2026-10-13, DD30): extract** the stepper chrome into `WizardStepperFrame`; `CreateWizardShell`
   renders inside it unchanged and `ReportWizard` composes it.
6. **Where the owner-delete count comes from** (DISCUSS D42 names the count, DESIGN no source for the Overview). The
   Overview's Team / Portfolio delete dialog asks `ReportService.getReports(owner)` when it opens and appends
   "and its N reports" when N ≥ 1. Pinned by the `deleting a Team or Portfolio that has reports` block in
   `OverviewDashboard.test.tsx`. **Confirm**, or give the Team / Portfolio read a report count instead (then that
   block's "none" case changes its first assertion).
   **Closed by DESIGN DD23 (2026-10-13): confirmed** — the count is the owner's list endpoint; no count member on the
   Team / Portfolio read. While it loads Delete waits; if it fails the clause reads "and any reports it has"
   (feature-delta → Loading and failure looks; copy to confirm).
7. **The report's address and where it renders**: `/teams/:id/reports/:reportId` and
   `/portfolios/:id/reports/:reportId`, rendered by the Team / Portfolio page under its tab bar with the Reports
   tab selected. App routes gain the two addresses. Pinned by `TeamDetail.reportsTab.test.tsx` and
   `PortfolioDetail.reportsTab.test.tsx`.
   **DESIGN answer (2026-10-13): confirmed** (DD22).
8. **A report of a type this version cannot show has no designed look.** The backend refusal is covered
   (`template-not-supported`, never a 500); the browser shows whatever the generic failure look is. No frontend
   scenario pins it.
   **DESIGN answer (2026-10-13, DD32):** the read answers 422; the view shows the not-found family of looks with "This
   report was made by a newer version of Lighthouse and can't be shown here." and Back to Reports; the list's Type
   column reads "Not supported in this version". Copy to confirm in DELIVER's sketch; a frontend case can now pin it.

## Copy DISTILL had to propose (**confirm**)

DISCUSS and the DT walkthrough fixed most of the words; these are the ones a test needed and nobody had written:

| Where | Proposed |
|---|---|
| Value dash, `too-few-points` | "Then has too few points for limits" |
| Value dash, `no-process` | "Then never varied from 0" |
| Value dash, `no-wip` (the average {Work Item Age}; {WIP} itself is a real 0) | "No Work Items in progress" ({Work Items} is the Terminology token) |
| Change with a Then of 0 | "Then was 0", and no percentage |
| Wizard step ② future end | "Then cannot end after today." |
| Wizard step ② presets | One-click buttons "30 days" and "90 days" on a Team, "90 days" and "180 days" on a Portfolio, beside the custom number of days |
| Wizard step ③ refusals | "Now needs at least 14 days." and "The Now window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept." |
| Wizard field labels | "Then ends on", "Then length in days", "Now length in days", "Name" |
| Wizard buttons | Next, Back, Create, Save, Cancel |
| Not-found look, link | "Back to Reports" |
| Uncaptured value on the report view | "Not captured for this report" (DISCUSS's words), in place of the value |
| Reports list failure | Retry, and no sentence (DT6 gives the report view's sentence only). *DESIGN 2026-10-13 proposes a sentence beside Retry, "Reports couldn't be loaded. Reload to try again." — confirm one or the other* |

## Markers the tests read (not copy)

- `data-report-status="ready"` on the report view once the report is read; any other value while it loads or is
  read again. The E2E page object waits for it.
- `data-change="better" | "worse" | "none"` on each change; the sign and the arrow (▲ / ▼) sit in its text, so the
  colour is never the only carrier.
- Each panel is a `region` named by its title, such as "Cycle Time: Then & Now".
- The Type card in wizard step ① is a radio.
