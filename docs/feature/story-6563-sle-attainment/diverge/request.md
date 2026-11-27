# DIVERGE request: story-6563-sle-attainment

ADO Story #6563 "Make the SLE easier to find and compare with actual performance". No parent Epic.

## The requester's words

source: requester-verbatim (maintainer, ADO Story #6563 description; the customer's and the informant's names are
replaced with `[the customer]` and `[the informant]`, because customer identifiers are not committed to git)

> [the customer] could not find where the SLE is shown. [the informant] looked on the Flow Metrics tab and did not
> see it until he found the toggle. Benjamin agreed: "where is it actually? We should have that somewhere."
>
> The SLE is a goal, so the point is to see it next to how the Team actually performs, for example 72.7% of items
> within 10 days against a target of 70%.
>
> Raised by [the customer]
>
> - Make the SLE more prominent instead of hiding it behind chart toggles and a small header indicator.
> - See how actual performance against the SLE develops over time. [the informant]: tracking this over time is key.
> - Use wording like "or less" so it is clear to people new to flow.
>
> Details to be worked out when we start on it.
>
> Source: [the customer] Lighthouse workshop, 2026-11-06. Part 2, 36:48 to 42:30.

source: requester-verbatim (maintainer, chat turns 2026-11-27)

> next wave for story 6563

> yep go on

> commit and go on

source: requester-verbatim (maintainer, chat turn 2026-11-27, after reading the first draft's recommendation)

> so no new widget in the flow overview?

source: requester-verbatim (maintainer, chat turn 2026-11-27, during the revision of the first draft)

> yeah, just my gut feeling was: one new dedicated SLE widget in the overview, perhaps SLE over time in predictability. alternatively what it triggered was: what if we could show in the "top bar" some information about the team, like use the quick settings but also show: SLE and how good we are, System WIP Limit and current number of items in progress, etc. (essentially tweaking the quick settings to also give us information)

source: requester-verbatim (maintainer, chat turn 2026-11-27, right after the block above)

> but not sure if that gets too complicated, like it should then be configurable, perhaps I wanna see items at risk etc, so alsmot like a quick info bar that is freely configurable, and that for me is too much for now. then I'd rather have the widgets and people be able eto build reports...

source: requester-verbatim (maintainer, chat turn 2026-11-27, after being told that reports do not reuse dashboard widgets today; "my mix" is the gut feeling above: one new dedicated SLE widget in the overview, perhaps SLE over time in predictability)

> yes, but we plan on having flow metric reports so not an issue. i'd like my mix scored pls

## DISCOVER result

source: host-summary

- Job: `docs/product/jobs.yaml`, `job-flow-coach-see-whether-we-keep-our-sle`. VALIDATED at low-to-medium
  confidence: one informant from one organisation, at one workshop; the maintainer agreed.
- V1: the share of closed Work Items within the SLE is already computed in
  `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRulesTimeAndAge.ts` (`calculateSLEStats`), over the selected
  date range. A reader sees it only in the status-indicator tooltip of the Cycle Time Percentiles widget. No widget
  shows it as a number.
- V2: nothing shows the share over time. Percentiles Over Time (Epic #5427, `docs/metrics/predictability.md`) plots
  50/70/85/95 from a forward-only daily snapshot; it is the closest existing over-time pattern.
- V3: since 2026-11-14 the SLE line on the Cycle Time Scatterplot is drawn by default (after the workshop). On the
  Work Item Aging chart it still depends on the legend toggle. The header shows the SLE as a Quick Setting chip
  ("SLE: 70% of Work Items within 10 days or less").
- V4: "days or less" is already the wording of the header and the chart labels; the status-indicator tooltips say
  "@ 10 days" without it.
- C1: Story #2343 "SLE Trend?" was removed from the backlog on 2025-05-17 with no reason recorded.
- Adjacent, not this job: `job-flow-coach-act-before-sle-breach` (SLE Risk, per item in progress, before a breach).
- Open questions for DIVERGE: where the comparison lives (its own widget, a value on an existing widget, the
  header, elsewhere); how "over time" is computed (a rolling window per day, one value per closed period, or a
  persisted snapshot); how a small number of closed items is shown honestly.
- Constraints: SLE is a configurable term (Settings, Terminology), so copy uses the seeded default. Anything that
  shows data follows `.claude/skills/visualization/SKILL.md`. Teams and Portfolios both have an SLE.
- Scope (host's choice; the requester set none): six options as the skill asks, scored, one recommendation.
