# Slice 03 — Show the SLE Risk column without horizontal scrolling

**ADO**: User Story #6035 · **Story**: US-R2-03 · **Job**: `job-flow-coach-act-before-sle-breach`
**Estimate**: ≤1 day.

## Goal

The risk column is on screen when the dialog opens, from every entry point that should have it.

## Learning hypothesis

**Disproved if** a `WorkItemsDialog` call site is found still silently omitting the descriptor after the sweep. The descriptor is passed per call site, so this class of miss is invisible by construction — the sweep is the deliverable, and a miss surviving it means the design needs changing, not the call site.

**Confirmed if** every call site is accounted for in a written table, and the column reads at 1280px from both the widget header's View Data and a bubble click.

## IN scope

- **Width** — `WorkItemsDialog.tsx:431` pins `maxWidth="md"`. Widen to `xl` and add a maximise toggle. The toggle is the part that keeps paying: this dialog has gained the age band, time in state, SLE risk and named cycle times, and will gain more.
- **The missing column** — the Work Item Aging chart renders its own `WorkItemsDialog` for bubble clicks at `WorkItemAgingChart.tsx:909`. `sleRiskColumn` is wired only into the `wipOverview` and aging dashboard configs in `BaseMetricsView.tsx` (L636, L713), so a bubble click opens a dialog with no risk column. Plumb the descriptor through as a prop.
- **The sweep** — enumerate every `WorkItemsDialog` call site and record, in this brief, whether it passes the risk descriptor or is deliberately without one. A call site with no risk to show (a closed-item list, a portfolio surface) is a legitimate "without", and saying so is the point.
- **Screenshot** — re-take `docs/assets/features/metrics/sle_risk_column.png` at the new width. This is the round's single re-take: it lands after slice 02 changed the column's description text and after this slice changed the width.

## Also in scope: delete `ItemsInProgress.tsx`

**Maintainer decision, 2026-09-19: no ADO item, fix it in this slice's DELIVER.**

`src/pages/Teams/Detail/ItemsInProgress.tsx` has no production caller. Only its own test imports it — verified: the sole other matches in `src/` are a same-named local variable in an unrelated test file. Delete the component and its test together.

It is in scope here because this slice sweeps every `WorkItemsDialog` call site, and a dead call site is one the sweep has to reason about and account for every time. **Round 1 already found this and nobody acted on it**, which is how a sweep's inventory grows a permanent footnote.

Carries no risk of the usual kind: nothing to grep in `Lighthouse.EndToEndTests/` because no E2E reaches it, and no `data-testid` leaves the shipped bundle. The one check worth doing is the deletion's characteristic Sonar failure — a helper, constant or type that only this component used becomes unused the moment it goes.

## Acceptance criteria

Written during DISTILL, which found DISCUSS promising *"Full ACs at `slices/slice-03-column-visible.md`"*
against a brief that carried none, while DESIGN cited `AC-03.1` and `AC-03.2` as if they existed. A test
written against a criterion nobody wrote down is a test whose subject can be argued after it fails.

| ID | Criterion |
|---|---|
| AC-03.1 | At a 1280px-wide viewport, for a viewer **with no stored grid layout**, the SLE Risk column is on screen when the work item dialog opens from the widget header's View Data, with no horizontal scrolling |
| AC-03.2 | The same, from a click on a bubble on the Work Item Aging chart |
| AC-03.3 | Every list of what the team has in flight today carries the risk column, and every other list deliberately does not — with no list unaccounted for |
| AC-03.4 | A coach can enlarge the dialog, and it opens the way they left it next time |
| AC-03.5 | A risk cell says how much finished work the number rests on, without changing what the cell shows, exports or sorts by |
| AC-03.6 | The disclosure is true for an item past its target, for an item with history, and for an item nothing the team finished ever ran as long as |
| AC-03.7 | Every `WorkItemsDialog` render site is accounted for, with the reason written for each deliberate omission — and the one site nobody can reach is deleted rather than footnoted again |

## OUT of scope

- The risk arithmetic — slice 02.
- The widget — slice 04.
- Any other column in the dialog. Widening the dialog changes how they all lay out, which is the point; none of them changes.

## Call-site table

**Fifteen production render sites, all accounted for** — sixteen before `ItemsInProgress.tsx` was
deleted, exactly as DESIGN predicted. Test renders are excluded: they render the dialog to assert
about the dialog, not to show a reader a list.

The rule is one sentence: **a list of what the team has in flight today carries the risk column, and
nothing else does.** The number answers "will this item breach its target", which only means
something for an item still running, against one team's published target.

