# Feature Delta — quiet-deliberate-choices

Two small changes that share one job: Lighthouse keeps telling an administrator about a choice they
already made on purpose. The Overview's onboarding panel nags an installation that deliberately runs
without a Portfolio, and the ServiceNow connector logs a WARNING every sync about states the team
deliberately never mapped. Both are noise about intent, not about a fault.

- **Created**: 2026-08-03
- **Feature type**: user-facing (frontend UI + backend log surface)
- **Personas**: config-admin (single acting persona)
- **Density**: lean (Tier-1 [REF] only; no `ask-intelligent` trigger fired — 1 persona, 2 bounded
  contexts, no compliance terms, WS strategy is not D)

---

## Wave: DISCUSS / [REF] Persona ID

`config-admin` — the person who wired Lighthouse to the work tracking system and owns the Team and
Portfolio configuration. In this feature they are also the only person who sees either surface: the
onboarding panel renders behind `rbac.isSystemAdmin`, and the log is read by whoever runs the
instance.

No second persona is modelled. The team whose Throughput is affected by unmapped states never reads
the log and never sees the panel.

---

## Wave: DISCUSS / [REF] JTBD one-liner

**job-config-admin-dismiss-onboarding-guidance** — When my Lighthouse is set up the way I intend it,
including deliberately having no Portfolio, I want the setup guidance to go away for good, so I can
open the Overview and see my own data instead of being told to finish something I already decided
not to do.

**job-config-admin-quiet-deliberate-state-omission** — When I have deliberately left states like
*Canceled* out of a Team's state mapping, I want Lighthouse to drop that work silently, so every
WARNING left in my log is something I actually have to fix.

