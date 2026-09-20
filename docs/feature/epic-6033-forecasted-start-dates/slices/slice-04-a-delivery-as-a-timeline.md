# Slice 04 — A Delivery shows its Features as a timeline

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-04
**Estimate**: evaluation spent (see the spike result below) + **~7–9h build on SVAR**
**Reference class**: **none in-house, and that is the main schedule risk.** The original entry here —
that the existing `@mui/x-charts` charts are the nearest thing — described the build this slice is no
longer doing. Two corrections. First, a reference class for the *x-charts* route did turn out to
exist: `BlackoutOverlay.tsx` and `DeliveryFeverChart.tsx` already draw custom marks from chart scales,
with a unit-test pattern to copy. Second, and the one that matters: the chosen route is SVAR, and for
that there is **no precedent anywhere in this codebase** — no third-party React component wrapped
behind an adapter, no `--wx-*`-style CSS-variable theming, and no test pattern for asserting against a
component we did not write. The estimate below carries that.

## Goal

Open a Delivery, choose Timeline, and see its Features as bars against a date axis — one percentile
selector moving both ends of every bar together.

## IN scope

- A Timeline tab beside Features and Metrics on the Delivery. The tab structure exists (S20).
- One bar per Feature, in `WorkItemBase.Order` (S15) — the board's order, which is also the order the
  simulation itself works in.
- A P70 / P85 / P95 selector, defaulting to P70, moving the left and right edge of every bar together
  (D10). A bar is always one scenario, never a P70 start welded to a P85 finish.
- A started Feature's bar begins at its observed start date (D5).
- The Delivery's target date marked on the axis, where it has one. **Delivered as a tinted axis column
  rather than a vertical line** — a drawn marker is a PRO feature and is deferred. The date is still
  marked; it is shaded, not ruled.
- A Feature with no forecast listed with a stated reason, rather than dropped. **Listed beside the
  timeline rather than as an empty row inside it** — an in-chart row for an undated task is a PRO
  feature, and the free build does something worse than omit it (it invents a bar), so such Features
  are filtered out of the chart and listed next to it.
- Premium gate, using the Delivery surface's existing notice (D8).
- Light and dark, and the narrowest width the Delivery view supports. **Below the narrow breakpoint the
  task-name pane is dropped** (`columns={false}`) and the timeline scrolls at full bar size; leaving the
  pane in place pushes the chart off-screen entirely at 360px.
- Docs saying plainly what an out-of-order board does to the picture (AC-4.9, D6). A Feature nobody has
  started, drawn as starting now while work sits lower in the order, is the timeline reporting the board
  rather than misreading it. Undocumented, that intended oddness arrives as a bug report.

## OUT of scope

- Dependencies drawn between bars. Slice 05, severable.
- Per-team sub-lanes under a multi-team Feature's bar. Slice 06, severable. One bar per Feature here.
- A timeline anywhere but on a Delivery.
- Dragging, editing, or writing anything back from the timeline. It is a read.
- Purchasing a licence. The evaluation may recommend one; the decision is not this slice's to take.

## Learning hypothesis

**Disproves, if it fails**: D11 — that a timeline can be built from what is installed, and therefore
that this Epic ends without a commercial dependency.

**Confirms, if it succeeds**: the Epic closes inside its own slices.

## Pre-slice SPIKE — build versus buy

**Yes, timeboxed to 2 hours, before any production code.** This is P8, the Epic's only open
pre-requisite.

The question is not "is a Gantt hard" in the abstract. It is: **can `@mui/x-charts` 9.0.1, already
installed, draw one horizontal bar per row against a date axis, themed like the rest of the product, in
light and dark, at the Delivery view's narrowest width?** What is being drawn is geometrically simple —
the hard parts of a commercial Gantt (drag-to-reschedule, editable durations, nesting, virtualised
scroll over thousands of rows) are all out of scope here.

Produce, within the timebox:

1. A throwaway render of five hardcoded bars on a date axis, in both themes.
2. The line count and the list of things that had to be hand-rolled.
3. A recommendation with a number attached — hours to finish, against the MUI X Premium licence cost.

Ask one question beyond the timebox's own: **can a bar carry sub-lanes?** Slice 06 needs a summary bar
with per-team lanes under it. It is severable, so it does not gate this decision — but a component that
forecloses it, or charges another tier for it, is worth knowing about while the choice is still open.

