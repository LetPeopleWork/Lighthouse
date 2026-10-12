<!-- markdownlint-disable MD024 -->
# Feature Delta — story-6202-lists-beyond-100-rows

> ADO User Story #6202 "Lists with more than 100 rows silently show only the first 100" · tag Tech Debt ·
> Active · no parent. Every list built on the shared `DataGridBase` shows its first 100 rows and nothing
> else, with no hint that more exist. Found in the adversarial review of Story #6144; recorded as a known
> limit in `docs/evolution/2026-10-09-epic-5510-sizing-votes.md:217` and
> `docs/evolution/2026-10-09-epic-5881-refinement-need.md:233`.

Lean DISCUSS pass (2026-10-12), density `lean`, Tier-1 `[REF]` sections only. User-facing, brownfield,
one story, one slice, no walking skeleton. **DISCOVER and DIVERGE were explicitly skipped by the
maintainer.** The layout was approved by the maintainer as a sketch on 2026-10-08 (D1, D2).

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the code on 2026-10-12.

| # | Fact | Evidence |
|---|---|---|
| S1 | The free MUI X `DataGrid` always paginates; its page size cannot exceed 100. `DataGridBase` sets no pagination model, so every grid sits on page 1 with 100 rows. | `DataGridBase.tsx:254` (`pageSizeOptions={[10, 25, 50, 100]}`) |
| S2 | `hidePagination` defaults to `true` and is wired to `hideFooter`, so the page controls (and the row count) are hidden. In place since commit `fe426f0a8` (2025-11-21). | `DataGridBase.tsx:66,256`; `types.ts:61-62` |
| S3 | Eight consumers, all affected: `DataOverviewTable`, `FeatureGrid`, `FeatureListDataGrid`, `WorkItemsDialog`, `OverviewDashboard`, `ArchivedFeatureGrid`, `RefinementGrid`, `ThirdPartyPackagesSection`. Only the last overrides the default (`hidePagination={false}`), so it alone shows a footer today, with a rows-per-page picker. | `ctx_search DataGridBase` over `Lighthouse.Frontend/src` (13 files: the 8 consumers, `DataGridBase.tsx`, `types.ts`, `index.ts`, 2 test files) |
| S4 | CSV export and Copy (premium) already take **every** row in the current sort order, not the visible page: they read `getSortedRowIds()`. | `DataGridToolbar.tsx:48-70` |
| S5 | `RefinementGrid` replaces row rendering (`slots`, `getRowSpacing`); whatever paging does must still work with a custom row. | `RefinementGrid.tsx:93`; `DataGridBase.tsx:258-259` |
| S6 | Story #6248 (work-items dialog context columns) is changing `DataGridBase` at the same time: `defaultColumnVisibilityModel` is declared but "not read yet", and its tests are `it.skip`. | `types.ts:77-81`; `DataGridBase.defaults.test.tsx` |

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `team-member-voter` | **Jonas Weber**, developer in Team Gravity. Gravity has **143** Work Items in refinement; he wants to vote on `GR-0131`, which sorts to row 131 and today is unreachable. Primary. |
| `flow-coach` | **Priya Raman**, coach of Team Gravity. Opens the Refinement tab and the Work Items dialog behind a chart (e.g. 260 closed Work Items over the last 180 days) and needs to trust that the list is whole. |
| `product-owner` | **Marco Bianchi**, PO of Team Gravity; walks the refinement list before Wednesday's Refinement. |
| `delivery-forecaster` | **Lena Fischer**, delivery lead for the Ocean Explorer Portfolio; reads the Portfolio's Feature list and archived Features. Secondary. |
| `config-admin` | Reads Settings → System → Third-party packages (well over 100 packages). Secondary. |

## Wave: DISCUSS / [REF] JTBD One-Liner

`job_id: job-team-member-give-sizing-view` (primary) — *When I am asked whether a Work Item fits our SLE,
I want to give my view in seconds, from where I already am.* Today a Work Item past row 100 cannot be
reached at all, so the job fails outright for it.

Also served: `job-flow-coach-refine-just-enough` (the coach's count and triage are only honest when every
Work Item in refinement is on the list). The same defect cuts every other list-backed job short (Feature
lists, Work Items dialogs, archived Features); the fix restores reach for all of them and changes none,
so **`jobs.yaml` is not edited**.

