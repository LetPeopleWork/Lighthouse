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

## OUT of scope

- The risk arithmetic — slice 02.
- The widget — slice 04.
- Any other column in the dialog. Widening the dialog changes how they all lay out, which is the point; none of them changes.

## Call-site table

Fill during the slice. One row per `WorkItemsDialog` call site: file:line · which config it passes · risk descriptor yes/no · if no, why that is correct.

## Dependencies

**Upstream**: slice 02, for the column description text, so the screenshot is taken once. The width and plumbing work do not otherwise depend on it.

**Downstream**: slice 04's View Data opens a `WorkItemsDialog`; adding that call site after this sweep means it lands on an already-correct pattern instead of becoming the sweep's next miss.

## Watch-outs

- **A `@screenshot` run needs a premium licence, and the fixture is gitignored** — absent from every worktree. Import it from the main checkout before running.
- **`rm` the PNG before regenerating.** The screenshot comparison keeps the old file when the pixel diff is under 0.5%, which is exactly the size of a width change on a wide dialog.
- **Exclude `@auth` from the run.** A DB wipe reds the premium tests and the PNGs already removed are then lost.
- **Run Playwright locally before committing** any spec or POM locator touched here.