**Candidates, so the timebox is not spent finding them.** Two hours is not long enough to survey the
field and build a prototype, so the field is listed here and the hours go on the prototype:

| Candidate | What it is | Why it might not fit |
|---|---|---|
| `@mui/x-charts` 9.0.1 | Already installed, MIT, already themed to the product | Not a Gantt. A horizontal bar on a date axis has to be assembled from primitives |
| Plain SVG or CSS grid | No dependency at all, total control of theming | Everything is hand-rolled — axis ticks, zoom, tooltips, responsive width |
| MUI X Gantt | Purpose-built, matches the design system exactly | ~~Premium licence, not owned. The thing the evaluation exists to price against~~ **It does not exist.** See the result below — this row was written on an assumption the evaluation disproved |
| A third-party Gantt (`frappe-gantt`, `vis-timeline`, `gantt-task-react` and similar) | Purpose-built and free | A second charting idiom to theme in light and dark, a new dependency to keep current, and licence terms to read before anything else |

The shortlist is not a recommendation and is not exhaustive. It exists so the two hours produce a
rendered prototype rather than a browser history.

Two outcomes, both cheap. Either the build is a few hundred lines and slice 04 proceeds as written, or
it is not and the product owner gets a costed licence question before six hours go into the wrong
answer.

**Record the result here before writing the first test:**

### Result — BUILD. Run 2026-09-20, inside the 2h box.

**Headline: the buy option does not exist.** MUI X Gantt is not a product. The docs page for it says,
in as many words, "The Gantt Chart component isn't available yet, but you can upvote this GitHub issue
to see it arrive sooner." It is a planned Premium component with no release date. So the framing this
spike inherited — build versus buy a licence we already half-expect to need — had no second half. There
is no price to pay MUI for a Gantt at any number of dollars. The $599/dev/yr MUI X Premium licence is
real and buyable, but it buys Data Grid Premium, the Scheduler, and a **Scheduler Event Timeline that
is in beta** — a resource-allocation calendar, not a Gantt over forecast distributions.

**And the build works.** A throwaway prototype rendered all of AC-4.1 to AC-4.6 plus the slice 06
sub-lane probe, in both themes, in **190 lines in one file** — of which ~95 lines is the custom `<g>`
that draws the bars and the rest is axis and container wiring. Zero console errors, zero new
dependencies, zero theming work.

**Tooltips, added after the first review, also work** — see the follow-up below. They are no longer an
assumption.

#### What the prototype actually did

Built on the installed `@mui/x-charts` 9.0.1 composition API — `ChartsContainer` with `series={[]}`, an
x-axis at `scaleType: "utc"`, a y-axis at `scaleType: "band"` over Feature names, and one child
component reading `useXScale` / `useYScale` / `useDrawingArea` to emit a `<rect>` per Feature.

| AC | Result |
|---|---|
| 4.1 one bar per Feature in board order | Yes. The band axis renders rows in array order; no sorting logic needed |
| 4.2 bar spans start to completion | Yes, a plain `<rect>` between two `xScale(date)` calls |
| 4.3 P70/P85/P95 moves both ends together | Yes, and measured: an unstarted bar went x=368 w=102 at P85 → x=435 w=142 at P95. Both edges moved |
| 4.4 observed start begins the bar | Yes. The started Feature's left edge stayed pinned at x=223 across all three percentiles while its right edge moved |
| 4.5 no-forecast Feature listed with a reason | Yes. The band axis reserves the row whether or not anything is drawn in it; the reason renders as `<text>` in the empty row |
| 4.6 target date marked on the axis | Yes, `ChartsReferenceLine` with `x={date}` — a built-in, no custom code |
| 4.7 premium gate | Not a charting concern. Untouched by the choice |
| 4.8 both themes, narrowest width | Themes: **free.** Narrow width: **the one real problem.** See below |
| Slice 06 sub-lanes | **Not foreclosed.** Per-team lanes inside the summary bar are a nested `<rect>` loop, ~12 lines |

**Theming cost is zero, and this is the finding that decides it.** Both themes were correct on the
first render with no theme code — axes, gridlines, tick labels, the reference line and its label all
inherit from the MUI theme already, because this *is* the product's chart library. Every third-party
candidate would have needed that work done by hand, twice.

