# Story 6452 — Add Info Tooltips to Delivery Metrics

ADO: User Story #6452 (Active, tagged Release Notes).

A customer cannot explain the Delivery **Metrics** tab charts (Burnup, Predictability, Fever, Features
over Time) to their clients: nothing on the chart says what it is for or how to read it. The flow-metric
widgets already carry an ⓘ popover with a short description and a Learn More link; the delivery charts
should too.

## Wave plan (maintainer decision, 2026-10-30)

The maintainer skipped DISCOVER, DIVERGE, DISCUSS, DESIGN and DEVOPS in as many words ("skip discover,
diverge, discuss, design and devops; lean distill and deliver"). The checklist answers those waves would
have produced are recorded here so nothing is skipped silently.

| Wave | Outcome |
|---|---|
| DISCOVER / DIVERGE | Skipped — the problem is a customer report and the fix is named in it (match the flow-metric ⓘ). |
| DISCUSS | Skipped. Checklist answers below. |
| DESIGN | Skipped — no new component, port or endpoint; reuses `InfoPopoverButton`. Notes below. |
| DEVOPS | Skipped. Usage-data answer below. |
| DISTILL | Lean — UI sketch review with the maintainer, then Vitest acceptance tests only. |
| DELIVER | Lean — one slice. |

### DISCUSS checklist

- **RBAC impact:** N/A, because the charts sit behind the existing Delivery read access and the popover adds no action.
- **Lighthouse-Clients CLI/MCP:** N/A, because no API payload or endpoint changes.
- **Website marketing surface:** N/A, because this is help copy on an existing tab, not a new capability.

### DESIGN notes

- Frontend only. Each delivery chart header gains an `InfoPopoverButton` (description + Learn More), the
  same control `WidgetShell` uses for the flow-metric widgets. No status guidance: these charts carry no RAG
  status indicator.
- Copy that names a configurable term (Feature, Work Item) renders the user's term, as the chart titles
  already do with `featuresTerm`.
- **Loading / failure look:** N/A for the popover — it is static text. The tab's existing "Loading metrics..."
  and "Could not load the metrics history" states are unchanged.

### DEVOPS

- **Usage-data event:** N/A, because the existing flow-metric ⓘ popovers emit none either, and a popover open
  would not show whether the charts became understandable — it would only count curiosity.

## Maintainer decisions

- **Popover only, no visible subtitle** (2026-10-30), matching the flow-metric widgets. Each popover carries a
  brief description and a Learn More link to that chart's section in `docs/portfolios/detail.md`.
- **For each chart, consider whether the chart itself can be made clearer** (2026-10-30) — possibly nothing to
  do; decided per chart in the DISTILL sketch review below.
- **Every axis on every delivery chart is labelled** (2026-10-30). Gaps today: the Burnup y-axis (none), the
  Features over Time x-axis (none) and its y-axes ("Items" / "Count", which name neither configurable term).
- **Docs:** add a short "how to read it" passage to the Burnup and Predictability sections of
  `docs/portfolios/detail.md` (2026-10-30). Regenerate the `deliveryXxx.png` screenshots if the ⓘ shows in them.

### DISTILL sketch review

- **Axis labels use the configurable terms.** Burnup y: *Work Items*. Features over Time x: *Date*,
  left y: *Work Items*, right y: *Features* (both rendered as the user's Terminology words).
- **Predictability "How Likely?": popover only.** The chart is unchanged; the popover names the 50/70/85%
  thresholds and their levels. The docs stop calling it "coloured confidence bands" (the chart draws none)
  and describe the line colour instead.
- **Fever chart naming: the docs follow the UI.** The docs heading becomes "Delivery Progress (Fever Chart)";
  Learn More points at its new anchor. The UI title stays "Delivery Progress".
- **Popover copy:** approved as below (2026-10-30). *Work Items*, *Features* and *Delivery* render the
  user's Terminology words. Learn More base: `https://docs.lighthouse.letpeople.work/portfolios/detail.html`.
  - Burnup (`#burnup`): "Total scope against completed *Work Items* over time. The top line is everything in
    this *Delivery* and rises when scope is added; the filled area is what's done. The gap between them is the
    remaining work: a gap that narrows is closing in, a gap that stays open or widens is losing ground to added
    scope. A dashed line shows how much of the scope is still estimated, from *Features* not yet broken down."
  - Predictability (`#predictability`): "How the forecast for this *Delivery* changed day by day. How Likely?
    shows the chance of finishing by the target date. The line's colour shows the level: Risky up to 50%,
    Realistic up to 70%, Confident up to 85%, Certain above that. When? shows the forecasted finish dates (50th
    to 95th percentile) against the target. Lines that rise mean the finish is slipping later; lines that fall
    mean it's pulling in."
  - Delivery Progress (`#delivery-progress-fever-chart`): "Where each *Feature* stands: how much of it is done,
    against how likely it is to be late. Green is on track, amber needs watching, red needs action. Press Run to
    replay how each *Feature* moved. One drifting up into red is using up its buffer faster than it's making
    progress."
  - Features over Time (`#features-over-time`): "Which *Feature* grew, and when. Each day is one bar, split into
    one band per *Feature* and sized by its *Work Items*. A band that suddenly grows is scope discovered inside
    that *Feature*. A hatched band is still an estimate rather than counted *Work Items*. The line, on the
    right-hand axis, counts how many *Features* the *Delivery* held."

### Popover structure (maintainer review of the running build, 2026-10-30)

The single paragraph read as "one big blob". Each popover now has three parts: a one-line purpose in body
text, then a **What you see** list, then a **How to read it** list, each under a small overline heading, and
Learn More at the bottom. The control is still `InfoPopoverButton`, with the lists passed as its children.
Bullets carry no trailing full stop. *Work Items*, *Feature(s)* and *Delivery* render the user's Terminology
words. Scope is the four delivery charts only; restructuring the flow-metric widget popovers would be a
story of its own.

- **Delivery Burnup**
  - Purpose: "Total scope against completed *Work Items* over time."
  - What you see: "Top line: everything in this *Delivery*; rises when scope is added" · "Filled area: the
    *Work Items* done" · "Dashed line: scope still estimated, from *Features* not yet broken down"
  - How to read it: "The gap between line and area is the remaining work" · "Gap narrowing: closing in" ·
    "Gap open or widening: losing ground to added scope"
- **Delivery Predictability**
  - Purpose: "How the forecast for this *Delivery* changed day by day."
  - What you see: "How Likely?: the chance of finishing by the target date, one point per day" · "Line
    colour: Risky up to 50%, Realistic up to 70%, Confident up to 85%, Certain above" · "When?: the
    forecasted finish dates (50th–95th percentile) against the target date"
  - How to read it: "How Likely? rising: the odds of making the target are improving" · "When? lines rising:
    the finish is slipping later; falling: pulling in" · "A step in the target line: the target was moved, so
    read jumps around it with that in mind"
- **Delivery Progress**
  - Purpose: "Where each *Feature* stands: how much is done, against how likely it is to be late."
  - What you see: "One dot per *Feature*: further right = more done, higher = more likely to be late" ·
    "Green, amber and red zones" · "Run replays how each *Feature* moved"
  - How to read it: "Green is on track, amber needs watching, red needs action" · "Drifting up into red: the
    *Feature* is using up its buffer faster than it is making progress" (reworded so no article sits before a renamed term)
- **Features over Time**
  - Purpose: "Which *Feature* grew, and when."
  - What you see: "One bar per day, split into a band per *Feature* and sized by its *Work Items*" ·
    "Hatched band: the size is still an estimate, not counted *Work Items*" · "The line (right-hand axis):
    how many *Features* the *Delivery* held"
  - How to read it: "A band that suddenly grows: scope discovered inside that *Feature*" · "A band going
    from hatched to solid: the *Feature* was broken down" · "A band that ends: the *Feature* left the
    *Delivery*"

The single-paragraph copy above is superseded.

## DELIVER

- **Slice 01** — `d43607cd7` feat, `a6492ee92` refactor (comments in plain words), `b64c8e4c8` tests + header wrap.
- **Adversarial review** (Haiku) — NEEDS_REVISION, then resolved:
  - Empty-state header tests, untested axis labels, internal-ref comments and the header wrap were fixed in
    `a6492ee92` and `b64c8e4c8`.
  - **Owed at finalize:** the docs heading rename to "Delivery Progress (Fever Chart)". Until it lands, the
    `#delivery-progress-fever-chart` Learn More anchor is dead. Also owed: the "how to read it" passages for
    Burnup and Predictability, dropping "coloured confidence bands", and regenerating the `delivery*.png`
    screenshots (the ⓘ now shows in every header).
  - Accepted: the `About <title>` names repeat when two Deliveries are open, the same as the existing
    `Enlarge <title>` buttons.
  - Open for the maintainer: chart titles hard-code "Delivery" while the popover text uses the Delivery term.
- **Mutation** — frontend 80.00 %; the 8 survivors are styling-only `sx` literals in `ChartTitle.tsx`. See
  `mutation/results.md`.
- **Rigor profile: `lean`** (maintainer, 2026-10-30) — one slice, Haiku crafter and acceptance designer, no
  nWave review. Project gates still run: `pnpm test`, `pnpm build`, frontend Stryker on the changed files.