Forces — *push*: rows past 100 vanish with no hint, so a reader concludes "that's all". *Pull*: the grid
already holds every row; only the way to reach them is missing. *Anxiety*: lists that are short today
must not grow a footer or change. *Habit*: scrolling a list to its end and trusting the end.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **The footer appears only when a grid has more than 100 rows.** A grid with 100 rows or fewer looks exactly as it does today, with no footer. | Maintainer, approved sketch 2026-10-08 |
| D2 | **Fixed pages of 100.** The footer shows the row range and total plus previous/next arrows, e.g. `1–100 of 243  ‹ ›`. There is **no rows-per-page picker**, so nothing is persisted. | Maintainer, 2026-10-08 |
| D3 | **Settings → Third-party packages follows the same rule.** Its always-on footer override goes; it shows a footer only above 100 rows, and loses its rows-per-page picker. | Maintainer |
| D4 | **Every `DataGridBase` consumer in one slice**, not page by page. Virtualised scrolling without pages (the alternative in the ADO item) was rejected in favour of the conditional footer. | Maintainer |
| D5 | **Lightweight DISCUSS**: one story, one slice, no walking skeleton; DISCOVER and DIVERGE skipped. | Maintainer |
| D6 | **CSV export and Copy keep exporting every row** in the current sort order, never only the visible page (S4 — already true; pinned here so paging cannot regress it). | Taken in DISCUSS, from current behaviour |
| D7 | **No DEVOPS wave.** The change is frontend-only, with no infrastructure, pipeline or observability impact. | Maintainer, 2026-10-12 |
| D8 | **No usage-data event.** A paging footer is a fix that makes existing rows reachable, not a feature anyone chooses to use, so there is nothing worth counting. | Maintainer, 2026-10-12 |

## Wave: DISCUSS / [REF] User Stories

### US-01: Every row of a long list can be reached, and a short list stays as it is

As **Jonas** (team-member-voter), I want a list with more than 100 rows to tell me how many rows it has
and let me page to the rest, so that I can find and vote on any Work Item in refinement, not just the
first 100.

`job_id: job-team-member-give-sizing-view` (also `job-flow-coach-refine-just-enough`)

#### Elevator Pitch

Before: Team Gravity's Refinement tab lists 143 Work Items in refinement but shows 100, with nothing
saying so. Jonas looks for `GR-0131`, does not find it, and assumes it was refined already.
After: below the 100th row the tab shows `1–100 of 143  ‹ ›`; Jonas clicks `›`, sees `101–143 of 143`,
and votes on `GR-0131`. A Team with 40 Work Items in refinement sees the same list as today, no footer.
Decision enabled: Jonas votes on every Work Item he has a view on, and Priya decides how many more need
refining from a list she knows is complete.

#### Domain Examples

1. **Happy path** — Gravity, 143 Work Items in refinement: footer `1–100 of 143`; `›` shows rows 101–143.
2. **Short list unchanged** — Team Kestrel, 38 Work Items in refinement; Ocean Explorer's Feature list
   with 12 Features: no footer, identical to today.
3. **Boundary** — exactly 100 rows: no footer. 101 rows: `1–100 of 101`, and page 2 holds one row.
4. **List shrinks** — Priya, on page 2 of a 243-row Work Items dialog, filters to 64 rows: the footer
   disappears and all 64 rows show — never an empty page with no way back.
5. **Export** — Lena (premium) exports the 243-row Feature list while on page 1: the CSV has 243 rows.

#### UAT Scenarios (BDD)

##### Scenario: A long list says how many rows it has and pages to the rest

```gherkin
Given Team Gravity has 143 Work Items in refinement
When Jonas opens Gravity's Refinement tab
Then he sees the first 100 Work Items and "1–100 of 143" below them
When he goes to the next page
Then he sees Work Items 101 to 143 and "101–143 of 143"
And he can vote on GR-0131
```

##### Scenario: A list of 100 rows or fewer looks exactly as today

```gherkin
Given Team Kestrel has 38 Work Items in refinement
When Priya opens Kestrel's Refinement tab
Then all 38 Work Items show
And no row count or page arrows show below the list
```

##### Scenario: The footer starts at the 101st row

```gherkin
Given a Feature list with exactly 100 Features
Then all 100 show and no footer shows
Given the list grows to 101 Features
Then "1–100 of 101" shows and the next page holds the 101st Feature
```

##### Scenario: Pages are always 100 rows, with nothing to choose

```gherkin
Given a list with 243 rows
When Priya looks at the footer
Then it offers the previous and next page only
And there is no rows-per-page choice
```

##### Scenario: Third-party packages follows the same rule

```gherkin
Given Lighthouse ships more than 100 third-party packages
When the admin opens Settings → System → Third-party packages
Then the footer shows the range and total with page arrows and no rows-per-page choice
When the admin searches for "react" and 9 packages match
Then all 9 show and the footer is gone
```

##### Scenario: A list that shrinks to 100 rows or fewer shows all of them

```gherkin
Given Priya is on the second page of a Work Items dialog with 243 Work Items
When she filters it down to 64 Work Items
Then all 64 show and no footer shows
```

