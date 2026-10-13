# DISTILL wave decisions — epic-5878-baseline (2026-10-13)

**Agent**: Quinn (`nw-acceptance-designer`), autonomous subagent, after the maintainer's UI-sketch walkthrough
(DT1–DT7 in `feature-delta.md`). **Scope**: US-01..US-08 and US-10, slices 01–08 and 10 (Epic #5878, ADO
#6159–#6168). US-09 (slice 09, settings-changed notice) and US-11 (slice 11, chart) are not distilled: DISCUSS
dropped both (slice 11 on 2026-10-03, slice 09 by D46).

**Reconciliation passed — 0 contradictions** across `discuss/`, `design/` and `devops/` (recorded in the delta's
DISTILL / Reconciliation section; not re-run).

| # | Decision | Why |
|---|---|---|
| DST-1 | Backend scenarios are black-box over HTTP and JSON on the real host and real EF; no backend scaffold, no migration | Precedent (refinement, story 6083). The Reports module does not exist yet, so anything typed against it would be guessed; the wire contract is what DESIGN fixed |
| DST-2 | The instance clock is pinned to 2 Oct 2026 09:00 in Europe/Zurich, the date every DISCUSS example uses | Windows are instance-zone calendar days; a test on the wall clock asserts nothing about which days |
| DST-3 | Stored report rows are reshaped with parameterised SQL on the test host's own connection, to stand in for a row written by another version (unknown type, extra payload member, a metric never captured) | DEVOPS's forward-tolerance rules are about rows this version did not write; the API cannot write one, by design. Policy row appended |
| DST-4 | One walking skeleton, E2E, on demo scenario 11 (Team Lightspeed): Reports tab → Create Report, each wizard step waited for by its own line and left as it opens → the report opens with Then frozen and the {Cycle Time} panel. `test.fixme` until slice 01 | DISCUSS's slice-01 elevator pitch; the maintainer keeps E2E to one thin skeleton per flow |
| DST-5 | Frontend scenarios render the real `ReportsTab` / `ReportView` and drive the wizard through them; only `ReportService`, the usage reporter and Terminology are stood in. The exception is the two tab-placement files (`TeamDetail.reportsTab`, `PortfolioDetail.reportsTab`), which stand the Reports components in because their subject is where the tab sits and which address opens what | The wizard is reached from the tab (create) and the view (edit); testing it alone would miss how it opens and what happens after |
| DST-6 | A held answer (`deferred()`) is how the loading and re-read looks are reached | DT6 asks for both looks; a fake that answers at once cannot show either |
| DST-7 | `ReportOpened` is asserted with fake timers: nothing at 4.9 s, one event at 5.1 s, nothing for a report left after 3 s or one that failed to load, nothing more after an edit's re-read | DEVOPS's "after 5 s mounted, once per mount" |
| DST-8 | Owner deletion is asserted in the Overview, where Team and Portfolio deletion lives: the dialog appends "and its 2 reports" and the usage event stays `TeamDeleted` / `PortfolioDeleted`, never `ReportDeleted` | D42 and DEVOPS's "not on owner cascade". Count source: upstream issue 6 |
| DST-9 | The wire is pinned where DESIGN left it open (member names, refusal codes, list `thenRebuildable`) | Listed in `upstream-issues.md`, items 1–4 |
| DST-10 | Copy no upstream artifact wrote is proposed and pinned, flagged for confirmation | `upstream-issues.md`, "Copy DISTILL had to propose" |
| DST-11 | Tier B (state-machine property tests) not used | The Then & Now journey is not a chain of three or more scenarios over a rich input space: each scenario is one create and one read. Window arithmetic is covered by `TestCase` rows at its edges (14 days, the oldest kept day, today) |
| DST-12 | Mandate-8 state-delta port and Python domain-types module: not applicable | The project is C# / TypeScript; the policy records NUnit and Vitest mechanisms and no state-delta port. Scenarios assert the unchanged part explicitly where it matters (`ThenTheOwnerHoldsNoReport`, refused edits re-reading the report) |
| DST-13 | Walking-skeleton strategy: ports and adapters per the project policy — real host, real EF and real browser for the skeleton; only the licence, the clock and the usage-data publisher are faked | Architecture of Reference, inherited; the old per-feature A/B/C/D choice is retired |