Both traced in `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Rationale |
|----|----------|-----------|
| D1 | The "Get Started" panel gets a close (✕) control that hides it **permanently for that browser**, via `localStorage`. | User decision, 2026-08-03. No server round-trip, no migration, no RBAC surface. Matches the dismissal pattern already shipped in `LighthouseVersion.tsx:45-84`. |
| D2 | Storage key is `lighthouse-hide-onboarding-stepper`, value `"true"`. | Reuses the `lighthouse-hide-*` naming already in `LighthouseVersion.tsx`, not the `lighthouse:datagrid:*` namespace used by `DataGridBase.tsx:55`. Two conventions exist; this one fits a boolean dismissal. |
| D3 | Dismissal is **not** recoverable from the UI. | The ask is "don't show it again". A "restore guidance" control would be a new setting to find and forget; clearing site data is the escape hatch, and the same content lives in the docs. |
| D4 | The portfolio step stays a required step of the stepper. | Considered and rejected with the user: making it optional would auto-hide the panel for portfolio-less setups without a click, but it also changes what the product tells a *new* admin the happy path is. D1 solves the reported annoyance without touching that. |
| D5 | `ReportStatesTheTeamNeverMapped` is **deleted**, not downgraded to Debug or Trace. | "No log whatsoever." A Debug line still shows up when someone raises the log level to chase a real problem, which is exactly when the noise costs most. |
| D6 | Only the unmapped-state warning goes. The other four ServiceNow warnings stay. | User decision, 2026-08-03. No query written, no kinds of work named, no transition history (`ReportKindsOfWorkNothingMeasuresStateOn`, `ReportHistoryUnavailable`), and instance unreachable are all misconfigurations or outages — not deliberate choices. |
| D7 | The dropped-record behaviour itself does not change. | Records in unmapped states are still left out (`ServiceNowWorkTrackingConnector.cs:296-299`). Only the report about it goes. |
| D8 | The comment claiming "DoD 5 forbids the silent no-op" at `ServiceNowWorkTrackingConnector.cs:302-305` goes with the method. | The premise was wrong for this case: `docs/concepts/worktrackingsystems/servicenow.md:206` already documents leaving *Canceled* out as normal operation. The log contradicted shipped guidance. |

---

## Wave: DISCUSS / [REF] Pre-requisites

None. Both changes sit on shipped code; no prior wave, no dependency, no feature flag, no migration.

Verified anchors:

- `Lighthouse.Frontend/src/components/Common/OnboardingStepper/OnboardingStepper.tsx` — the panel.
  Renders `null` at `activeStep === 3`; has no close affordance today.
- `Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.tsx:359-372` — the only call site, wrapped
  in `rbac.isSystemAdmin`.
- `Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowWorkTrackingConnector.cs:290`
  (call) and `:306-327` (`ReportStatesTheTeamNeverMapped`).
- `Lighthouse.Backend.Tests/.../ServiceNow/ServiceNowTeamSyncTest.cs:190-200` and `:729-741` — the two
  tests that assert the warning. `:170-184` (`WorkInAStateTheTeamNeverMapped_IsLeftOut`) asserts the
  drop itself and must stay green.

---

## Wave: DISCUSS / [REF] Driving ports

| Port | Surface | Change |
|------|---------|--------|
| Overview page (`/`) | React — "Get Started" panel | Adds a close icon button |
| Browser `localStorage` | `lighthouse-hide-onboarding-stepper` | New key |
| Backend log (`ILogger<ServiceNowWorkTrackingConnector>`) | Warning channel | One message removed |

No HTTP endpoint, no CLI, no MCP tool, no DTO, no database change.

---

## Wave: DISCUSS / [REF] User stories

### US-01 — Close the Get Started panel for good

As a Configuration Administrator whose installation deliberately has no Portfolio, I want to dismiss
the "Get Started" panel permanently, so the Overview shows my data instead of unfinishable setup
guidance.

`job_id: job-config-admin-dismiss-onboarding-guidance`

#### Elevator Pitch
Before: the "Get Started" panel sits at the top of the Overview on every single visit, and an
installation that runs Teams without a Portfolio can never complete it, so it never goes away.
After: click the ✕ in the top-right of the "Get Started" panel on the Overview → the panel vanishes
immediately and is absent on every later visit and reload in that browser.
Decision enabled: the administrator declares their setup finished as it is, and reclaims the top of
the Overview for the Teams and Portfolios they actually watch.

**Acceptance criteria**

- **AC-1.1** When the panel renders (system admin, onboarding incomplete), it shows a close control in
  its top-right corner with an accessible name (`aria-label`) that names the dismissal.
- **AC-1.2** Clicking it removes the panel from the Overview in the same render — no reload, no refetch.
- **AC-1.3** After the click, `localStorage.getItem("lighthouse-hide-onboarding-stepper") === "true"`.
- **AC-1.4** Reloading the Overview, or navigating away and back, with onboarding still incomplete →
  the panel does not render.
- **AC-1.5** With the key already `"true"` before first render, the panel never mounts — no flash of
  the panel followed by a removal.
- **AC-1.6** With the key absent, holding an unexpected value, or with `localStorage` throwing (private
  browsing, storage disabled), the Overview renders exactly as it does today and does not crash.
- **AC-1.7** The existing auto-hide still applies independently: with a connection, a Team and a
  Portfolio all present, the panel does not render whether or not the key is set.
- **AC-1.8** No network request is made by the dismissal, and the `rbac.isSystemAdmin` gate on the panel
  is unchanged.

### US-02 — Stop logging states the team never mapped

As a Configuration Administrator running a ServiceNow connection, I want Lighthouse to drop work in
states I deliberately left unmapped without saying anything, so every WARNING left in my log is one I
have to act on.

`job_id: job-config-admin-quiet-deliberate-state-omission`

#### Elevator Pitch
Before: every ServiceNow team sync writes a WARNING naming the states the team left unmapped — for
most instances that is *Canceled*, which the ServiceNow docs page already tells admins to leave out —
so the log reports intended configuration as a problem, on every sync, forever.
After: run a ServiceNow team sync holding records in an unmapped state → those records are left out of
the metrics exactly as they are today, and the log says nothing about it at any level.
Decision enabled: an administrator reading the log treats every remaining WARNING as actionable,
instead of first filtering out the one they already decided to ignore.

**Acceptance criteria**

- **AC-2.1** A sync where one or more records sit in a state the Team never mapped still returns only
  the records in mapped states — behaviour unchanged (`WorkInAStateTheTeamNeverMapped_IsLeftOut` stays
  green, untouched).
- **AC-2.2** That same sync produces **zero** log entries at any level — Warning, Information, Debug or
  Trace — naming the unmapped label or a left-out count.
- **AC-2.3** A record carrying no state at all is likewise left out and likewise unlogged.
- **AC-2.4** The other four ServiceNow warnings still fire, each proven by its existing test: no query
  written on the Team; no kinds of work named; no transition history (both
  `ReportKindsOfWorkNothingMeasuresStateOn` and `ReportHistoryUnavailable`); instance unreachable.
- **AC-2.5** `ReportStatesTheTeamNeverMapped` and its call site are deleted, along with the
  `MappedRecord.Label`-only plumbing and test helpers that no dead code may be left behind
  (`AWarningContaining` in `ServiceNowTeamSyncTest` loses both callers). `dotnet build` stays at zero
  warnings.
- **AC-2.6** No replacement surface appears anywhere else — no UI banner, no validation message, no
  API field. The omission stays silent, as documented.

---

## Wave: DISCUSS / [REF] Out of scope

- Making the Portfolio step optional, or otherwise changing what the stepper considers "done" (D4).
- Any way to bring the dismissed panel back from the UI (D3).
- Syncing the dismissal across browsers or users — it is a browser-local preference, and a second
  browser will show the panel again. Accepted.
- Silencing any other ServiceNow warning (D6), or the equivalent drop behaviour in the Linear
  connector, which never logged it in the first place.
- Changing which records are dropped, in either connector (D7).
- Surfacing unmapped states somewhere quieter (a validation hint on the Team settings page). Not asked
  for; if it ever is, it is a separate feature, not a consolation prize for this one.

---

## Wave: DISCUSS / [REF] WS strategy

**Strategy B — brownfield, no walking skeleton.** Both changes are edits to running code with an
existing test suite around them. There is no new mechanism, no new integration point, and nothing to
prove end to end that is not already proven. Phase 1.5 scope assessment: **PASS** — 2 stories, 2
bounded contexts, no new abstraction, well under one day each.

---

## Wave: DISCUSS / [REF] Slices

| # | Slice | Story | Est. | Learning hypothesis |
|---|-------|-------|------|---------------------|
| 01 | `slice-01-dismissible-onboarding-stepper` | US-01 | ~2h | Disproved if a browser-local flag cannot hide the panel without a flash, or if the E2E RBAC specs depend on the panel being present in a way a dismissal breaks. |
| 02 | `slice-02-quiet-unmapped-servicenow-states` | US-02 | ~1h | Disproved if some other surface — validation, a UI hint, another test — silently depends on this warning existing. |

Order: 02 then 01. Slice 02 is the smaller and the more likely to surface a hidden dependency; landing
it first means a surprise costs an hour, not a day. They are independent — either can ship alone.

Briefs: `docs/feature/quiet-deliberate-choices/slices/`.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | How measured |
|-----|--------|--------------|
| K1 — Unmapped-state warnings per ServiceNow sync cycle | 0 (from ≥1 per sync per team with any unmapped state) | Backend log of a ServiceNow-connected instance across one full sync interval; count entries naming a left-out count or an unmapped label. |
| K2 — Overview visits showing the panel after dismissal | 0 across ≥3 reloads and one navigate-away-and-back | Playwright walking skeleton on the Overview, plus manual check on Benjamin's dev instance. |
| K3 — ServiceNow warnings that remain actionable | 4 of 4 still fire | The existing backend tests for the four kept warnings stay green (AC-2.4). |

K3 is the guard rail: the value of K1 is only real if it does not come from turning the whole channel
off.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|------|----------|
| 1 | Business value articulated | Both jobs above; the reported annoyance is Benjamin's own on a portfolio-less setup and on the ServiceNow connection. |
| 2 | User stories with job traceability | US-01, US-02; both carry a real `job_id`, no `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 8 + 6 ACs, each observable from outside the code (rendered panel, `localStorage` value, log content, returned work items). |
| 4 | Dependencies identified | None. Verified: no ADR references the removed warning; no docs page promises it. |
| 5 | Technical feasibility confirmed | All five touch points read and quoted above with file:line. |
| 6 | Sized to fit a slice | ~2h and ~1h; scope assessment PASS. |
| 7 | Non-functional requirements | `localStorage` access must not throw the page (AC-1.6). Zero build warnings, zero new Sonar issues (AC-2.5). No performance dimension. |
| 8 | Out-of-scope explicit | Section above, 6 items, including the two alternatives considered and rejected. |
| 9 | Handoff target agreed | DESIGN can be skipped — no architectural decision is open, both slices are single-file-plus-tests edits. Recommend going straight to DELIVER per slice. |