##### Scenario: Export takes the whole list, not the page

```gherkin
Given Lena has a premium licence and a Feature list of 243 Features sorted by name
And she is looking at the first page
When she exports the list to CSV
Then the file holds all 243 Features in name order
```

#### Acceptance Criteria

- [ ] **AC-1.1** Every `DataGridBase` grid with more than 100 rows shows a footer with the range and total (`1–100 of 143`) and previous/next arrows; every row is reachable through them.
- [ ] **AC-1.2** A grid with 100 rows or fewer renders no footer and is visually unchanged (existing @screenshot images of short lists do not change).
- [ ] **AC-1.3** The rule switches at the boundary: 100 rows → no footer, 101 rows → footer with a second page holding one row.
- [ ] **AC-1.4** Pages are fixed at 100 rows; no rows-per-page control is shown and nothing about paging is persisted to the grid's saved state.
- [ ] **AC-1.5** Settings → Third-party packages has no override left and follows AC-1.1 to AC-1.4, including when its search narrows the list.
- [ ] **AC-1.6** When a grid's rows drop to 100 or fewer while a later page is open (filter, search, data reload), all remaining rows show and the footer disappears; no state leaves the reader on an empty or unreachable page.
- [ ] **AC-1.7** Sorting orders the whole list, not only the visible page (sorting by a column puts the overall first row on page 1).
- [ ] **AC-1.8** CSV export and Copy (premium) contain every row in the current sort order, whichever page is open (D6).
- [ ] **AC-1.9** It holds for all eight consumers (S3), including `RefinementGrid` with its custom row rendering.

#### Outcome KPIs

See [REF] Outcome KPIs.

#### Technical Notes

- Frontend only; no API, DTO or schema change.
- MUI X free tier caps a page at 100 (S1) — D2 sits at that cap.
- `DataGridBase.tsx` is also being changed by Story #6248 (S6): expect a merge, not a conflict of intent.

## Wave: DISCUSS / [REF] Definition of Done

1. AC-1.1 … AC-1.9 pass in Vitest + React Testing Library against `DataGridBase` and at least `RefinementGrid` and `ThirdPartyPackagesSection`.
2. Checked by hand on the dev instance (`:5169`) on a list above 100 rows and one below (see slice dogfood).
3. Existing E2E and @screenshot runs are green with no regenerated images for lists of 100 rows or fewer.
4. Docs: grep `docs/` for wording that says a list shows "all" rows or mentions page controls; amend only if found. A release-notes line is drafted on #6202 (`Release Notes` tag) in Team/Work Item/Feature terms.
5. `pnpm test` and `pnpm build` (Biome included) clean; no new SonarCloud issues; Stryker ≥ 80 % on changed lines.

## Wave: DISCUSS / [REF] Out of Scope

- Virtualised scrolling without pages (rejected, D4).
- A rows-per-page picker or remembering the page across visits (D2).
- Server-side paging; lists keep loading all their rows as today.
- Any change to which rows a list holds, its columns, sort defaults or filters.
- Whether CSV export should honour an active column filter (today it does not — S4 reads every sorted row); noted for DESIGN, not changed here.

## Wave: DISCUSS / [REF] WS Strategy

Strategy **B (extend existing)**: no walking skeleton. The grid, its consumers and MUI's pager exist end to
end; the slice changes when the footer shows and what it holds, in one shared component.

## Wave: DISCUSS / [REF] Driving Ports

- The `DataGridBase` component's props (`rows`, and the `hidePagination` prop whose fate is an open question), rendered by each of the eight consumers.
- The user's pager clicks, sort clicks and column filter, and consumer-side search (Third-party packages).
- The toolbar's Export/Copy actions (premium).

## Wave: DISCUSS / [REF] Pre-requisites

- None blocking. MUI X `DataGrid` free tier as installed. Coordinate the `DataGridBase.tsx` edit with Story #6248 (S6), which is unpushed at the time of writing.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

1 story, 1 slice, 1 bounded context (shared frontend grid) touched through 1 component plus 1 consumer
override removed. About 1 day. No oversized signal.

## Wave: DISCUSS / [REF] Story Map and Prioritisation