**The reference class the brief said did not exist, does.** `BlackoutOverlay.tsx` already draws raw
`<rect>`s into a chart using `useDrawingArea` + `useXScale`, pulling colours from `appColors`, and
`DeliveryFeverChart.tsx` already uses `useXScale` + `useYScale` the same way. Both have tests that mock
`@mui/x-charts/hooks`. So the idiom, the theming convention, and the unit-test pattern are all in the
codebase — the estimate below leans on that. What is genuinely new is only `ChartsContainer` with no
series (the existing cases hang off a `BarChart` or `ScatterChart`); it worked without complaint.

#### What had to be hand-rolled

Axis ticks, gridlines, the date scale, the reference line, tick formatting and both themes all came
free. Hand-rolled: the bar rectangles, the observed-start marker, the no-forecast row text, the
sub-lane rects, and the x-domain min/max computation. That is the ~95 lines.

#### Shortlist, with the evidence

Checked 2026-09-20 against the npm registry, Bundlephobia, and each project's own licence page.

| Candidate | Version / last publish | Licence | Bundle added (min+gzip) | React 19 | Verdict |
|---|---|---|---|---|---|
| **`@mui/x-charts` 9.0.1 primitives** | installed | MIT | **0** — already in the bundle | yes | **Recommended** |
| Hand-rolled SVG, no chart lib | — | — | 0 | yes | Viable but strictly worse: re-implements ticks, scales and theming that x-charts already gives free |
| MUI X Gantt | **does not exist** | (would be Premium) | — | — | Not purchasable at any price |
| MUI X Scheduler Event Timeline | `@mui/x-scheduler-premium` 9.0.0-**beta**.12 | Premium, $599/dev/yr | ~850 KiB unpacked | yes | Wrong shape (resource calendar, not forecast bars) and beta. Rejected |
| SVAR React Gantt | `@svar-ui/react-gantt` 2.7.3, 2026-09-09 | MIT core / PRO $749 perpetual | 90 KiB | yes | Best third-party. But the MIT core omits **vertical markers** — which is AC-4.6 — and 17 transitive `@svar-ui/*` packages arrive with it. Note the older `wx-react-gantt` package on npm is **GPLv3** and stale (Feb 2025); easy trap |
| DHTMLX Gantt | `dhtmlx-gantt` 10.0.3, 2026-09-03 | MIT community / PRO from $799 per dev | 171 KiB | vanilla JS, needs a wrapper | Mature, but the heaviest option and its own DOM and CSS world to theme twice |
| Frappe Gantt | 1.2.2, 2026-02-25 | MIT | **14 KiB** | vanilla JS, needs a wrapper | Lightest third-party. Still its own CSS to theme twice, and imperative DOM against React |
| vis-timeline | 8.5.4, 2026-08-12 | Apache-2.0 OR MIT | 120 KiB | wrapper needed; **peer-depends on `moment`** | Rejected. Dragging `moment` into a 2026 bundle is a step backwards |
| `gantt-task-react` | 0.3.9, **2022-07-10** | MIT | 10 KiB | peer caps at React 18 | Rejected. Unmaintained for four years and incompatible with the installed React 19 |
| `react-calendar-timeline` | only `0.30.0-beta.*` | MIT | — | betas only | Rejected. No stable release; `latest` tag points at beta.4 while beta.19 exists |
| Planby | 2.1.0, 2026-08-23 | **"Custom License"** | — | yes | Rejected. EPG-shaped, and a non-standard licence is a legal review this slice does not need |

Two notes the table cannot hold. First, **Lighthouse ships under the Lighthouse Source Available
License**, not an OSI licence — so any GPL candidate is disqualified outright, which is why the
`wx-react-gantt` / `@svar-ui/react-gantt` package-rename trap matters. Second, the third-party column
"bundle added" understates the cost: the real price of every one of them is a second charting idiom
that has to be themed for light and dark by hand and kept current forever, against a first-party one
that is themed already.

#### Recommendation

**Build it on `@mui/x-charts` 9.0.1. Buy nothing. ~6–8 hours, £0, no new dependency.**

