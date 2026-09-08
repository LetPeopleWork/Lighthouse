# Feature Delta — story-5914-metrics-time-horizon-presets

**ADO**: User Story #5914 — *Default Timings in Metrics Time Horizon Selection* · Active · tagged
`Release Notes` · no parent Epic.

**Waves**: DISCUSS (2026-09-08).

**One line**: the metrics dashboard's time window can only be reached by typing two calendar dates
into a popover; this feature adds named windows you reach in one click and a pair of steppers that
walk the same-length window backwards and forwards through time.

**Density**: `lean` + `ask-intelligent` (`~/.nwave/global-config.json`). Tier-1 `[REF]` only.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `delivery-lead-rte` | **Primary.** Opens a Team's or Portfolio's metrics during a retro, a review or a leadership conversation, where the window on screen is rarely the window the question is about, and where the follow-up question is always *"better or worse than before?"* |
| `flow-coach` | **Secondary.** Same controls, different cadence — narrows to the last week during a standup rather than widening to a quarter for a review. Reads, never configures. |

No new persona. No third stakeholder.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-lead-reach-the-metrics-window-i-mean-in-one-click` | When the window on screen isn't the one my question is about, I want to reach the window I mean without computing two calendar dates, so I can ask the question while I still have the room's attention. |
| `job-lead-compare-this-period-against-the-one-before` | When I've read this period's numbers and someone asks whether that's better or worse, I want to step the same-length window back one period and re-read the same widgets, so I answer with a comparison instead of a snapshot. |

Both are new jobs against `docs/product/jobs.yaml`. The second is the *reading* twin of the shipped
`job-delivery-lead-tell-blocked-trend-vs-last-period` (Epic #5074), which gives one widget a
baked-in previous-period delta. This job generalises that instinct to **every** widget on the
dashboard, by moving the window rather than by adding a second series to each chart.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Grounded in the code as of `a8e13c1e8`, not in the story text. Five findings, two of which change
what this story is.

| # | Finding | Evidence |
|---|---|---|
| S1 | The window is React state seeded from `?startDate` / `?endDate`, written back with `replace: true`. There is no preset, no stepper, and no persistence of any kind. | `BaseMetricsView.tsx:1215-1252` |
| S2 | **Portfolios already open at 90 days.** The story asks for this; it shipped already. | `PortfolioMetricsView.tsx:68` |
| S3 | **Teams do not open at a flat 30 days.** The opening range is derived from the Team's own throughput window (`throughputEndDate − throughputStartDate`), falling back to 30 only when *Fixed Dates* is on. | `TeamMetricsView.tsx:49, 117-128` |
| S4 | **The two date setters cannot be composed.** `handleStartDateChange` writes `updateDateParams(date, endDate)` and `handleEndDateChange` writes `updateDateParams(startDate, date)`, both closing over the *current render's* values. Calling them in sequence to move both ends makes the second call rewrite the first's `startDate` back to the stale value — and `updateDateParams` rebuilds from a stale `searchParams` too. Any two-ended change needs a new combined setter; it cannot reuse these. | `BaseMetricsView.tsx:1232-1252` |
| S5 | **A committed window change refetches the whole visited category.** `useVisitedCategories`' reset token embeds the formatted start and end dates, so a new window resets the visited set to just the current category and every fetch for it re-fires. | `BaseMetricsView.tsx:1267-1270`, `useCategorySelection.ts:68-104` |

Reusable precedent, both already in the codebase:

| Precedent | Location | Reused for |
|---|---|---|
| A `<Chip>` row of relative date shortcuts (`End of week`, `+1 week`, `+2 weeks`) | `ManualForecaster.tsx:360-393` | The visual language of the preset chips. **Code is not shared** — see D17. |
| The date popover: `CalendarMonthIcon` `ButtonBase` → `Popover` → `DateRangeSelector` (two bounded MUI-X pickers) | `DashboardHeader.tsx:100-144`, `DateRangeSelector.tsx:149-195` | The home for the presets. |
| A Playwright POM that applies a window by URL and waits on the request that carries the new `endDate` | `MetricsPage.ts:347-407` (`MetricsDateRange`) | The E2E acceptance path; extended, not replaced. |

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — The Team's opening range stays derived from its throughput window

The story asks to "start teams with a default range of 30 days". S3 shows Teams already have a
default, and it is a *better* one: the window their own throughput configuration names. Overriding
it would silently change what every existing Team dashboard shows on the next load, and would
decouple the metrics window from the window the forecasts are computed over.

`TeamMetricsView.tsx:117-128` is not touched. The presets are purely additive. *(User, 2026-09-08.)*

### D2 — The Portfolio's 90-day default is already shipped and is not re-litigated

`PortfolioMetricsView.tsx:68` already passes `defaultDateRange={90}`. Half of the story's
default-range ask is a no-op. Recorded here so nobody re-implements it, and so the release notes do
not claim it as new.

### D3 — Two controls ship, not one

Absolute **preset chips** (a named window ending today) *and* relative **window steppers** (shift the
whole window by one step). They answer different questions and neither substitutes for the other:
a preset reaches "last 3 months" in one click but cannot walk backwards; a stepper walks backwards
but takes eight clicks to widen 30 days into 90. *(User, 2026-09-08.)*

### D4 — The preset sets differ by owner type

| Owner | Presets |
|---|---|
| Team | `Last 7 days` · `Last 14 days` · `Last 30 days` · `Last 90 days` |
| Portfolio | `Last 30 days` · `Last 90 days` · `Last 180 days` |

Teams are read at a standup cadence and Portfolios at a quarterly one, which is the same reason
their opening defaults already differ (S2, S3). A shared set would be too coarse at one end and too
noisy at the other.

### D5 — The stepper granularity differs by owner type too

Team steps **1 week**; Portfolio steps **4 weeks**. Straight from the story: *"For teams we could
add something like '- 1 week'. For portfolios perhaps just in 4 week steps."*

### D6 — A preset sets `end = today` and `start = today − N days`

A preset is a *named window ending now*, not a duration applied to wherever the window currently
sits. Clicking `Last 30 days` after having stepped back three months returns you to today. This is
the escape hatch from a stepped-back window, and it is why D10's lack of persistence is survivable.

### D7 — A stepper moves both ends and preserves the window's length

`‹` sets `start −= step, end −= step`. `›` is its mirror. The window's length never changes, which
is the entire point: *"give me the same range in the previous period."*

The forward stepper **clamps so `end` never passes today**, and renders disabled once the window
already ends today. There is no floor on the backward stepper — history is as deep as the data is,
and an empty window is a legitimate answer that the widgets already render honestly.

Widening the window instead of moving it was considered and rejected: turning a 30-day window into a
60-day one ending today does not answer "last month versus this month", it averages the two together
and hides the very difference the question is about.

### D8 — Clicks accumulate; only the settled window is committed

Each click updates a **pending** window immediately. The pending window is committed to state and to
the URL only after a quiet period (**500 ms**), and only the committed window fetches.

The reason is concrete, not cosmetic: by S5, every committed change resets the visited-category set
and re-fires every fetch for the current category. Four clicks to walk back a month would be four
full refetch storms against a backend that has been asked for three windows nobody wants to look at.
The story anticipated this — *"we should wait a bit before we apply, as we may click it multiple
times"* — and S5 is why.

### D9 — The pending window never reaches the URL

Only the committed window writes `?startDate` / `?endDate`, still with `replace: true`. Intermediate
steps do not enter the URL, are not shareable, and do not appear in browser history. A link copied
mid-click therefore names a real window that was really fetched.

### D10 — No persistence. The window stays URL-only

No `localStorage`, no per-entity memory, no instance-wide setting. The window keeps living exactly
where it lives today. *(User, 2026-09-08.)*

The `useCategorySelection.ts` localStorage pattern was the obvious candidate and is deliberately not
used. Consequence, stated plainly: every fresh visit re-lands on the derived default and the user
re-picks. D6 keeps that cheap — one click, not two typed dates.

### D11 — Steppers live on the header bar; presets live in the popover

The `‹` and `›` icon buttons flank the date label in `DashboardHeader`, directly around
`{formatDate(startDate)} → {formatDate(endDate)}`. The preset chips go **inside** the popover, above
the two pickers in `DateRangeSelector`.

Rationale: walking back three periods must not cost three popover round-trips, and `‹ [window] ›` is
the pattern every calendar and analytics tool already trained the user on. Presets are a
change-the-question action and belong where changing the window already lives.

Under the `sm` breakpoint `DashboardHeader` hides the date text and shows the calendar icon alone
(`DashboardHeader.tsx:90-124`); the steppers stay visible there, since an icon-only stepper pair
around the calendar button is still a usable control and stepping is the more likely mobile action.

**This is the one placement call made without asking.** If DESIGN disagrees, it is cheap to move —
nothing else depends on where the buttons sit.

### D12 — Terminology: not applicable, explicitly

Every label this feature adds is pure date language — `Last 30 days`, and two directional icon
buttons. None of the eleven renameable terms (feature, work item, team, portfolio, delivery, cycle
time, throughput, WIP, blocked, SLE) appears in any new string. The surrounding `Metrics shown for:`
label is untouched.

### D13 — No RBAC and no premium gate

The controls navigate data the viewer can already reach by typing two dates. Gating them would gate
convenience, not information. No `useRbac()` call, no `canUsePremiumFeatures` check, nothing new in
`IRbacAdministrationService`.

### D14 — Zero backend change

No new endpoint, no DTO change, no EF migration, no `HistoricalSchemaPatch` entry. The existing
metrics endpoints already take `startDate` and `endDate`; this feature only changes which pair the
browser sends. `dotnet build` / `dotnet test` are run as a regression check, not because anything
under `Lighthouse.Backend/` is edited.

### D15 — Lighthouse-Clients (CLI / MCP): not applicable, explicitly

The window is browser-side view state. The clients already accept explicit start and end dates on
the calls that need them, and gain nothing from a UI shortcut. No client version bump.

### D16 — The end picker's future dates are left alone

`DateRangeSelector.tsx:180-188` gives the end picker a `minDate` but no `maxDate`, so a user can
already select an end date in the future. Presets (D6) and the forward stepper (D7) both clamp at
today; the manual picker's behaviour is **unchanged**. Tightening it is a separate question about a
pre-existing surface and is not smuggled in here.

### D17 — New code uses date-fns, not dayjs

`DateRangeSelector` runs on `date-fns` via `AdapterDateFns`; `ManualForecaster`'s chip row
(`ManualForecaster.tsx:360-393`) runs on `dayjs`. The chips are a **visual** precedent only. Pulling
`dayjs` into the metrics popover to share four lines of arithmetic would put two date libraries on
one surface to save a `setDate` call.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** Two user stories, two slices, one bounded context (metrics view state),
frontend only. No new component beyond two small presentational ones, no schema change, no new
endpoint, no external integration, no new abstraction. Well under every oversized signal.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield. Every mechanism on the path already runs in production: the
window state, the URL round-trip, the popover, the fetch cycle keyed on the dates, and the E2E POM
that drives it. Nothing here is a mechanism nobody has run.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| Team → Metrics · Portfolio → Metrics, date popover | Gains an owner-aware preset chip row above the two pickers (D4, D11). |
| Team → Metrics · Portfolio → Metrics, dashboard header | Gains backward and forward icon buttons flanking the date label (D5, D7, D11). |
| `?startDate` / `?endDate` query params | Unchanged shape and encoding (local Y/M/D). Written only on commit (D9). |
| HTTP API | **None.** No endpoint added, removed or changed (D14). |
| CLI / MCP | **None** (D15). |

---

## Wave: DISCUSS / [REF] Pre-requisites

- None blocking. Every surface is shipped and stable.
- No coordination with another in-flight item. Bug #5915 (date-picker crash) touches
  `DateRangeSelector`'s validity handling and is held unpushed; it hardens the same file this
  feature extends but does not conflict with it — the presets never hand the picker a user-typed
  date, they set state directly.

---

## Wave: DISCUSS / [REF] Out of Scope

- Persisting the window, per entity or per instance (D10).
- Changing the Team's opening range (D1) or the Portfolio's (D2).
- Constraining the manual end picker to today (D16).
- A previous-period *overlay* — drawing last period as a second series on each chart. That is a
  different, much larger feature; this one moves the window, it does not compare two at once.
- Presets on any other date surface: `ManualForecaster`, `BacktestForecaster`, delivery date pickers,
  Settings. Those keep the controls they have.
- Any backend or client change (D14, D15).
- A "compare to previous period" numeric delta on individual widgets. The Blocked widget already has
  one from Epic #5074; generalising that is not this story.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — Reach the window I mean in one click

**Job**: `job-lead-reach-the-metrics-window-i-mean-in-one-click`

As a Delivery Lead opening a Team's or Portfolio's metrics, I want a row of named windows in the
date popover, so that reaching "the last quarter" does not mean working out what date it was three
months ago and typing it twice.

#### Elevator Pitch
Before: reaching a different window means opening the popover and typing two calendar dates, which
means doing date arithmetic in your head in front of the room.
After: run `Team → Metrics → the date button → Last 90 days` → sees the header read
`10 Jun 2026 → 08 Sep 2026` and every widget on the category redraw over that window.
Decision enabled: whether the pattern you are looking at is a this-month artefact or a
this-quarter trend — the thing you cannot tell from one window.

#### Acceptance Criteria

- **AC-01.1** — Opening the date popover on a **Team**'s metrics shows exactly four preset chips,
  labelled `Last 7 days`, `Last 14 days`, `Last 30 days`, `Last 90 days`, above the two pickers.
- **AC-01.2** — Opening the date popover on a **Portfolio**'s metrics shows exactly three preset
  chips, labelled `Last 30 days`, `Last 90 days`, `Last 180 days`.
- **AC-01.3** — Clicking `Last 30 days` sets the end date to today and the start date to today
  minus 30 days, and both are visible in the header label immediately.
- **AC-01.4** — After clicking a preset, the URL carries **both** `startDate` and `endDate` matching
  the chip's window, in local Y/M/D encoding. This is the regression net for S4: a torn write that
  moves one end and leaves the other stale fails here.
- **AC-01.5** — Clicking a preset while the window is stepped three periods into the past returns
  the window to one ending today (D6).
- **AC-01.6** — The chip whose window matches the current one renders as selected; after any manual
  edit to either picker, no chip renders as selected.
- **AC-01.7** — Clicking a preset navigates with `replace: true` — the browser back button leaves
  the metrics view rather than walking back through preset clicks.
- **AC-01.8** — With `?startDate` / `?endDate` already in the URL on load, that window wins and no
  chip forces itself over it.

### US-02 — Step the same window back a period and read it again

**Job**: `job-lead-compare-this-period-against-the-one-before`

As a Delivery Lead who has just read this period's numbers, I want to move the whole window back one
period and forward again, so that "is that better or worse than last month?" is answered on the same
widgets rather than from memory.

#### Elevator Pitch
Before: comparing against the previous period means computing four dates, typing two of them,
memorising the numbers, then typing the other two back.
After: run `Team → Metrics → the back stepper beside the date label` → sees the header shift from
`09 Aug 2026 → 08 Sep 2026` to `10 Jul 2026 → 09 Aug 2026` and every widget redraw over the period
immediately before.
Decision enabled: whether the current number is a change or a level — whether to raise it.

#### Acceptance Criteria

- **AC-02.1** — With a 30-day window ending today on a **Team**, clicking the back stepper once
  produces a window still 30 days long, with both ends 7 days earlier (D5, D7).
- **AC-02.2** — On a **Portfolio**, one back-stepper click moves both ends 28 days earlier.
- **AC-02.3** — Clicking the back stepper four times in quick succession results in **one** committed
  window (28 days earlier on a Team) and **one** round of metrics requests — not four (D8, S5).
- **AC-02.4** — During those four clicks the header label updates on every click, before anything is
  fetched, and is visually distinguished as not-yet-applied until the commit lands.
- **AC-02.5** — The window is committed 500 ms after the last click, at which point the URL updates
  and the widgets refetch.
- **AC-02.6** — The forward stepper is disabled while the window already ends today, and stepping
  forward never produces an end date after today (D7).
- **AC-02.7** — Stepping forward from a window whose end is fewer than one step from today lands the
  end exactly on today and shortens nothing — the window keeps its length and the start moves with
  it, clamped as one unit.
- **AC-02.8** — Only committed windows enter the URL; four clicks add one history-free
  `replace: true` navigation, not four (D9).
- **AC-02.9** — Unmounting the view mid-debounce (navigating away between the last click and the
  commit) fires no state update and logs no React warning.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone:** open a dashboard → decide the window I need → reach it → read it → compare it against
the period before → answer the question.

| Slice | Stories | Ships |
|---|---|---|
| 01 — *Named windows in one click* | US-01 | The combined two-ended setter that S4 shows is missing, plus the owner-aware preset chip row inside the popover. |
| 02 — *Walk the window through time* | US-02 | The header steppers on top of that setter, with debounced commit, pending-window label and the forward clamp. |

Slice 01 runs first because slice 02 is built on the setter slice 01 has to introduce. Shipping the
steppers first would mean writing that setter anyway and shipping it under the harder of the two
features.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Ships 4+ new components? | No. Slice 01 ships one presentational chip row; slice 02 ships two icon buttons and one hook. |
| Every slice depends on a new abstraction? | The combined `applyDateRange(start, end)` setter is new, and it ships **first**, inside slice 01, in service of a user-visible chip row — not as a standalone plumbing slice. |
| Does any slice disprove a pre-commitment? | Yes, both. Slice 01 disproves "a two-ended window change can reuse the existing per-end setters" (S4). Slice 02 disproves "debouncing the commit is enough to stop the refetch storm without the label lying about what is on screen". |
| Synthetic data only? | No. Both are accepted against the dev instance restored from a production backup, where the deep history a 180-day window needs actually exists. |
| Two slices identical but for scale? | No. Different controls, different failure modes — one is a correctness bug about torn writes, the other is a timing bug about pending state. |

All pass.

---

## Wave: DISCUSS / [REF] Prioritization

1. **Slice 01 first — dependency, and the sharper of the two correctness risks.** S4 is a real trap:
   the two existing setters close over stale values, so the obvious implementation (call both) writes
   a window nobody asked for and fetches it. Finding that with four chips on screen is cheap; finding
   it underneath a debounced stepper, where the torn window flashes past between commits, is not.
2. **Slice 02 second — where the timing uncertainty is.** The pending-versus-committed split is the
   one thing here a user can catch us lying about: a header that reads a window the widgets are not
   showing. Nothing is built on top of this slice, so if the honesty treatment is wrong it costs only
   this slice.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| KPI-1 — Clicks to a named window | **1**, down from 2 typed dates plus mental arithmetic | AC-01.3 acceptance test |
| KPI-2 — Clicks to the previous period | **1**, down from 4 dates computed and 2 typed | AC-02.1 acceptance test |
| KPI-3 — Requests per burst of stepper clicks | **1** round, for any number of clicks inside 500 ms | AC-02.3, asserted on intercepted requests — the direct measure of S5's storm |
| KPI-4 — Torn windows written to the URL | **0** — no navigation ever carries a start from one window and an end from another | AC-01.4 and AC-02.8 |
| KPI-5 — Windows ending in the future via a preset or stepper | **0** | AC-02.6, AC-02.7 |
| KPI-6 — Existing opening ranges changed | **0** Teams and **0** Portfolios open on a different window than before the upgrade | AC-01.8 plus a before/after check on the dev instance restored from a production backup (D1, D2) |

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' acceptance criteria pass as automated tests (Vitest + Playwright).
2. `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean on `./src`.
3. `dotnet build` zero warnings and `dotnet test` green on the non-connector filter — as a regression
   check only; no backend file is edited (D14).