---

## Wave: DISCUSS / [REF] Standing checklist

Answered explicitly, no silent N/A.

- **RBAC impact** — N/A, because the panel is already gated behind `rbac.isSystemAdmin` at
  `OverviewDashboard.tsx:359` and the dismissal adds no permission, no endpoint and no server state.
  The log removal has no authorization surface at all.
- **Lighthouse-Clients (CLI / MCP) versioning** — N/A, because no API contract, DTO or endpoint changes.
  Nothing in the clients can observe either change.
- **Website marketing surface** — N/A, because neither change is a capability a prospect would evaluate;
  removing a nag and removing a log line are both invisible on letpeople.work.
- **Docs prose** — one optional sentence. `docs/concepts/worktrackingsystems/servicenow.md:206` already
  says "Items in unmapped states are not tracked by Lighthouse and will not affect your metrics." That
  stays true. Adding "and Lighthouse does not warn about it" is a nicety; decide at finalization.
- **Screenshots** — expected N/A, because the demo data seeds a connection, Teams and a Portfolio, so
  the panel is already hidden at `activeStep === 3` in every screenshot run. Verify at DELIVER before
  claiming it.
- **Terminology** — the panel renders `teamTerm` / `portfolioTerm` / `connectionTerm` from Settings →
  Terminology already; the close control adds no new copy beyond its accessible name, which names the
  panel, not a domain concept.

---

## Wave: DISCUSS / [REF] Changed assumptions

`ServiceNowWorkTrackingConnector.cs:302-305` and `ServiceNowTeamSyncTest.cs:186-189` both assert, in
prose, that "DoD 5 forbids the silent no-op" and that "dropping records without a word reads as low
Throughput with the settings page still saying the team is valid".

That reasoning held when it was written — it was aimed at a flow coach *mistyping* a state label. It
does not hold for the dominant case in the field, which is a coach deliberately not mapping *Canceled*,
exactly as the ServiceNow docs page instructs. The originating DISCUSS documents under
`docs/feature/epic-5513-servicenow-integration/` are left untouched; this feature supersedes that
decision for the unmapped-state path only. The near-miss-typo case is accepted as a cost: it is
recoverable by looking at the Team's state mapping, which is where the coach would look anyway.