The number: 205 prototype lines already cover AC-4.1 to AC-4.6, bar tooltips, and the sub-lane probe.
What remains is the narrow-width label strategy (~1.5h), the percentile selector and tab wiring (~1h),
keyboard and screen-reader access (~1h), swapping `theme.palette.*` for the `appColors` tokens the
other charts use (~0.5h), wiring real no-forecast reasons and the premium gate (~1h), and unit tests
following `BlackoutOverlay.test.tsx` (~2h). **The brief's original ~6h build estimate holds.**

The cheapest third-party alternative that satisfies AC-4.6 is SVAR PRO at **$749 perpetual per
developer** plus 90 KiB plus a second theming surface — to replace roughly 95 lines of `<rect>`. That
is not a trade worth making.

#### The one thing that did not work: 360px

At the narrowest width the 190px Feature-name label column eats the chart. The drawing area collapses
to ~150px, bars shrink to 17–22px and stop being readable as durations, and the no-forecast reason text
runs past the plot edge. This is a layout problem, not a library problem — the fix is a responsive
label strategy (names above bars rather than beside them below some breakpoint, and truncation with a
tooltip for the reason text) and it is the 1.5h in the estimate above. **No candidate would have solved
this for us**; a third-party Gantt with a fixed task-list pane would have made it harder, not easier.

#### What would change this recommendation

State the disagreement against one of these, not in general:

1. **MUI ships X Gantt.** If it lands before slice 04 starts and is in Premium, re-open — the licence
   would then buy a first-party component with the same theming-is-free property. Track the upvote
   issue. Nothing suggests this is imminent.
2. **Slice 05 dependencies turn out to need arrow routing.** Drawing arrows between bars is meaningfully
   harder than drawing bars. If slice 05 grows to need real edge routing with collision avoidance, the
   total build cost crosses the $749 line and SVAR PRO becomes rational. This spike deliberately did not
   prototype arrows — slice 05 is severable and its brief should run its own probe before assuming.
3. **Someone wants drag-to-reschedule.** Explicitly out of scope here, and the single thing that flips
   build-versus-buy hardest. A read-only timeline is ~95 lines; an editable one is a product.
4. **The row count grows past a few dozen.** No virtualisation was tested. At five Features it is
   irrelevant; at 200 it is not. If Deliveries with hundreds of Features are real, that needs measuring
   before the estimate is trusted.
5. **The 360px fix costs more than 1.5h.** It is the only unproven number in the estimate.

#### Honest limits of two hours

- Not tested: keyboard navigation, screen-reader output, or print. (Tooltips **were** tested — below.)
- Not tested: more than five rows, so nothing is known about virtualisation or scroll.
- Not prototyped: dependency arrows (slice 05). The sub-lane probe (slice 06) *was* done and passed.
- The third-party candidates were assessed on licence, size, maintenance and API shape — **not** built
  with. Their rejections rest on documented facts rather than on hands-on failure.
- Prototype lives outside the repository, under the session scratchpad, and nothing was installed. It
  is throwaway by construction; no Lighthouse file other than this brief was touched.

#### Follow-up: tooltips, confirmed

Raised at review as an assumption, so it was tested rather than assumed. **They work, in 15 lines.**

The obstacle was real and worth recording: the codebase's existing custom-tooltip pattern
(`CumulativeStateTimeChart.tsx`) uses `ChartsTooltipContainer` + `useItemTooltip()`, which resolves the
hovered thing out of *series* data. This timeline has `series={[]}` and draws raw `<rect>`s, so there is
no chart item for `useItemTooltip()` to find, and that pattern does not transfer.

What does work is simpler: wrap each `<rect>` in a plain MUI `<Tooltip followCursor>`. Verified by
hovering under Playwright — the tooltip renders with the Feature name, its start (worded "Started" for
an observed start and "Starts" for a forecast one) and its completion with the percentile, in both
themes, with zero console errors. It themes itself, because it is the same `Tooltip` the rest of the
product uses.

Consequence for slice 04: tooltips leave the estimate as a line item and the remaining accessibility
work is keyboard and screen-reader only. Consequence for the eventual implementation: do **not** reach
for the `ChartsTooltipContainer` pattern here by analogy with the other charts — it does not apply to
custom-drawn marks.

#### Follow-up: is the free SVAR edition enough?

Asked at review, so it was checked properly rather than left on one line from a pricing page.