| # | Call site | What it lists | Risk column | Why |
|---|---|---|---|---|
| 1 | `MetricsView/WidgetShell.tsx:380` | whichever payload the widget was built from | **yes, when the payload carries one** | The one site that renders every `buildViewData` payload. It forwards `viewData.sleRiskColumn`, so the decision is the payload's rather than this site's — which is the whole of ADR-198, and what the key-set partition test enforces. **Five of the twenty-six payloads carry a descriptor** — four when this table was written, plus `sleRisk`, added by slice 04 |
| 2 | `Charts/WorkItemAgingChart.tsx:801` | the items behind one bubble | **yes** | In flight today, on one team, at one age. Assembled inside the chart from a click, so no payload can reach it — this slice's fix, and the defect that was reported |
| 3 | `MetricsView/BaseMetricsView.tsx:1947` | items that contributed days to one state | no | A history question — how long work sat in a state — not a list of what is in flight. Its own highlight column is "Days Contributed" |
| 4 | `Charts/CycleTimeScatterPlotChart.tsx:554` | closed items | no | They finished. There is nothing left to be at risk of |
| 5 | `Charts/EstimationVsCycleTimeChart.tsx:286` | closed items with estimates | no | Same |
| 6 | `Charts/BlockedItemsOverTimeChart.tsx:127` | what was blocked on a past day | no | A snapshot of a day that has gone. Today's odds do not describe it |
| 7 | `Charts/ProcessBehaviourChart.tsx:562` | the items behind one point on a control chart | no | A historical point, not a population in flight now |
| 8 | `Charts/BarRunChart.tsx:149` | the items behind one bar | no | Run charts count what happened on a past day |
| 9 | `Charts/LineRunChart.tsx:197` | the items behind one point | no | Same |
| 10 | `Charts/TotalWorkItemAgeRunChart.tsx:168` | the items behind one day's total age | no | Same — a past day's total, not today's population |
| 11 | `Charts/WorkDistributionChart.tsx:348` | one slice of a type distribution | no | Deliberately mixed: its highlight column reads cycle time where there is one and age otherwise, so it is closed and open work together. Not a list of what is in flight |
| 12 | `Charts/FeatureSizeScatterPlotChart.tsx:790` | features by size | no | Features, and closed ones. The target is a work-item target |
| 13 | `Teams/Detail/TeamFeatureList.tsx:149` | a feature's child work items | no | Reached from a feature rather than from the team's flow. Left alone deliberately — see below |
| 14 | `Portfolios/Detail/PortfolioFeatureList.tsx:146` | a feature's child work items | no | Portfolio surface. A feature spans teams, and the risk is one team's number against one team's target |
| 15 | `Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx:856` | a feature's work items in a delivery | no | Same reason as 14 |

**Rows 13 and 14 are the ones worth revisiting, and this slice deliberately does not.** They list
work items that may well be in flight, and a coach looking at a feature could reasonably want the
same number. The obstacle is real rather than editorial: the risk is computed per team against that
team's published target, and a feature's children can belong to several teams — so the column would
need a per-item team lookup that nothing on those surfaces currently has. Recorded here rather than
acted on, because widening the rule is a decision about what the number means, not a plumbing job.

**What the sweep cannot catch, stated rather than implied.** A sixteenth render site added outside
`buildViewData` and outside the aging chart. The partition test governs the payloads and the
required prop governs the chart; a brand-new component rendering the dialog itself is caught by
nothing but this table being redone. ADR-198 says so in writing.

**The partition's first live exercise, one slice later.** Slice 04 adds a `sleRisk` payload, and the
test red on its first run naming the key, both lists and this table — before any test of that slice
existed. It is classified as a list of what is in flight today, which is why row 1's count moved from
four payloads to five.

**And a correction this table owes itself**: slice 04's own DISTILL said the new payload would become
"row 16" here. It does not. This table has one row per **render site**, and a new payload adds no
render site — it is a fifth thing rendered by the site already in row 1. Fifteen rows is still the
whole of it.

## Dependencies

**Upstream**: slice 02, for the column description text, so the screenshot is taken once. The width and plumbing work do not otherwise depend on it.

**Downstream**: slice 04's View Data opens a `WorkItemsDialog`; adding that call site after this sweep means it lands on an already-correct pattern instead of becoming the sweep's next miss.

## Watch-outs

- **A `@screenshot` run needs a premium licence, and the fixture is gitignored** — absent from every worktree. Import it from the main checkout before running.
- **`rm` the PNG before regenerating.** The screenshot comparison keeps the old file when the pixel diff is under 0.5%, which is exactly the size of a width change on a wide dialog.
- **Exclude `@auth` from the run.** A DB wipe reds the premium tests and the PNGs already removed are then lost.
- **Run Playwright locally before committing** any spec or POM locator touched here.