Backbone: *open a list* → *see how much there is* → *reach any row* → *act on it / export it*.
Single release, single slice (`slice-01-every-row-reachable`). Priority rationale: it is a correctness
defect that silently blocks voting and sizing on large Teams (#5510, #5881 shipped with it as a known
limit), costs about a day, and has no dependency.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-6202-no-hidden-rows` | 0 of the 8 `DataGridBase` consumers leave a row unreachable (was 8 of 8 above 100 rows) | Component tests with 243 rows on `DataGridBase` + consumer spot checks (AC-1.9); dev-instance check of Third-party packages and a Work Items dialog over 100 rows |
| `OUT-6202-short-lists-untouched` | 100 % of grids with ≤ 100 rows render no footer; 0 @screenshot images of short lists change | Vitest boundary test (AC-1.3); @screenshot run without regenerated images |

A behavioural KPI (votes on Work Items past row 100) was drafted and dropped: it needs a usage-data event, and the maintainer decided there is none for this story (D8).

## Wave: DISCUSS / [REF] Open Questions for DESIGN

1. **Page on change while still above 100 rows.** When sort, filter or a data reload changes a list that stays above 100 rows, does the reader stay on the current page or go back to page 1? (AC-1.6 already fixes the ≤ 100 case: all rows, no footer.) Lean: back to page 1 on sort/filter, keep the page on a background reload — DESIGN decides.
2. **`hidePagination` prop.** Remove it (one rule for all, D3/D4), or keep it for a future grid that must never page? With its only `false` caller gone, nothing passes it.
3. **Footer copy and look.** MUI's default range label (`1–100 of 243`) and arrow buttons match D2; confirm the default pager hides its rows-per-page select when only one size is offered, or hide it explicitly.
4. **Export and the column filter.** Export reads every sorted row and ignores an active column filter, although a comment in the toolbar says the filter decides. Out of scope here; DESIGN may want a separate item.
5. **Loading look.** While `loading` is true and no rows are known, the footer stays hidden (≤ 100 rule). Confirm no flash of a footer when a list loads in chunks.

## Wave: DISCUSS / [REF] Lighthouse DISCUSS Checklist

1. **RBAC impact — N/A, because** paging changes how rows already delivered to the browser are shown, not which rows are delivered or who may act on them; no new `useRbac()` gate and no read of `/authorization/my-summary`.
2. **Lighthouse-Clients CLI/MCP versioning — N/A, because** the change is frontend-only: no API endpoint, DTO or payload changes, and `lh` / MCP never render `DataGridBase`. No clients release.
3. **Website marketing surface — N/A, because** this fixes a defect in existing lists; there is nothing new to market. DELIVER greps `/storage/repos/website` for any screenshot of a Third-party packages list with its old rows-per-page picker (outside this session's readable roots).
4. **Configurable terminology — applied.** The footer copy uses no configurable term (range and total only). This document, the release note and any docs use the seeded defaults *Team*, *Portfolio*, *Feature*, *Work Item*; never Epic/Story/Initiative for user concepts.
5. **Each Epic ships on its own — N/A, because** this is a single User Story with no parent Epic and one slice.
6. **Usage-data event — N/A** (maintainer, D8), because a paging footer makes existing rows reachable rather than adding a feature anyone chooses to use. DEVOPS, where this is normally asked, is skipped (D7).

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | Problem statement clear, domain language | PASS | Elevator Pitch Before: 143 Work Items, 100 shown, nothing says so |
| 2 | Persona with specific characteristics | PASS | Jonas Weber (`team-member-voter`), Team Gravity, 143 Work Items in refinement; Priya, Marco, Lena, config-admin |
| 3 | 3+ domain examples with real data | PASS | 5 examples: Gravity 143, Kestrel 38 / Ocean Explorer 12, 100 vs 101, 243 → 64 filter, 243-row export |
| 4 | UAT in Given/When/Then, 3-7 | PASS | 7 scenarios |
| 5 | AC derived from UAT | PASS | AC-1.1…1.9 map to the scenarios plus S3/S5 coverage |
| 6 | Right-sized | PASS | ~1 day, 7 scenarios, one demo session |
| 7 | Technical notes | PASS | Frontend-only; MUI 100-row cap; #6248 overlap |
| 8 | Dependencies resolved or tracked | PASS | None blocking; #6248 tracked in Pre-requisites |
| 9 | Outcome KPIs with measurable targets | PASS | 2 KPIs with numeric targets, both measurable without usage data (D8) |

**DoR: PASSED.** `job_id` present; Elevator Pitch present. Peer review (`nw-product-owner-reviewer`) not run
in this pass — left to the dispatching session.

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions:** D1 footer only above 100 rows; D2 fixed pages of 100, range + arrows, no picker; D3 Third-party packages follows; D4 all consumers in one slice, conditional footer over virtualised scroll; D6 export stays whole-list.
- **Feature type:** user-facing defect fix in a shared frontend component.
- **Constraints:** MUI X free tier (100-row page cap); no backend change; short lists must not change.
- **Waves skipped by the maintainer:** DISCOVER, DIVERGE, DEVOPS (D7). DESIGN, DISTILL follow (DISTILL starts with the maintainer sketch walk-through; the D1/D2 sketch is already approved).
- **Upstream changes:** none to `jobs.yaml` or personas.