**Two things in SVAR's favour, both worth stating plainly.** It is genuinely React — the tarballs for
`react-gantt`, `react-core` and `lib-react` contain no `.svelte` files and no JavaScript that so much
as mentions Svelte, despite SVAR being a Svelte-first house. And the whole transitive tree is **25
packages, every one of them MIT**, which clears the Lighthouse Source Available licence without
argument. The upgrade path is clean too: PRO is the same API behind a perpetual licence, so starting
free and buying later is a real option and not a rewrite.

**But two of this slice's acceptance criteria are PRO features, not one.** From SVAR's own feature
matrix:

| AC | SVAR feature that implements it | Edition |
|---|---|---|
| AC-4.6 target date marked on the axis | "Vertical markers on timeline" | **PRO** |
| AC-4.5 Feature with no forecast, listed with a reason, no bar | "Unscheduled tasks" | **PRO** |

AC-4.6 is deferrable — a Delivery timeline without its target line is still useful, and the marker can
arrive with a later licence purchase. **AC-4.5 is not deferrable in the same way.** Dropping it does
not mean "the timeline lacks a nicety"; it means a Feature with no forecast silently disappears from a
picture the reader is using to plan. Lighthouse already carries scar tissue from exactly this shape of
bug — a work item that vanishes with no warning is worse than one shown as unknown. The AC is written
the way it is on purpose.

Both have workarounds — an absolutely-positioned div for the target line, a zero-duration task with a
custom cell template for the unforecast row. Both mean hand-drawing over a library adopted so we would
not have to hand-draw.

**The deciding cost is not the AC list, though. It is theming.** SVAR styles itself through its own
Willow / WillowDark themes and a `--wx-gantt-*` CSS-variable system. Matching the product means mapping
`appColors` onto a second, parallel variable system and keeping the two in step for as long as the
component lives. `@mui/x-charts` costs zero here because it reads the MUI theme directly — which the
prototype demonstrated by rendering both themes correctly with no theme code at all.

Two smaller mismatches point the same way. SVAR is an **editing-first** Gantt — drag-and-drop
rescheduling is one of its free headline features — and this timeline is explicitly read-only, so we
would be adopting an editor and switching the editing off. And it renders a fixed task-list grid pane
beside the chart, which makes the 360px problem worse rather than better, since a fixed side pane is
precisely what is already eating the narrow width.

**Verdict: unchanged. Build on x-charts.** The free edition would cost two ACs and a permanent second
theming surface, to avoid writing ~95 lines of `<rect>`.

**When SVAR does become the right answer:** if slice 05's dependency arrows prove hard. At that point
the calculation changes shape — $749 perpetual buys dependency links, vertical markers and unscheduled
tasks together, and the arrow-routing problem is the one genuinely worth not solving ourselves. That is
a slice 05 decision on slice 05's evidence, and this spike deliberately did not gather it.

### DECISION — SVAR React Gantt, free edition. Taken 2026-09-20 by the maintainer.

**This supersedes the spike's recommendation above.** The spike recommended building on `@mui/x-charts`;
the maintainer chose SVAR's MIT edition, accepting the AC-4.5 and AC-4.6 deferrals, on the grounds that
the free edition is generous, that hand-building is judged harder than the spike's 205-line prototype
suggests, and that PRO remains a fair one-off purchase if the deferred ACs are wanted later. The
recommendation is left standing above rather than rewritten, so a reader six months from now can see
what was weighed and disagree with the same evidence.

**What this slice builds with**: `@svar-ui/react-gantt` 2.7.3, MIT, 90 KiB gzip, 25 transitive MIT
packages, React 19 compatible.

**Accepted deferrals** — both are PRO features, both are wanted eventually, neither blocks a first
version:

- **AC-4.6** — no target-date marker on the axis. Clean deferral; nothing else depends on it.
- **AC-4.5** — no "unscheduled task" row for a Feature with no forecast. **This one needs a
  substitute, not just a deferral.** A Feature that silently disappears from a planning picture is the
  failure this AC exists to prevent. The cheapest substitute needs no PRO licence and no Gantt feature
  at all: list the unforecast Features *beside or beneath* the timeline, with their reason, as ordinary
  markup. The reader still learns the Feature exists and why it has no bar; only the in-chart row is
  deferred. Slice 04 should do this rather than let those Features vanish.