4. SonarQube Cloud introduces no new issue of any severity.
5. Frontend mutation testing (StrykerJS) run per-feature, ≥80% kill rate, recorded under
   `docs/feature/story-5914-metrics-time-horizon-presets/mutation/`. Backend Stryker: **N/A, because
   no backend code changes** (D14).
6. `docs/metrics/` prose updated to describe the preset chips and the steppers on the date control.
7. The metrics-dashboard screenshot showing the date popover regenerated so the chip row is visible
   — **delete the PNG first**, since the regen keeps the old file when the diff is under 0.5%.
8. `ARCHITECTURE.md`: **N/A, because** this adds no concept, port, adapter or store — it is view
   state on an existing surface.
9. Lighthouse-Clients CLI/MCP version bump: **N/A, because** no client-facing contract changes (D15).
10. Website marketing surface: **N/A, because** this is a usability refinement of an existing
    dashboard, not a capability the site advertises.
11. RBAC impact: **N/A, because** no gate is added, removed or moved (D13).
12. Release notes drafted from the customer pain (typing dates, and having no way to ask "versus last
    period") rather than from the control names — the story carries the `Release Notes` tag.
13. ADO #5914 transitioned Active → Resolved, not Closed.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Both elevator pitches; the story is a direct maintainer/customer ask on ADO #5914 with a mockup attached. |
| 2 | Job traceability | US-01 → `job-lead-reach-the-metrics-window-i-mean-in-one-click`; US-02 → `job-lead-compare-this-period-against-the-one-before`. No `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 17 ACs, each asserting a rendered element, a URL value, or an observed request count. |
| 4 | Dependencies known | None blocking. The pre-requisites section records the one adjacent unpushed change (Bug #5915) and why it does not conflict. |
| 5 | Sized | Two slices, ~4h and ~5h of crafter dispatch. Frontend only. |
| 6 | Technical feasibility | Every mechanism is shipped (S1, S5, and the popover/POM precedents). The one genuine trap is S4, and it is named with file and line before a slice starts. |
| 7 | Non-functional constraints | No new request type. D8 strictly *reduces* request volume versus a naive implementation; KPI-3 measures it. |
| 8 | UX defined | D4 fixes the exact labels, D5 the step sizes, D11 the placement of both controls, D7 the clamp behaviour. |
| 9 | Testable in isolation | Vitest against `DashboardHeader`, `DateRangeSelector` and the debounce hook; Playwright through the extended `MetricsDateRange` POM. |

**Requirements completeness: 0.96.** The one deliberate gap is D11's placement of the steppers on the
header bar — the only call made without asking, flagged as cheap to move and left open to DESIGN.

**Per-wave peer review: skipped.** No trigger fired.

**Expansion catalog: no trigger fired.** AC ambiguity — no, every AC names an observable.
Cross-context complexity — no, one context, one stack. Multi-stakeholder — no, two personas reading
the same control, under the three-persona bar. Compliance — no regulatory language anywhere. WS
strategy is C, not D. Strict lean output.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions

- **[D1]** Team opening range stays derived from the throughput window — overriding it would change
  every existing Team dashboard for no gain (user, 2026-09-08).
- **[D2]** Portfolio's 90-day default already ships; recorded so it is not re-built or re-announced.
- **[D3]** Both presets and steppers ship — they answer different questions (user, 2026-09-08).
- **[D7]** Previous-period means *move both ends, keep the length*, with a forward twin clamped at
  today (user, 2026-09-08).
- **[D8]** Debounced commit at 500 ms, because a committed change resets the visited-category set and
  refetches the whole category (S5).
- **[D10]** No persistence; the window stays URL-only (user, 2026-09-08).
- **[D14]** Zero backend change.

### Requirements Summary

- Primary need: reach a named metrics window in one click, and walk that same-length window through
  time to compare periods, without computing calendar dates.
- Walking skeleton scope: N/A — strategy C, brownfield.
- Feature type: **user-facing**, frontend only.

### Constraints Established

- The two existing date setters cannot be composed; a combined two-ended setter is required (S4).
- Every committed window change costs a full category refetch (S5) — hence D8's debounce and KPI-3.
- Dates are encoded local Y/M/D on both the URL and the wire; nothing here may route through UTC
  (the Bug #5566 failure mode, invisible on a UTC CI runner).
- The metrics popover is a `date-fns` surface; `dayjs` does not enter it (D17).

### Upstream Changes

None. No DISCOVER or DIVERGE wave ran for this story; nothing upstream is contradicted.

---

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | Two new jobs (see JTBD one-liners) with dimensions, four forces and opportunity scores; `story-5914-metrics-time-horizon-presets` added to `feature_context`. |
| `docs/product/journeys/story-5914-metrics-time-horizon-presets.yaml` | New journey `compare-this-period-against-the-last`, with emotional arc and D1-D17 recorded. |
| `docs/product/personas/delivery-lead-rte.yaml` | Both new job IDs appended to `primary_jobs`. |

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set.
**To**: `nw-platform-architect` (DEVOPS) — KPIs only; expected to be a no-op, since there is no
deployment, infrastructure or observability surface here (D14).

Open for DESIGN:

1. D11's placement of the steppers on the header bar rather than inside the popover — the one
   unasked call.
2. The debounce mechanism itself: D8 fixes 500 ms and the pending/committed split, and leaves the
   implementation (a `useDebouncedValue`-style hook versus a ref-held timer inside the stepper) open.
3. Whether the combined setter belongs in `BaseMetricsView` beside the two it supersedes, or in a
   `useDateRange` hook that owns the window, the URL round-trip and the pending state together.