**Licence nuance, for whoever buys it later**: SVAR PRO at $749 is a perpetual licence to *use* the
versions released in the term, bundled with **one year** of support and updates. Perpetual use, not
perpetual updates — a renewal is needed to keep receiving new versions, not to keep running.

**Mandatory: the third-party component is encapsulated behind our own boundary.** The maintainer's
condition, and the thing that keeps the slice 05 reconsideration cheap. Concretely:

- One adapter component owns every `@svar-ui/*` import. Nothing else in `Lighthouse.Frontend` imports
  from SVAR — worth an ArchUnit-style guard or a Biome `noRestrictedImports` rule so it stays true.
- The adapter's props speak Lighthouse's language — Features, percentiles, observed starts, Delivery
  target — never SVAR's task/link vocabulary. Mapping our model onto SVAR's task shape happens inside
  the adapter and nowhere else.
- The adapter is read-only by contract. SVAR is an editing-first Gantt; its drag, resize and link
  editing are switched off at the boundary rather than relied upon to be unused.
- Theming is configured in one place, mapping `appColors` onto SVAR's `--wx-gantt-*` CSS variables for
  both light and dark.

**Before writing production code, re-run the prototype against SVAR.** The SVAR assessment in this
brief is documentation-based — licence terms, feature matrix, package tree — and the component was
never built with. The two unknowns that only a hands-on probe settles are the real theming cost of
mapping `appColors` onto `--wx-gantt-*` across both themes, and how the fixed task-list pane behaves at
360px, which is already this slice's weakest point. Both are cheaper to discover in a throwaway than
mid-build.

**Consequence outside this file**: AC-4.5 and AC-4.6 are defined in `feature-delta.md`, which this
session does not own. Their deferral, and the substitute listing for AC-4.5, need recording there by
whoever holds that file — this brief cannot be the only place the change lives. Likewise P8 in the
pre-requisites table now resolves to "SVAR free edition", not to the spike's recommendation.

### SVAR hands-on probe — run 2026-09-20, after the decision

The hands-on probe the decision above asked for, run before any production code. Throwaway React 19 +
Vite app outside the repository, `@svar-ui/react-gantt` 2.7.3 installed only there. **150 lines of TSX
and 21 of CSS**, driven under Playwright. Zero console errors in the working configuration.

**Read-only holds, and it is one prop.** `readonly` on the component. Verified adversarially rather
than assumed: the probe grabbed the first bar, dragged it 160px and released. Bar geometry was
byte-identical before and after. Editing is genuinely off, not merely unused.

**Hiding chrome is one prop too.** `columns={false}` removes the entire task-grid pane, leaving just
the timeline. This matters more than it sounds — see the narrow-width result below.

**Theming works, and switches the way the product switches.** `Willow` and `WillowDark` are wrapper
components; swapping which one wraps the chart re-themes it, so a Lighthouse light/dark toggle drives
it directly. Retinting bars to a product colour is a single CSS-variable override
(`--wx-gantt-task-color`) and composes on top of either theme. Confirmed by computed style, not by
eye: `--wx-background` goes `#ffffff` → `#2a2b2d`, font `#2c2f3c` → `rgba(255,255,255,.9)`.

**Narrow width is better than x-charts managed** — with a caveat. At 360px with the task grid on, the
timeline is pushed entirely off-screen and the component is useless. With `columns={false}` it scrolls
horizontally at full bar size instead of squashing, which is a more honest answer than the 17px stubs
x-charts produced. The narrow-width strategy is therefore "drop the grid pane below a breakpoint", not
a bespoke label layout.

#### Four traps, all found by running it

1. **`markers` is silently ignored in the free build.** It is in `IConfig` and it typechecks, because
   the type package is shared with PRO. At runtime nothing renders, no warning, no error. A developer
   would reasonably conclude the feature works and only discover otherwise by looking. **The free
   substitute is the per-scale-cell `css` callback**, which tints the target's own column — visible in
   the screenshots as the shaded Dec 2026. Not a line, but it marks the date on the axis without a
   licence.
2. **`unscheduled: true` is also ignored — and this one invents data.** The Feature with no forecast
   did not appear without a bar; it was drawn *with* a bar, at a position nothing in the data implies.
   That is worse than the silent omission this AC was written to prevent, because a plausible-looking
   bar is not obviously wrong. **Whatever the adapter does, it must not hand SVAR a task it cannot
   place.** Unforecast Features are filtered out of the task list and listed separately, as the
   decision above already requires.
3. **Scale callbacks receive LOCAL midnight, not UTC.** `css` and `format` are handed
   `2026-12-01T00:00+01:00`, i.e. `2026-11-30T23:00Z`. Reading those with `getUTCMonth()` lands a month
   early anywhere east of UTC — the probe tinted January before this was found. Given how much of this
   codebase is anchored to UTC deliberately, any date comparison at the SVAR boundary has to convert
   explicitly. This is exactly the class of bug that ships.
4. **The `Material` theme crashes the free build.** Clicking it throws `e is not a function` and blanks
   the whole app. Only `Willow` and `WillowDark` are usable, which is all this slice needs — but do not
   reach for `Material` on the assumption that it is the closest fit to MUI.

Two smaller papercuts: `columns` has a **typing bug** — the component declares `false |
IColumnConfig[]` while `IConfig` declares `IGanttColumn[]`, and the intersection rejects `false`, so
the documented way to hide the grid needs a cast. And the component paints no background of its own;
the host must set one, which is one line using SVAR's own `--wx-background`.

Also worth knowing: import **`@svar-ui/react-gantt/all.css`**, not `style.css`. The slim sheet omits
the surrounding grid and core styles, which makes the dark theme look broken while reporting the
correct variables — it cost this probe a wrong conclusion before it was caught.

**Verdict on the decision: it stands, and the probe found nothing to reverse it.** Read-only, chrome
hiding and dual-theme switching are each one prop or one wrapper. The two PRO gaps have free
substitutes — a tinted target column for AC-4.6, and the separate listing for AC-4.5 — so neither
deferral leaves a hole in the picture. The adapter boundary the decision mandates is what keeps traps
1 to 3 in one file.

#### Capability survey — what the free edition supports

Read off the installed typings and the free bundle, with the load-bearing ones rendered rather than
inferred.

| Capability | Free? | How |
|---|---|---|
| **Dependency arrows** | **Yes — rendered and confirmed** | A `links` array of `{source, target, type}` with `s2s`/`s2e`/`e2s`/`e2e`. Routing is automatic: elbow connectors with arrowheads, no geometry code |
| Tooltips on bars | Yes | The exported `Tooltip` component takes a custom `content` render-prop, handed `{task}`, `{link}`, `{rollup}` or `{resource}` |
| Click / interaction events | Yes | 37 dispatchable actions, each surfacing as an `on<Action>` prop — `onselectTask`, `onopenTask`, `onscrollChart`, `onzoomScale` and so on |
| Change the resolution | Yes | `zoom: { levels: [{ minCellWidth, maxCellWidth, scales }] }` — each level carries its own scale set, from year down to hour, plus a `zoom-scale` action |
| Summary tasks, milestones, sub-task hierarchy | Yes | Task `type` and `parent` |
| Filtering, sorting | Yes | `filter-tasks`, `sort-tasks` |
| **Export to PNG / PDF / Excel** | **No — PRO** | `export-data` is absent from the free bundle entirely. Worth knowing beyond the licence: `IExportConfig` carries a `url`, so this is **server-side** export against a service, not a client-side canvas render. PRO alone would not deliver it — something has to host the exporter |
| Vertical markers | No — PRO | Confirmed: SVAR's own docs carry the PRO badge, and nothing renders in the free build. The marker CSS *does* ship in the free stylesheet, which is why it looks as though it ought to work |
| Unscheduled tasks | No — PRO | See trap 2 above |
| Critical path, baselines, resources, WBS, undo | No — PRO | Not wanted by this Epic |

**This corrects an earlier caveat in this brief.** The recommendation section said slice 05's
dependency arrows might be the thing that forces a PRO purchase, because arrow routing is the piece
genuinely worth not solving ourselves. That is now wrong on the facts: arrows are free and SVAR routes
them itself. **Slice 05 has no licence implication**, and the strongest remaining argument for ever
buying PRO is image export — which, being server-side, is a larger decision than a licence anyway.

**If image export is wanted later**, weigh browser print or a client-side canvas library against PRO
plus a hosted export service. The licence is the cheaper half of that bill.

### Build estimate on SVAR — ~7–9h

The header's original `~6h build` was computed for the `@mui/x-charts` route and does not transfer;
this replaces it. The work is a different shape — no custom drawing at all, but an adapter, a
dependency to keep honest, and a testing problem we have not had before.

| Work | Est. |
|---|---|
| Adapter component: confine every `@svar-ui/*` import, props in Lighthouse's vocabulary, map Features → tasks, filter out the unplaceable ones | 1.5h |
| Theme wiring: `appColors` → `--wx-gantt-*` for light and dark, wrapper swap driven by the app's current mode | 1h |
| Timeline tab on the Delivery + the P70/85/95 selector | 1h |
| Unforecast Features listed beside the chart (the AC-4.5 substitute) | 0.5h |
| Target-month tint via the scale `css` callback, including the local/UTC conversion | 0.5h |
| `readonly`, `columns={false}`, and the narrow-width breakpoint | 0.5h |
| Premium gate — reuses the existing notice | 0.25h |
| Unit tests | 2h |
| Dependency hygiene: bundle-size check, Biome/Sonar on a new dep, the `noRestrictedImports` rule guarding the adapter | 0.75h |

**Roughly 8h, call it 7–9h.** Slightly above the x-charts figure rather than below it, which is worth
being clear about: choosing the library removed the drawing but added an adapter, an import guard and
dependency upkeep that the in-house route did not carry. The saving is in risk and in slices 05 and
06, not in this slice's hours.

**Where the estimate is softest**, in order:

1. **Testing a component we did not write.** There is no precedent for it here. Assert on the *mapping
   function* — Features and percentiles in, tasks out — which is pure and where every interesting
   decision lives. Do not assert on SVAR's DOM; that is testing someone else's library and it will
   break on their release, not ours.
2. **Theme-variable coverage.** `--wx-gantt-task-color` was verified. The full set needed for a
   convincing light and dark pass — grid lines, scale header, row borders, hover — was not enumerated.
   If SVAR's variables do not reach some surface, the fallback is overriding its classes, which is more
   brittle and would push the 1h higher.
3. **Whether `readonly` also suppresses hover affordances and the context menu**, or only blocks the
   write. Editing was confirmed not to happen; whether the UI still *offers* it was not checked.

### Handover — changes needed in `feature-delta.md`

This session does not own that file; the slice 03 session does. Four changes, none of them made here:

1. **P8** in the pre-requisites table → resolved: SVAR React Gantt, free MIT edition. Not "build on
   x-charts", which is what this brief recommended before the decision.
2. **AC-4.6** → the target date is marked as a tinted axis column, not a drawn vertical line. A drawn
   marker is PRO and is deferred; the licence can be bought later without rework.
3. **AC-4.5** → the Feature with no forecast is listed *beside* the timeline with its reason, not as an
   empty row within it. Keep the AC's intent word for word — the Feature must not disappear — and
   change only where it is rendered. The free build's `unscheduled` handling is not merely absent: it
   draws a bar at a position the data does not support, so the adapter must never pass it such a task.
4. **D11** — "a timeline can be built from what is installed, and therefore this Epic ends without a
   commercial dependency." This is the Epic's learning hypothesis and it has now returned a split
   result, which is worth recording rather than leaving to stand. *Built from what is installed*: *no*
   — a new dependency is being added. *Ends without a commercial dependency*: *yes* — SVAR's core is
   MIT, the whole transitive tree of 25 packages is MIT, and nothing is being bought. The spike also
   showed the first half was achievable (the x-charts prototype worked); it was not chosen. D11 should
   say that, so a later reader does not mistake the outcome for a failure to find a way.

## Acceptance criteria

AC-4.1 through AC-4.9 in `feature-delta.md`.

## Dependencies

Slice 01 (start distribution). Slice 02 is not required but will have surfaced any believability problem
first, which is why it is sequenced earlier.

## Effort

~2h evaluation + ~6h build. The build half is a genuine ≤1-day slice only if the evaluation confirms
the geometry is simple. If it does not, this brief is rewritten around whatever the evaluation
recommends rather than stretched.

## Dogfood moment

Same day: open a real Delivery on the dev instance and switch the percentile from 70 to 95. The demo is
not that bars appear — it is watching every bar slide right together and seeing whether the Delivery
still clears its target date. That is the moment the feature either earns the word "plan" or does not.

Expect bars to abut rather than gap where one team works Features in sequence (D7). That is the model
being honest and is not to be "fixed".
