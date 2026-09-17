<!-- markdownlint-disable MD024 -->
# Feature: epic-4127-sle-risk

ADO Epic: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4127> — "Show SLE Probability for In Progress Items" (Active as of 2026-09-16, tagged `Community; Productboard`, no child items before this wave).

Related to Story #5884 (work item age bands) — related only, not a parent. #5884 answers *how unusual is this item for the state it is in*; this Epic answers *is a commitment we published about to be broken*. Different question, different arithmetic, different history window. Nothing here is scoped into #5884 and nothing there is re-opened.

## Wave: DISCUSS / [REF] Pre-DISCUSS code reality check

Every claim below was read out of the code before any story was drafted.

**The SLE is already a first-class, configurable pair.** `WorkTrackingSystemOptionsOwner.cs:40-42` carries `ServiceLevelExpectationProbability` (validated 50–95 in the UI) and `ServiceLevelExpectationRange` (days), both defaulting to `0`. Team and Portfolio both inherit it. It round-trips through `SettingsOwnerDtoBase.cs:23-24`, `TeamExtensions.SyncServiceLevelExpectation` (`:83`) and `PortfolioExtensions.SyncServiceLevelExpectation` (`:105`). Users edit it in one click via `SleQuickSetting.tsx`, which treats `probability <= 0 || range <= 0` as unset and permits `0/0` explicitly.

**The SLE is already drawn on the Work Item Aging chart.** `WorkItemAgingChart.tsx:662-666` renders a `ReferenceLine` at `serviceLevelExpectation.value` days, labelled `${sleTerm}: ${percentile}% @ ${value} days or less`, gated behind the existing `sleVisible` chip from `useChartVisibility`. The same line exists on the Cycle Time Scatterplot (`CycleTimeScatterPlotChart.tsx:484-488`). So "is this item already past the target" is answered today; "is it about to be" is not.

**The SLE already drives population RAG.** `ragRules.ts:161-216` computes `percentageWithinSLE` over the window's cycle times and reports the gap against the declared probability in plain words ("Only 70.0% within the SLE target (80% @ 10 days) — 10.0pp below"). The *population* question is therefore already served; this feature is strictly the *per-item* question.

**The raw cycle times are already in hand on both sides.** Backend: `WorkItemBase.CycleTime(zone)` (`:62-74`) and `WorkItemBase.WorkItemAge(zone, today)` (`:86-98`) both count inclusive whole days via `GetDateDifference`, so an age and a cycle time are directly comparable numbers with no conversion. Frontend: `useMetricsData.ts:57` already exposes `cycleTimeData: T[]` (the window's closed items, fetched for the scatterplot) alongside `inProgressItems` (`:55`). The two counts this feature needs are computable from data that already exists on both sides — which is precisely why the single-source-of-truth decision (D5) matters.

**Per-item write-back already ships and already covers all work items.** `WriteBackTriggerService.ResolveWriteBackForTeam` (`:25-60`) loads every work item of the team and calls `ResolveWorkItemValue` (`:178-189`) per item per mapping; `WorkItemAgeCycleTime` already writes a raw number to every item on every team update. Premium-gated at `:33`. Adding a value source is an enum entry plus one `switch` arm plus the validator list in `WriteBackMappingValidator.cs:9-12`. `WriteBackTargetValueType` is `Date | FormattedText` only, so a percentage travels as text.

**Ownership multiplicity is asymmetric, and it settles the Epic's first open question.** `WorkItem.TeamId` is a single non-nullable `int` (`WorkItem.cs:18`) — a work item belongs to exactly one team, so team-level ambiguity **cannot occur** and needs no warning. `Feature.Portfolios` is a `List<Portfolio>` (`Feature.cs:65`) and each portfolio carries its own SLE and its own history — genuinely N answers for one feature, with one write-back field to put them in. That asymmetry is why D4 draws the scope line at teams.

**The dialog is the natural home and already receives the SLE.** `WorkItemsDialog` takes an `sle` prop (`ItemsInProgress.tsx:191` passes it) and already hosts the optional `ageBandColumn` descriptor from #5884 (`WorkItemsDialog.tsx:52, 99-102, 321-322`). A second optional column follows an established, working precedent. `DataGridColumn` already declares `valueGetter` and `sortComparator`, and `useDataGridExport` reads the column *value*, not the rendered cell — the same trap #5884 D12 documented.

**The Items In Progress card has exactly one chip slot.** `ItemsInProgress.tsx:148-172` lays out each row as `title | count | chip`, where the chip column is a fixed 44–80px box currently holding `Goal: N` when `idealWip` is set. A second chip has to share or replace that box; it cannot be added for free.

**The pace-band overlay is a boolean today.** `useShowPaceBands.ts:3` persists `workItemAgingPaceBandsEnabled` in `localStorage` as a string boolean, default OFF. `PACE_BAND_COLORS_LOW_TO_HIGH` (`WorkItemAgingChart.tsx:60`) is a five-entry low-to-high palette. Dots are already coloured by blocked state (`:303`) and by group (`:614`) — the dot colour channel is taken.

**The metrics view is range-driven.** Every percentile endpoint takes `startDate`/`endDate` (`TeamMetricsController.cs:190-202`), and per `widget-loose-ends` D3 Work Item Age is measured as of the **last day of the selected range**. Any display value here inherits that convention; the write-back value cannot, because it fires from a background update with no range picker. D6 records how those two callers stay honest.

## Wave: DISCUSS / [REF] Persona ID

**Primary and only**: `flow-coach` (`docs/product/personas/flow-coach.yaml`) — team lead, agile coach, scrum master or RTE running the standup or flow review where the intervention actually happens.

The persona file already carries the goal this feature serves almost verbatim: *"Spot work that's been sitting too long before it shows up as a missed forecast."* Today Lighthouse serves the second half of that sentence and not the first. No persona extension is needed beyond one mental-model line recording that this coach reasons about the SLE as a **promise with a deadline**, not as a statistical property of the population.

No secondary persona. `delivery-lead-rte` reads the same number at a wider scope, but portfolio scope is out (D4), so there is nothing for them in this feature yet.

## Wave: DISCUSS / [REF] JTBD one-liner

When I look at what is in flight during a standup, I want to know which items are **more likely than not to break the SLE we published** — while there is still time to do something about it — so I can spend the standup on those items instead of finding out at the retro that we missed.

**Job-id**: `job-flow-coach-act-before-sle-breach` (NEW — added to `docs/product/jobs.yaml` in this wave).

**Why this is a new job and not a refinement of an existing one.** The two nearby jobs are genuinely different questions:

- `job-flow-coach-spot-pace-outliers` (#5884, `aging-pace-percentiles`) asks *how unusual is this item for the state it is in* — a per-state comparison against the team's own distribution, with no commitment involved. An item can be perfectly ordinary and still be about to breach; an item can be a wild per-state outlier and finish comfortably inside the SLE.
- `job-flow-coach-gauge-wip-age-spread` (`work-item-age-percentiles`) asks *is my WIP as a whole aging healthily* — a population snapshot, not a per-item verdict.

This job's situation (a commitment exists and has a deadline), motivation (know the odds of breaking it on a specific item) and outcome (intervene before the breach rather than explain it after) are all distinct. It is the only job in the file whose trigger is a **published promise**.

## Wave: DISCUSS / [REF] The calculation

Locked by the user on 2026-09-16 after a worked comparison against the formula in the Epic description.

For an in-flight item of age `a`, against an SLE range of `R` days, over the closed items in the history window with cycle times `T`:

```
risk = count(T > R AND T >= a) / count(T >= a)
```

Read in plain words: *of every item that was still open on day `a`, what fraction went on to take longer than `R` days.*

- When `a <= R`, the intersection is just `T > R` (anything over `R` is necessarily at least `a`), so it simplifies to `count(T > R) / count(T >= a)`.
- When `a > R`, every item in the denominator already exceeds `R`, so the ratio is exactly `1`. **The Epic's scenario 3 falls out of the arithmetic and needs no special case.**
- `count(T >= a) == 0` → no completed item ever ran this long. There is no evidence to divide by, so the value is the sentinel `Beyond history`, never a fabricated `100%` (D9).

### Why not the formula as written in the Epic

`(100 − p) / (100 − percentile(a))` is the same conditional-survival identity, but it takes its numerator from the **declared** SLE probability and its denominator from the **empirical** distribution. Those agree only when the team is hitting its target exactly. Worked against a real-shaped 20-item history — cycle times `1 2 2 3 3 4 4 5 5 6 6 7 8 9 11 13 15 18 22 30`, SLE set to 80% @ 10 days, actual hit rate 70%:

| Item's age | Empirical (locked) | Epic's formula |
|---|---|---|
| 2 days | 6/19 = **32%** | 24% |
| 5 days | 6/13 = **46%** | 36% |
| 9 days | 6/7 = **86%** | 67% |
| 10 days | 6/6 = **100%** | 67%, patched to 100% by scenario 3 |

At day 9, six of the seven items that ever got that far breached. The declared form reads 67% because it keeps assuming the 80% promise is being kept. It also needs a clamp (it exceeds 100% whenever the item's percentile passes `p`), a divide-by-zero guard, and the hard-coded scenario-3 rule. The empirical form needs none of the three.

The `probability` half of the SLE setting is therefore **not consumed at the item level** (D2). It is a property of the population, and `ragRules.ts` already reports it there.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | Which formula? | **Empirical conditional** — `count(T > R AND T >= a) / count(T >= a)`. User-decided 2026-09-16 against the worked comparison above. |
| D2 | Is the SLE *probability* consumed per item? | **No.** Only `ServiceLevelExpectationRange` (the days) drives the number. The probability stays a population statement, already reported by `ragRules.ts`. Consequence: no "enable SLE" toggle is needed — `range > 0` *is* the enablement. |
| D3 | What when no SLE is set? | **Compute nothing, render nothing.** No derived default, no 85th-percentile fallback. A derived SLE makes the breach rate `100 − p` true by construction and collapses the metric into a restatement of the item's percentile — i.e. #5884's Age Band with a percent sign. User-decided: "this is a more advanced thing", and an owner without an SLE is an expected, acceptable, silent state. |
| D4 | Teams only, or portfolios too? | **Teams and work items only.** `Feature.Portfolios` is many-to-many with a per-portfolio SLE and a per-portfolio history, so one feature has N risks and write-back has one field. User-decided: out of scope, returns as its own feature if demand appears. Consequence: on portfolio detail pages the shared surfaces render exactly as today — same code path as "no SLE set". |
| D5 | Where does the number get computed? | **Once, in the backend domain, consumed by every surface.** The frontend *could* compute it from `cycleTimeData` + `inProgressItems`, but write-back must compute it server-side regardless, and two implementations of one rule drift — the exact failure #5884 D8 exists to prevent. A drift here is worse than #5884's: the disagreement would be between Lighthouse and the user's own tracker, across systems, with no way to see both at once. |
| D6 | Which history window? | **Whatever window the caller already uses.** Display surfaces pass the metrics view's `startDate`/`endDate`, so the number follows the range picker like every other value on the page. Write-back has no picker and uses the team's configured history, same as the rest of the update pipeline. One domain function, two callers, two windows — stated rather than discovered. |
| D7 | Which cycle-time definition? | **The default started→finished window only.** Named cycle-time definitions are out. The SLE is already anchored to the default window (`multiple-cycle-times` D11, which took the same decision to avoid shipping a false red under a named selection). |
| D8 | Boundary convention | Denominator `T >= a`, numerator `T > R AND T >= a`. An item at age `a` may still close today with cycle time `a`, so it belongs to the survivor set; an item that took exactly `R` days **met** the SLE and is not a breach. Both `WorkItemAge` and `CycleTime` count inclusive whole days via `GetDateDifference`, so the comparison needs no conversion. #5884 D8 records that boundary conventions are exactly where two surfaces silently start disagreeing. |
| D9 | Empty denominator | Sentinel **`Beyond history`**, muted and unpainted — deliberately not `100%` and not blank, mirroring #5884 D7's `No history`. An absence of evidence must not read as certainty. |
| D10 | What is it called? | **`${sle} Risk`** — "SLE Risk" on seeded defaults, via `TERMINOLOGY_KEYS.SLE`. Both `sle` and `serviceLevelExpectation` are renameable under Settings → Terminology, so the surface renders the user's own word. Never a literal "SLE" in markup. |
| D11 | What counts as "at risk" for the card chip? | **≥ 50%** — "more likely than not to breach". One explainable line rather than a tunable nobody sets. Not a setting in this Epic; if it needs to move, that is evidence for a follow-up, not a guess now. |
| D12 | How does the chart show it? | **A third mode of the existing background control**, not a second toggle: `Off / Pace percentiles / ${sle} Risk`. Pace bands are per-state-column because age-in-state is per-column; risk is end-to-end so it depends only on a dot's height, making it **full-width horizontal zones**. Both paint the same background channel, so they are mutually exclusive by construction — the same stance `work-item-age-percentiles` took for cycle-time vs age percentile lines on one chart. `workItemAgingPaceBandsEnabled` migrates from boolean to tri-state (`true → "pace"`, `false → "off"`). |
| D13 | Do the risk zones and the existing SLE line say the same thing twice? | **No — the line is the top zone's labelled lower edge.** The 100% zone begins exactly at `R` days, which is where the `sleVisible` reference line already sits. Keeping both is not duplication; the line supplies the label the zone boundary would otherwise need. |
| D14 | Palette | Reuse `PACE_BAND_COLORS_LOW_TO_HIGH` (`WorkItemAgingChart.tsx:60`). Five colours, low-to-high = good-to-bad in both features, so risk and pace speak one colour language and no second palette is invented. |
| D15 | Dot colouring | **Unchanged.** Dots already carry blocked state and group; recolouring them by risk would overload a channel that already means something. The background carries risk, exactly as it carries pace today. |
| D16 | Write-back shape | **Raw integer percent, every update.** New `WriteBackValueSource.SleRisk`, `WriteBackAppliesTo.Team` only, travelling as `FormattedText`. User-decided: "users chose if they wanna enable write back anyways." The known cost is recorded under Out-of-scope rather than mitigated. |

## Wave: DISCUSS / [REF] Scope assessment

**PASS with a note — right-sized at 4 stories across 4 slices, ~3.5 days.**

Against the oversize heuristics: 4 user stories (limit 10); two modules plus the write-back adapter (limit 3); zero new integration points (limit 5); ~3.5 days (limit 2 weeks). **One signal does fire** — the read surfaces and the write-back are independent user outcomes that could ship separately. That is answered by the slicing rather than by splitting the Epic: slice 04 is the write-back and can be dropped or deferred without touching slices 01–03. No split proposed.

## Wave: DISCUSS / [REF] User stories

All four carry `job_id: job-flow-coach-act-before-sle-breach`.

### US-01 — Read each in-flight item's SLE risk in the work item dialog

As a flow coach triaging what is in flight, I want each in-flight item's probability of breaching the SLE on its own row, so I can order the list by who is actually in trouble rather than by who is simply oldest.

#### Elevator Pitch

Before: the SLE line on the aging chart tells me which items have **already** breached; nothing tells me which are about to, so the intervention window is gone by the time I can see the problem.
After: open the Work Item Age widget's **View Data** dialog on a team with an SLE set → sees an `SLE Risk` column reading e.g. `86%` on each in-flight row, sortable worst-first.
Decision enabled: which two or three items get the standup's attention today, while they can still be saved.

#### Acceptance criteria

- AC-01.1 — On a team with `ServiceLevelExpectationRange > 0`, the View Data dialog on the Work Item Age widget renders an `SLE Risk` column with an integer percentage on every in-flight row.
- AC-01.2 — The value equals `count(T > R AND T >= a) / count(T >= a)` over the closed items in the **currently selected date range**, where `a` is the same `workItemAge` the row's age column shows and `R` is the team's configured SLE range.
- AC-01.3 — An item whose age exceeds the SLE range reads `100%`, with no special-case code path — it is the arithmetic result.
- AC-01.4 — An item older than every closed item in the window reads `Beyond history`, rendered muted and unpainted, and sorts off-scale.
- AC-01.5 — On a team with no SLE set (`range <= 0`), the column is **omitted entirely** and the dialog is otherwise unchanged.
- AC-01.6 — On a portfolio detail page the column is omitted, identically to the no-SLE case.
- AC-01.7 — The column sorts numerically by risk, not lexically by the rendered string; `Beyond history` sorts last in the descending (worst-first) direction.
- AC-01.8 — Export (CSV and clipboard) carries the rendered label — `86%` or `Beyond history` — not a bare ratio. Export stays premium-gated exactly as today.
- AC-01.9 — The column header reads `${sle} Risk`, rendering "SLE Risk" on seeded terminology and the user's own word when renamed.
- AC-01.10 — A column order persisted under `work-items-dialog` from before this feature shipped does not hide the new column.
- AC-01.11 — An item at an age exactly equal to a closed item's cycle time is counted in the denominator (`T >= a`); an item that took exactly `R` days is **not** counted as a breach (`T > R`).

### US-02 — See how many items are at risk without opening anything

As a flow coach opening the team page, I want the count of at-risk items on the Items In Progress card, so I know whether there is anything worth opening the dialog for.

#### Elevator Pitch

Before: nothing on the team page hints that any in-flight item is in trouble; I have to open the dialog to find out there was nothing to find.
After: open `/teams/{teamId}` → the Items In Progress card row shows a chip reading `3 at risk`, coloured by severity.
Decision enabled: whether this standup needs a triage conversation at all, before spending anyone's time on one.

#### Acceptance criteria

- AC-02.1 — On a team with an SLE set, each Items In Progress row shows a chip counting its items at ≥ 50% risk.
- AC-02.2 — The chip is absent when the count is zero, and absent when no SLE is set — the row renders exactly as today in both cases.
- AC-02.3 — Clicking the row opens the same dialog as today, with the `SLE Risk` column from US-01 present.
- AC-02.4 — The chip and the dialog column never disagree: both read the same computed value for the same range.
- AC-02.5 — The `Goal: N` chip continues to render when `idealWip` is set; the layout keeps the count column aligned whether one chip, both, or neither is present.
- AC-02.6 — `Beyond history` items are counted as at-risk, since an item beyond all history is not a safe item.

### US-03 — Read the risk as zones on the Work Item Aging chart

As a flow coach reading the aging chart, I want the background to show where the odds turn against an item, so I can see the risk gradient rather than only the deadline line.

#### Elevator Pitch

Before: the chart's SLE line marks the deadline, but everything below it looks equally safe — a dot one day short of the line reads the same as a dot on day one.
After: on `/teams/{teamId}`, switch the aging chart's background control to `${sle} Risk` → the chart paints five full-width horizontal zones, the top one beginning exactly at the SLE line.
Decision enabled: which part of the team's flow the coaching conversation belongs in — the dots clustered just under the 75% zone are the ones still worth saving.

#### Acceptance criteria

- AC-03.1 — The aging chart's background control offers three mutually exclusive modes: `Off`, `Pace percentiles`, `${sle} Risk`.
- AC-03.2 — In risk mode the chart paints full-width horizontal zones at the ages where risk crosses 25%, 50%, 75% and 100%, coloured from `PACE_BAND_COLORS_LOW_TO_HIGH`.
- AC-03.3 — The 100% zone's lower edge is exactly the SLE range in days — the same y-value as the existing `sleVisible` reference line.
- AC-03.4 — The `${sle} Risk` mode is unavailable (not merely empty) when no SLE is set.
- AC-03.5 — Dot colouring is unchanged: blocked state and group still own the dot colour.
- AC-03.6 — An existing `workItemAgingPaceBandsEnabled` value of `"true"` resolves to `Pace percentiles` and `"false"` to `Off`, so no user's chart changes appearance on upgrade.
- AC-03.7 — Selecting a mode persists it and it survives a reload.

### US-04 — Have the risk in my work tracking system

As a flow coach whose team lives in Jira or Azure DevOps, I want the SLE risk written onto the item itself, so the signal reaches people who never open Lighthouse.

#### Elevator Pitch

Before: the risk exists only inside Lighthouse, so it reaches only the people already looking at Lighthouse — not the ones working the board.
After: configure a write-back mapping with value source `${sle} Risk` on a Jira or Azure DevOps connection → after the next team update, the mapped field on each in-flight issue reads `86`.
Decision enabled: the team's own board filters and dashboards can sort and alert on breach risk without anyone opening Lighthouse.

#### Acceptance criteria

- AC-04.1 — `${sle} Risk` appears as a selectable write-back value source for `Team`-scoped mappings.
- AC-04.2 — It is **not** offered for `Portfolio`-scoped mappings (D4).
- AC-04.3 — On each team update, every in-flight work item with a computable risk receives the integer percentage as text (`86`, not `86%` and not `0.86`).
- AC-04.4 — Items with no computable risk — no SLE set, a closed item, or `Beyond history` — produce no write at all, following the existing `=> null` convention in `ResolveWorkItemValue`.
- AC-04.5 — The write-back value uses the team's configured history window and `clock.Today`, not any UI range.
- AC-04.6 — Write-back stays premium-gated and a resolution failure is swallowed and logged without cutting short the rest of the update, exactly as the existing sources behave.

## Wave: DISCUSS / [REF] Story map and slices

Backbone (the coach's activity, left to right): **open the team page → notice something is wrong → triage what is in flight → carry the signal to where the work happens.**

| Slice | Stories | ADO | Walking-skeleton value | Est. |
|---|---|---|---|---|
| [01 — the number exists and is readable](slices/slice-01-sle-risk-visible-in-dialog.md) | US-01 | [#6016](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6016) | A coach reads a per-item breach probability for the first time | ~1.5 d |
| [02 — at-risk count on the card](slices/slice-02-at-risk-count-on-card.md) | US-02 | [#6017](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6017) | The signal is visible without opening anything | ~0.5 d |
| [03 — risk zones on the aging chart](slices/slice-03-risk-zones-on-aging-chart.md) | US-03 | [#6014](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6014) | The gradient, not just the deadline | ~1 d |
| [04 — write-back](slices/slice-04-sle-risk-write-back.md) | US-04 | [#6015](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6015) | The signal reaches the board | ~0.5 d |

ADO IDs are not in slice order — #6014/#6015 were created before #6016/#6017 when a first pass tripped over quoting. Titles and descriptions carry the slice number; the mapping above is the one to trust.

**Walking skeleton**: slice 01 — it is the only slice that ships the domain calculation, and every later slice consumes it. Per Mandate 5 this is **strategy B** (thin end-to-end slice through existing structure): no new bounded context, no new integration, one new domain function surfaced through the existing metrics controller and the existing dialog contract.

### Slice taste tests

- **"Ships 4+ new components" → passes.** Slice 01 ships three: a domain calculation, a controller action, a dialog column. Slices 02–04 ship one each.
- **"Every slice depends on a new abstraction" → passes by construction.** Slices 02–04 all depend on slice 01's calculation, and slice 01 ships that abstraction **first, with user-visible value of its own**. The abstraction is never a slice on its own.
- **"No slice disproves a pre-commitment" → passes.** Slice 01's hypothesis targets the Epic's load-bearing assumption directly (see below), and its failure stops the other three.
- **"Synthetic data only" → passes.** Every slice carries a production-data acceptance criterion; slice 01's cannot be satisfied any other way.
- **"Two slices identical except for scale" → passes.** Four different surfaces, four different failure modes.

### The risk this feature is actually betting on

Slice 01's learning hypothesis is **small-sample instability**, and it is the reason the slicing is ordered this way. The denominator `count(T >= a)` shrinks as `a` grows — in the worked 20-item example it is already down to 7 items at day 9 and 6 at day 10. One closed item entering or leaving the window can move a displayed risk by 15 percentage points overnight, on exactly the items the coach is being told to care about most.

If real teams' numbers jump that hard, the metric is not decision-grade, and slices 02–04 would be three more surfaces for a number nobody trusts. That is a judgement to make on production data after slice 01, not a guess to make now — which is why slice 01 ships alone, to the dogfood instance, before anything is built on top of it.

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Measurement |
|---|---|---|
| `OUT-4127-risk-stability` | On the dogfood instance, ≤ 10% of in-flight items see their displayed risk move by more than 20 percentage points between consecutive daily updates, measured over 10 consecutive days after slice 01 ships | Daily capture of the endpoint response on the dogfood team; compare consecutive days per item. **Gates slices 02–04.** |
| `OUT-4127-early-warning` | ≥ 60% of items that eventually breach the SLE showed ≥ 50% risk at least 2 days before crossing the range, over 8 weeks post-release | Retrospective replay over closed items on the dogfood instance: reconstruct each item's risk trajectory from its age history and compare against its actual cycle time |
| `OUT-4127-column-used` | The `SLE Risk` column is sorted or filtered by ≥ 20% of users who open the Work Item Age dialog on a team with an SLE set, within 6 weeks of release | Existing opt-in usage telemetry (`epic-5733-opt-in-usage-data`) |
| `OUT-4127-risk-mode-used` | The chart's `${sle} Risk` background mode is selected by ≥ 15% of users who view a team detail page with an SLE set, within 6 weeks of release | Same telemetry; mirrors the `OUT-aging-pace-legend-toggled` bar set by `aging-pace-percentiles` |
| `OUT-4127-no-support-contradiction` | Zero reports of a card chip count disagreeing with the dialog column, or of a written-back tracker value disagreeing with the Lighthouse display for the same team and day | Community Slack and GitHub issues |

## Wave: DISCUSS / [REF] Out of scope

- **Portfolios and features** (D4). The shared surfaces render as today on portfolio pages; nothing regresses, nothing appears.
- **The SLE *probability* half as an item-level input** (D2). It stays a population statement, already reported by `ragRules.ts`.
- **Any derived or default SLE** (D3). No 85th-percentile fallback, no "enable SLE" toggle.
- **Named cycle-time definitions** (D7). Default started→finished only.
- **Per-state risk.** The SLE is an end-to-end promise, so the risk is end-to-end. The per-state question is #5884's and stays there.
- **A configurable at-risk threshold** (D11). Fixed at 50% for this Epic.
- **Persisting risk history.** The value is a pure function of age, history and the SLE range; nothing is stored, no domain event is raised, no new column is added. A trend of risk over time is a different feature with a different cost.
- **Write-back noise mitigation** (D16). The value moves for every in-flight item on every refresh, and per `quiet-jira-writeback` D1 Jira can suppress watcher *email* only — issue history, the `Updated` timestamp, webhooks, listeners and automation rules fire on every deployment regardless. Threshold-crossing writes were offered and declined: write-back is opt-in per mapping, so the choice is the user's. Recorded here so that a future noise report is recognised as this decision rather than as a new defect.
- **Notifications or alerts** on crossing a risk threshold. Nothing pushes; every surface is pull.

## Wave: DISCUSS / [REF] Driving ports

| Port | Surface | Slice |
|---|---|---|
| HTTP (inbound) | Team-scoped metrics read returning per-item risk for the selected range — shape (dedicated action vs. a field on the existing in-progress payload) is DESIGN's call | 01 |
| UI | `SLE Risk` column in `WorkItemsDialog` | 01 |
| UI | At-risk count chip on the Items In Progress card | 02 |
| UI | Tri-state background mode control on the Work Item Aging chart | 03 |
| Background (outbound) | `WriteBackValueSource.SleRisk` via the existing `WriteBackTriggerService` team path | 04 |

No CLI or MCP surface. The Lighthouse-Clients CLI and MCP server expose team metrics but not per-item in-flight detail; adding one would be a separate decision with its own versioning obligation, and nothing in the Epic asks for it.

## Wave: DISCUSS / [REF] Pre-requisites

All verified present in code during the reality check above; nothing is blocked.

- `ServiceLevelExpectationRange` on `Team`, already round-tripping through settings and editable via `SleQuickSetting` — **present**.
- `WorkItemBase.CycleTime` / `WorkItemAge` counting inclusive whole days through one helper — **present**.
- The optional-column precedent on `WorkItemsDialog` (`ageBandColumn`, #5884) — **present and shipped**.
- `PACE_BAND_COLORS_LOW_TO_HIGH` and the pace-band overlay toggle — **present**.
- The per-item team write-back path (`ResolveWriteBackForTeam` → `ResolveWorkItemValue`) — **present**.
- Opt-in usage telemetry for the two adoption KPIs — **present** (`epic-5733-opt-in-usage-data`).

No new dependency, no new NuGet or npm package, no migration. `WriteBackValueSource` gains an enum member, which is persisted by name in `WriteBackMappingDefinition` — DESIGN confirms whether that storage is by name or by ordinal before slice 04.

## Wave: DISCUSS / [REF] Definition of Done

1. All four stories' ACs pass as automated tests.
2. `dotnet build` zero warnings; `dotnet test` green with the live-connector categories excluded.
3. `pnpm test` green; `pnpm build` zero errors and zero warnings (implies a clean Biome check).
4. SonarQube Cloud introduces no new issues of any severity.
5. Stryker kill rate ≥ 80% on the new backend calculation and on the new frontend modules.
6. `docs/ci-learnings.md` rules pre-applied before the first line of code, not after the first red CI run.
7. Docs prose and per-feature screenshots updated at each slice's finalization, not batched into `/release`.
8. Every user-facing string renders through `TERMINOLOGY_KEYS`; no literal "SLE", "Service Level Expectation" or "Work Item" in markup.
9. Independent `nw-software-crafter-reviewer` pass per slice; never self-reviewed.

## Wave: DISCUSS / [REF] DoR validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value articulated | The SLE is the one commitment Lighthouse lets a team publish, and today it is only ever evaluated *after* the fact. Epic tagged `Community; Productboard`. |
| 2 | Job traceability | `job-flow-coach-act-before-sle-breach`, added to `docs/product/jobs.yaml` this wave; all four stories carry it. |
| 3 | Acceptance criteria testable | 29 ACs across four stories, each naming an observable surface, a concrete value, or an explicit absence. |
| 4 | Dependencies identified | Pre-requisites section; all six verified present in code, none outstanding. |
| 5 | Scope bounded | Out-of-scope section names eight explicit non-goals, each tied to a locked decision. |
| 6 | Sized for delivery | Four slices, each ≤ 1.5 days, each with its own brief and learning hypothesis. |
| 7 | Technical approach viable | Reality check read every touched file; the only structural change is one domain function plus one enum member. |
| 8 | Success measurable | Five outcome KPIs, each with a numeric target and a named measurement method. |
| 9 | Risks named with a mitigation | Small-sample instability is the load-bearing risk; mitigated by making it slice 01's learning hypothesis with `OUT-4127-risk-stability` as an explicit gate on slices 02–04. Write-back noise is named, quantified against `quiet-jira-writeback` D1, and accepted by decision rather than left undiscovered. |

Requirements completeness: **0.97** — every story has a job, an elevator pitch with a real entry point, testable ACs and a slice; the single open item is the endpoint shape under Driving ports, which is deliberately DESIGN's call rather than a gap.

## Wave: DISCUSS / [REF] Wave decisions summary

### Key decisions

- **[D1] Empirical conditional over the Epic's declared-probability formula** — the declared form mixes a committed target with an empirical distribution and misreports risk by roughly the amount the team is off target; it also needs a clamp, a divide-by-zero guard and a hard-coded scenario-3 rule that the empirical form makes unnecessary (see *The calculation*).
- **[D3] No SLE means no number** — a derived SLE makes the breach rate true by construction and collapses the metric into #5884's Age Band.
- **[D4] Teams only** — `Feature.Portfolios` is many-to-many with a per-portfolio SLE, so one feature would carry N risks and one write-back field.
- **[D5] One backend computation for every surface** — a drift between Lighthouse and the user's own tracker is worse than a drift between two Lighthouse surfaces.
- **[D12/D13] A third background mode, not a second toggle** — risk is end-to-end so it renders as full-width horizontal zones, and the existing SLE reference line is the top zone's labelled lower edge rather than a duplicate of it.

### Requirements summary

- Primary job: intervene on an in-flight item before it breaks the team's published SLE, rather than explaining the breach afterwards.
- Walking skeleton: slice 01 — the domain calculation surfaced as one column in the dialog that already hosts #5884's Age Band.
- Feature type: **cross-cutting** — backend domain calculation, HTTP read surface, three frontend surfaces, and one outbound adapter.

### Constraints established

- The SLE probability is never an item-level input; only the day range is (D2).
- One domain function, two callers, two history windows — display follows the range picker, write-back follows the team's configured history (D6).
- Portfolio surfaces take the same code path as "no SLE set" and render exactly as today (D4).
- The dot colour channel on the aging chart is already spoken for by blocked state and group, and stays that way (D15).

### Upstream changes

None. There is no DISCOVER or DIVERGE wave for this Epic — DISCUSS is the first wave, grounded in the Epic description and a read of the code rather than in prior wave artifacts. No prior assumption is contradicted.

---

# Wave: DESIGN — slice 01

Run 2026-09-16, application scope (`@nw-solution-architect`), interaction mode PROPOSE, against slice 01 only. Slices 02-04 are gated on `OUT-4127-risk-stability` and are not designed yet.

Wave-decision reconciliation: passed, 0 contradictions against DISCUSS. Two DISCUSS decisions are *confirmed by prior art* rather than merely upheld — see DDD-1 and DDD-5.

Full reasoning, alternatives and enforcement: [ADR-192](../../product/architecture/adr-192-sle-risk-as-a-pure-conditional-over-the-cycle-time-population.md).

## Wave: DESIGN / [REF] Correction to a DISCUSS statement

DISCUSS's driving-ports section said a new route needs no Lighthouse-Clients version gate. That conclusion is right and the reason given was not. Per ADR-065 §4, a **new endpoint 404s opaquely on an old server**, so any CLI or MCP wrapper over it must be version-gated with a `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry. No gate is owed here because DISCUSS put CLI and MCP out of scope and **no wrapper is being added** — not because the route is new. If a wrapper is ever added, the gate comes with it.

## Wave: DESIGN / [REF] DDD list

| ID | Decision | Verdict |
|---|---|---|
| DDD-1 | Where does the arithmetic live? | A **pure calculator owned by neither caller** — `SleRiskCalculator.Risk(ageInDays, targetRangeInDays, closedCycleTimes) -> int?`. No repository, no clock, no service provider. `TeamMetricsService` and (slice 04) `WriteBackTriggerService` both call it with different windows and identical semantics. The age is an **input**, never computed inside, so the as-of-date convention stays the caller's business. |
| DDD-2 | Does the standing "no shared service" chain (ADR-018/021/024) forbid DDD-1? | **No, and the distinction is the whole of ADR-018's reasoning.** ADR-018 refused to share between two consumers whose *identical signatures hid different inclusion rules*. Here the two consumers have the **same** rule and differ only in the window they pass — the case ADR-018 names as the right time to share, because "the consumers' EXACT semantic needs are known concretely". They are: they are the same. This is ADR-188's situation, not ADR-018's. |
| DDD-3 | `TeamMetricsService` or `BaseMetricsService`? | **`TeamMetricsService` only.** The base class is shared with the portfolio service, so a method there makes the portfolio twin a one-line addition. DISCUSS D4 excluded portfolios for a reason that does not expire (`Feature.Portfolios` is many-to-many with a per-portfolio target). A scope decision that costs one line to break is not a scope decision. |
| DDD-4 | Which closed items are the evidence? | **The identical expression `GetCycleTimePercentilesForTeam` already evaluates** — `GetWorkItemsClosedInDateRange(team, startDate, endDate)` → `CycleTime(Clock.Zone)` → `> 0`. Reused rather than re-selected, so the risk and the percentile lines on the same page cannot come to disagree about which work counts. |
| DDD-5 | Which items are the in-flight population? | **`GetWipSnapshotForTeam(team, endDate)`** — ADR-065's choice, and the same set behind `/metrics/wip` and the aging chart's dots. Slices 02 and 03 assume the dialog's rows and the chart's dots are the same items; this makes that true by construction rather than by coincidence. |
| DDD-6 | What crosses the wire? | `GET .../teams/{teamId:int}/metrics/sleRisk?startDate&endDate` → `IEnumerable<SleRiskDto>`, `SleRiskDto(string ReferenceId, int? Risk)`. **`ReferenceId` not `Id`**, because write-back addresses items by `ReferenceId` and slice 04 then needs no second join. **`Risk` nullable**, `null` = beyond history: the item stays listed and its answer is absent. Not `0`, not `100`, not an omitted entry. |
| DDD-7 | Why a new DTO? | `PercentileValue` models percentile → value and this is neither. Widening `WorkItemDto` changes a payload shared across many call sites and, per the ADR-062 family, obliges a client version gate for a field nobody asked for. |
| DDD-8 | What does a team with no target return? | **An empty collection** — not a list of nulls, not a 400. No promise exists, so nothing can be at risk of breaking one. The frontend's "omit the column" (AC-01.5) reads directly off that. |
| DDD-9 | Cache key | `SleRisk_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}` through the existing `GetFromCacheIfExists`. The key is **complete**: ages are already measured as-of the range's end (`ProjectStateAsOf`, and `widget-loose-ends` D16), so the answer is a function of `(team, startDate, endDate)` alone and `Clock.Today` does not enter the display path. |
| DDD-10 | Premium gate? RBAC change? | **Neither.** The read rides the existing class-level `[RbacGuard(TeamRead)]`, per ADR-065 §5. #5884's age bands — the nearest analogue — ship free. The premium boundary here is export and write-back, and slice 04 inherits the gate that already guards write-back. |
| DDD-11 | Frontend shape | `buildSleRiskColumnDescriptor` in `utils/charts/sleRisk.ts` → `SleRiskColumnDescriptor`, consumed through an optional prop exactly as `AgeBandColumnDescriptor` is (ADR-188). `labelFor` carries the column's **value** so the export gets `86%` and not a bare `86`; `riskFor` carries the **sort key** so ordering is numeric and not lexical. **No risk arithmetic in TypeScript** — the descriptor is built from the endpoint's response. |

## Wave: DESIGN / [REF] Component decomposition

| Component | Path | Change |
|---|---|---|
| `SleRiskCalculator` | `Lighthouse.Backend/Services/Implementation/Metrics/SleRiskCalculator.cs` | **CREATE NEW** — pure, static, no dependencies |
| `SleRiskDto` | `Lighthouse.Backend/Models/Metrics/SleRiskDto.cs` | **CREATE NEW** — two fields |
| `TeamMetricsService.GetSleRiskForTeam` | `Services/Implementation/TeamMetricsService.cs` | **EXTEND** — one method, reusing two existing selections and the existing cache |
| `ITeamMetricsService` | `Services/Interfaces/ITeamMetricsService.cs` | **EXTEND** — one signature |
| `TeamMetricsController.GetSleRisk` | `API/TeamMetricsController.cs` | **EXTEND** — one action under the existing class-level guard |
| `sleRisk.ts` | `Lighthouse.Frontend/src/utils/charts/sleRisk.ts` | **CREATE NEW** — descriptor factory + labels, mirroring `paceBands.ts` |
| `WorkItemsDialog` | `components/Common/WorkItemsDialog/WorkItemsDialog.tsx` | **EXTEND** — one optional prop, one column |
| `BaseMetricsView` | `pages/Common/MetricsView/BaseMetricsView.tsx` | **EXTEND** — fetch + descriptor construction at the existing call site |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `GetCycleTimePercentilesForTeam` | `TeamMetricsService.cs:310` | Selects the closed population and its cycle times over a window | **EXTEND (reuse the expression)** | DDD-4. The risk must read the same work the percentile lines read; a second selection is the only way for them to disagree |
| `GetWipSnapshotForTeam` | `TeamMetricsService.cs` | Selects the in-flight population as-of a date | **EXTEND (reuse)** | DDD-5, and ADR-065 already chose it for the same purpose |
| `GetFromCacheIfExists` | `BaseMetricsService.cs` | Per-entity keyed metrics cache | **EXTEND (reuse)** | Same idiom as every sibling read; no new cache flavour, which ADR-018 warns against |
| `BuildPercentiles` / `PercentileCalculator` | `BaseMetricsService.cs:349` | Percentile computation | **NEITHER — not applicable** | The risk is a conditional over counts, not a percentile. Forcing it through the percentile machinery would model it as something it is not |
| `AgeBandColumnDescriptor` + `paceBands.ts` | `utils/charts/paceBands.ts:217` | An optional dialog column fed by a descriptor | **CREATE NEW, deliberately parallel** | Same *shape*, different *question* — one is a per-state pace band, the other an end-to-end breach probability. ADR-188's note applies in reverse: folding two different questions behind one descriptor would hide the difference exactly where a reader meets both columns side by side |
| `WorkItemDto` | `API/DTO/` | Carries per-item values to the frontend | **CREATE NEW (`SleRiskDto`)** | DDD-7 — widening a shared payload obliges a client version gate for a field no client asked for |
| `WriteBackTriggerService.ResolveWorkItemValue` | `WriteBackTriggerService.cs:178` | Resolves a per-item value for write-back | **EXTEND — slice 04, not now** | The calculator exists from slice 01 so slice 04 adds one `switch` arm and no arithmetic |

Zero unjustified CREATE NEW decisions. The two new types are a pure function with no home in an existing class (DDD-1, and `protected` on `BaseMetricsService` is unreachable from write-back) and a two-field DTO.

## Wave: DESIGN / [REF] Driving ports

| Port | Surface | Guard |
|---|---|---|
| HTTP | `GET /api/{version}/teams/{teamId:int}/metrics/sleRisk?startDate&endDate` → `IEnumerable<SleRiskDto>` | Existing class-level `[RbacGuard(TeamRead)]`; shared `startDate.Date > endDate.Date ⇒ 400` guard |
| UI | The `${sle} Risk` column in `WorkItemsDialog` | none |

No portfolio route (DDD-3). No CLI, no MCP, therefore no version gate (see the correction above).

## Wave: DESIGN / [REF] Driven ports and adapters

| Port | Class | Adapter |
|---|---|---|
| `IWorkItemRepository` | Driven internal | Real, EF — reached only through the two existing selections, never queried directly |
| `Cache<string, object>` | Driven internal | Real, via `GetFromCacheIfExists` |
| `ILighthouseClock` | Driven external / non-deterministic | Supplies `Clock.Zone` for cycle-time arithmetic. **Not** the age source on the display path (DDD-9) |

No new outbound side effect in slice 01. Slice 04 adds the tracker write through the existing write-back adapter.

## Wave: DESIGN / [REF] Technology choices

Pinned by the existing stack; nothing new is introduced. Backend C# / .NET 10 ASP.NET Core, OOP with ports-and-adapters. Frontend React 18 + TypeScript. No new NuGet package, no new npm package, no EF migration, no persistence.

## Wave: DESIGN / [REF] Open questions — deferred to DELIVER

1. **Rounding.** `SleRiskCalculator` returns a whole percentage; whether that is `Math.Round` (banker's or away-from-zero) is left to DELIVER. The acceptance scenarios pin values that are unambiguous under any of them — 6/13 = 46.2%, 6/7 = 85.7%, 2/3 = 66.7% — so the choice is free and the tests will not silently encode one.
2. **Where `SleRiskCalculator.cs` sits.** `Services/Implementation/Metrics/` is proposed; if DELIVER finds no `Metrics/` folder there and no precedent for creating one, `Services/Implementation/` flat is acceptable. A pure type's folder is not an architectural decision.
3. **Whether the endpoint returns entries for in-flight items with an unusable age** (age ≤ 0, which `WorkItemAge` returns for an item whose started date is missing or in the future). Not reached by any slice-01 scenario. DELIVER should treat it as beyond-history (`null`) for consistency with DDD-6 rather than omit the row, and add a test when it does.

## Wave: DESIGN / [REF] Wave decisions summary

**Pattern**: modular monolith with ports-and-adapters — unchanged. **Paradigm**: OOP — unchanged.

**Key decisions**: a pure calculator shared by the read path and (from slice 04) write-back, reachable from both because it belongs to neither (DDD-1); the standing no-shared-service chain does not bind, because its objection was to hiding *different* inclusion rules behind one signature and these two are the *same* rule (DDD-2); the team service only, so the portfolio exclusion stays a decision rather than a one-line oversight (DDD-3); both populations reused from the methods that already select them, so the new number and the existing ones cannot drift apart (DDD-4, DDD-5).

**Constraints established**: no risk arithmetic in TypeScript; the age is always an input to the calculator, never computed inside it; `null` is the only representation of "no answer" on the wire; no premium gate and no RBAC surface on the read.

**Upstream changes**: none to DISCUSS decisions. One DISCUSS *rationale* corrected (the client version gate — see above); the conclusion it supported is unchanged.

---

---

# Wave: DISTILL — slice 01

Run 2026-09-16, against slice 01 only (ADO Story #6016). Slices 02-04 are gated on `OUT-4127-risk-stability` and are not distilled yet.

**Wave-decision reconciliation: passed, 0 contradictions.** DESIGN was authored after a first DISTILL pass and reconciled against it. Every contract the scenarios had pinned — `risk: null` for beyond history, `referenceId` as the join key, an empty collection for a team with no target, no portfolio route, the class-level `TeamRead` guard — DESIGN confirmed as DDD-6, DDD-8, DDD-3 and DDD-10. Reconciliation found **one gap, not a contradiction**, and it is fixed below.

**What reconciliation caught.** DDD-9 makes "the age is measured as of the range's end, not today" load-bearing: it is why the cache key `SleRisk_{start}_{end}` is complete. No scenario proved it, because the harness pins the instance clock to the same day the window ends — so today and the range end coincide everywhere and an implementation reading `Clock.Today` would have passed the whole file. `A_window_that_ended_in_the_past_is_answered_as_of_that_day` closes it: the same item reads 100% as of ten days ago and has no answer at all as of today.

## Wave: DISTILL / [REF] Scenario list

Backend, all in `Lighthouse.Backend.Tests/API/Integration/SleRisk/`, categories `acceptance` + `epic-4127-sle-risk` + `slice-01`.

| Scenario | Tags | AC |
|---|---|---|
| `A_team_with_a_target_is_told_each_open_item_s_chance_of_missing_it` | `@walking_skeleton @driving_port @real-io` | AC-01.1, AC-01.2 |
| `The_longer_an_item_stays_open_the_worse_its_chances_get` (×3 cases: 2d→32%, 9d→86%, 10d→100%) | `@driving_port @real-io` | AC-01.2 |
| `An_item_already_past_the_target_is_certain_to_have_missed_it` | `@driving_port @real-io` | AC-01.3 |
| `An_item_older_than_anything_ever_finished_is_given_no_answer` | `@driving_port @real-io @error` | AC-01.4 |
| `A_team_that_never_published_a_target_is_told_nothing_rather_than_zero` | `@driving_port @real-io @error` | AC-01.5 |
| `A_team_that_has_finished_nothing_yet_is_given_no_answer` | `@driving_port @real-io @error` | AC-01.2 |
| `An_item_that_finished_on_the_target_day_did_not_miss_the_target` | `@driving_port @real-io` | AC-01.11 |
| `An_item_as_old_as_a_finished_one_still_counts_that_one_among_its_survivors` | `@driving_port @real-io` | AC-01.11 |
| `Only_work_finished_inside_the_chosen_window_counts_as_evidence` | `@driving_port @real-io` | AC-01.2 |
| `A_window_that_ended_in_the_past_is_answered_as_of_that_day` | `@driving_port @real-io` | AC-01.2, DDD-9 |
| `Someone_who_may_not_see_the_team_may_not_see_what_is_at_risk_on_it` | `@driving_port @real-io @error` | DDD-10 |
| `Portfolios_are_not_asked_this_question_at_all` | `@driving_port @error` | AC-01.6 |

14 test cases across 12 methods. Error-path share: 5 of 12 methods.

Frontend, in `WorkItemsDialog.test.tsx` (the `SLE Risk column` block), rendered through the real component with a literal descriptor:

| Specification | AC |
|---|---|
| heads the column with the configured term and shows a whole-number percentage on every row | AC-01.1, AC-01.9 |
| says beyond history rather than a number for an item nothing can be compared against | AC-01.4 |
| paints the worst risks in the same colour the chart paints its worst zone | AC-01.1 |
| leaves an item with no answer muted and unpainted | AC-01.4 |
| shows no risk column at all for a team that published no target | AC-01.5 |
| stays visible for a coach whose saved column arrangement predates it | AC-01.10 |
| sits beside the band it belongs with when both are offered | AC-01.1 |
| orders by the risk itself, not by the spelling of the risk | AC-01.7 |
| never lets an item with no answer head a worst-first list | AC-01.7 |
| carries the percentage into the export, not the number behind it | AC-01.8 |
| does not take over which column the dialog opens sorted by | AC-01.1 |

**Not acceptance scenarios, and why:**

- **The sort's exact tie-breaking between two items on the same risk.** `100%` appears twice in the frontend fixture and the assertion pins both positions, but nothing pins *which* of the two leads. #5884's band column has a scenario for this because bands are coarse and ties are the norm; a continuous percentage ties rarely, and pinning an order the grid supplies would test MUI rather than us.
- **AC-01.6's frontend half** (the column absent on a portfolio page). The dialog cannot tell a portfolio from a team — it renders whatever descriptor it is handed, and DDD-3 means a portfolio page has none. The claim belongs to `BaseMetricsView`, which builds the descriptor, and lands with slice 01's wiring rather than as a dialog specification.

## Wave: DISTILL / [REF] Test placement

`API/Integration/SleRisk/` — a new folder mirroring `TaskManager/` and `BlockedItems/`, with `SleRiskAcceptanceTest` as the Epic-wide harness so slices 02-04 inherit it rather than each standing up its own host. `Slice01SleRiskReadScenarios.cs` + `Slice01SleRiskReadSpecifications.cs` partial-class split, per the project's existing convention.

The harness pins the instance clock through `FakeLighthouseClock`, per the ATDD policy row added during `epic-5511-task-manager` slice 03. An in-flight item's age is counted against whatever day the instance believes it is, so without a fixed instant every expected number in the file goes stale overnight rather than at a boundary anyone chose.

## Wave: DISTILL / [REF] Ports and doubles

| Port | Class | Treatment |
|---|---|---|
| `GET /api/latest/teams/{id}/metrics/sleRisk` | Driving | Real, over `Factory.CreateClient()` with `AsTeamAdmin` / `AsViewer` |
| `IWorkItemRepository`, `IRepository<Team>` | Driven internal | Real, EF over SQLite |
| `ILighthouseClock` | Driven external / non-deterministic | `FakeLighthouseClock` pinned to 2026-09-16 |
| Work tracking connectors | — | Not reached. This is a pure read over stored rows; nothing in slice 01 talks to a tracker |

No premium gate on the path, so no `ILicenseService` double is needed — which is itself a check on DDD-10: if the read ever acquires a licence check, these scenarios fail.

## Wave: DISTILL / [REF] Scaffolds

- `Lighthouse.Frontend/src/utils/charts/sleRisk.ts` — `SleRiskColumnDescriptor`, `SLE_RISK_BEYOND_HISTORY_LABEL`, the two wording helpers, and `buildSleRiskColumnDescriptor`, which throws with a `__SCAFFOLD__` marker. It throws rather than returning `undefined`, so a production call site that reaches it fails loudly instead of quietly agreeing that no team has a target.
- `WorkItemsDialog.tsx` — the optional `sleRiskColumn` prop on the props interface only. A skipped or failing Vitest test is still type-checked, so the prop has to exist before the suite can compile at all; the dialog ignores it, which is what makes the specifications fail on a missing column rather than on a missing prop.

**No backend scaffold.** Every backend scenario is driven over HTTP, so a missing route is a 404 and the assertion fails on the answer rather than on a compile error.

## Wave: DISTILL / [REF] Red gate

Measured first, then **marked skipped for the hand-off commit** — `[Ignore(...)]` on the backend fixture, `describe.skip` on the frontend block. The classification below is what they did before the markers went on; DELIVER removes each marker as it implements the behaviour beneath it. Without the markers every build between now and slice 01's implementation would report a missing route and a missing column as failures, and any unrelated red in that window would be harder to see.

Backend: **12 failed, 2 passed of 14**. Every failure is `Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK))` against a 404 — missing functionality, not a broken fixture, not an import error. The host stands up, the seeding runs, the assertion is reached.

Frontend: **10 failed, 63 passed of 73** in `WorkItemsDialog.test.tsx`. Every new failure is `unable to find an element with role columnheader and name /SLE Risk/`. The 62 pre-existing specifications are untouched and green.

**Two backend scenarios pass today, and neither is evidence of anything yet.** `Portfolios_are_not_asked_this_question_at_all` and `Someone_who_may_not_see_the_team_may_not_see_what_is_at_risk_on_it` both assert a refusal, and a route that does not exist refuses everything. They are guards that only start carrying weight the moment the route lands — at which point the first keeps DDD-3 honest and the second keeps DDD-10 honest. Recorded here so that "they were green from the start" is never read as "they were verified from the start". The same is true of the frontend's `shows no risk column at all for a team that published no target`.

## Wave: DISTILL / [REF] Outcomes registry

`OUT-2` — kind `specification`. Input shape: an in-flight item's age in days, the day range its team published as a target, and the cycle times of the work that team finished in a window. Output shape: the share of the work still open at that age which went on to exceed the target, as a whole percentage, or an explicit no-answer when nothing finished ever ran that long. Keywords: `sle`, `breach-probability`, `conditional`, `cycle-time`, `in-flight`.

No collision with `OUT-1` (#5884's pace-band classification): that one takes per-state percentiles and returns a band name for one state; this takes a flat cycle-time population and returns a probability against a published target. Different inputs, different output, different question — the `not_this_job` note on `job-flow-coach-act-before-sle-breach` is the same distinction stated for the backlog.

**Not yet written to `docs/product/outcomes/registry.yaml`** — the registry is maintained through `nwave-ai outcomes register`, which is not on PATH in this worktree. The row above is ready to register.

## Wave: DISTILL / [REF] Carried into DELIVER

0. **The markers come off one at a time.** `[Ignore(...)]` on `Slice01SleRiskReadTest` and `describe.skip` on the `SLE Risk column` block are the hand-off state, not the intended state. DELIVER removes each as the behaviour under it lands, and slice 01 is not done while either survives. A grep for `epic-4127` plus `Ignore` or `describe.skip` is the check.
1. **Rounding is free but must be chosen.** DESIGN open question 1: every pinned value is unambiguous under any rounding mode (6/13 = 46.2%, 6/7 = 85.7%, 2/3 = 66.7%), so the scenarios do not silently encode one. DELIVER picks and states it.
2. **Age ≤ 0 is untested.** DESIGN open question 3 — an item whose started date is missing or in the future gets age 1 from `WorkItemAge`, so it never actually reaches zero, but the calculator should still be total. DELIVER adds the test beside the implementation.
3. **`BaseMetricsView` wiring has no specification yet.** The fetch and the descriptor construction are slice-01 work with no acceptance test above them; the dialog specifications assume a descriptor arrives. The portfolio-page absence (AC-01.6's frontend half) belongs there too.

---

# Wave: DEVOPS — skipped

Skipped for this Epic on the user's explicit instruction, 2026-09-17. Recorded rather than left
silent, because a skipped wave otherwise gets improvised downstream.

What it would have decided, and why nothing is owed here: slice 01 adds one read route inside an
existing controller, under the guard that controller already carries. No new infrastructure, no new
dependency, no migration, no persistence, no new outbound call, no new secret, and no change to how
the app is built, deployed or observed. Slices 02 and 03 are frontend-only. **Slice 04 is not
covered by this skip** — it writes to a customer's tracker on every update, which is a production
behaviour with a blast radius, and it should have its own DEVOPS pass when it is unblocked.

# Wave: DELIVER — slice 01

Run 2026-09-17 against ADO Story #6016. All four hand-off items from DISTILL are closed, both red
markers are gone, and the gates below are the evidence.

## Wave: DELIVER / [REF] The four carried items

1. **The markers came off.** `[Ignore]` on `Slice01SleRiskReadTest` and `describe.skip` on the
   `SLE Risk column` block are both deleted. Neither `Ignore` nor `describe.skip` survives anywhere
   under `epic-4127`.
2. **Rounding chosen: away from zero** (`MidpointRounding.AwayFromZero`). A risk landing exactly
   between two whole percentages is reported as the worse of the two, because this number exists to
   decide which items get attention today and rounding half of them down is the wrong direction to
   be wrong in. Pinned by `Risk_HalfwayBetweenTwoWholePercentages_RoundsToTheWorseOne` (1/8 = 12.5%
   reads 13, not 12), which is the only value in the suite that distinguishes the two modes.
3. **Age ≤ 0 answers `null`**, per DESIGN open question 3. `AgeOnDay` returns 0 for an item whose
   start is missing or still ahead of the day being asked about; there is nothing to say about how
   long such an item has survived, and a 0 would read as the safest item on the board. Tested at 0
   and at a negative, so the calculator is total rather than merely lucky.
4. **`BaseMetricsView` wiring now has its own specifications** — four of them, including AC-01.6's
   frontend half. The portfolio one asserts that the portfolio metrics service cannot answer the
   question at all, so it fails the moment `getSleRisk` moves onto the shared interface. That is the
   one-line change DDD-3 says the scope decision has to survive.

## Wave: DELIVER / [REF] One decision DESIGN did not anticipate

**The cache key carries the target as well as the window.** DDD-9 called
`SleRisk_{start}_{end}` complete on the grounds that ages are measured as-of the range end, which is
true and is only half the question: the answer also depends on `ServiceLevelExpectationRange`, and
nothing invalidates the metrics cache when a team's settings are saved — `InvalidateTeamMetrics` is
reached by a refresh, a blackout change and the recording handlers, not by a settings write. Every
other cached metric on this service is a function of stored work alone, so this is the first one for
which that matters. A coach tightening the target in one click would otherwise have kept reading the
old odds until the next refresh, with nothing on screen saying the number predated the change.

The key is now `SleRisk_{start}_{end}_{range}`, and
`Tightening_the_target_changes_the_answer_rather_than_repeating_the_old_one` fails without it.

Found by the independent reviewer pass, not by the author.

## Wave: DELIVER / [REF] Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 errors, 0 warnings from this change |
| `dotnet test` (connector categories excluded) | 6972 passed, 1 environmental failure — the Defender SQLite file-lock on `ServiceProviderValidationTest`, not a regression |
| `dotnet format analyzers --severity info` | 0 findings in any file this change touches |
| `pnpm test` | 5239 passed, 371 files |
| `pnpm build` | clean, which implies a clean Biome check |
| Stryker backend | **100%** — see `mutation/results.md` |
| Stryker frontend | **94.12%** — 2 survivors, one equivalent mutant, documented |
| Independent review | `nw-software-crafter-reviewer`, rejected on the cache key above; re-reviewed after the fix |

## Wave: DELIVER / [REF] Finalization checklist

No silent N/A — every item gets an answer.

- **Docs prose** — done. `docs/metrics/flow-metrics.md` gains an **SLE Risk Column** section beside
  the Age Band one: what the number answers, where it comes from, the three consequences a reader
  will otherwise be surprised by (100% past the target, `Beyond history`, and meeting the target
  exactly), and a note that the risk consumes the SLE *range* and not its probability.
- **Per-feature screenshot** — **N/A, because** the sibling section this one sits beside carries
  none either. A dialog column is reached by a click, through a widget that already has its
  screenshot, and what it adds is words in a grid rather than a shape on a chart. Slice 03's chart
  zones will want one; this does not.
- **Demo data** — **N/A, because** the demo teams already carry an SLE, so the column appears on
  them with no seeding change. Nothing about the column needs a scenario that does not exist.
- **Lighthouse-Clients CLI/MCP versioning** — **N/A, because** DISCUSS put both out of scope and
  DESIGN's correction states the reason precisely: a gate would be owed if a wrapper were added, and
  none is. No `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry is due.
- **Website marketing surface** — **N/A, because** this is one column inside a dialog, gated on a
  stability measurement that has not been taken. It is not a headline until the Epic is whole.
- **Release Notes tag on #6016** — pending the user's call; the tag is the contract with
  `/release-notes` and is never added silently.
- **RBAC** — no change. The read rides the existing class-level `TeamRead` guard, and
  `Someone_who_may_not_see_the_team_may_not_see_what_is_at_risk_on_it` fails if it ever acquires its
  own.

## Wave: DELIVER / [REF] What slice 02 inherits

- `SleRiskCalculator` is reachable from anywhere and takes the age as an input, so the card chip
  (D11, ≥ 50%) and slice 04's write-back consume the same rule without a second implementation.
- The endpoint lists **every** in-flight item, including the ones it cannot answer for. AC-02.6
  counts a `Beyond history` item as at-risk, and the payload already carries it as `null` rather
  than omitting the row — no shape change needed.
- `OUT-4127-risk-stability` is still the gate on slices 02–04, and it is still unmeasured. It wants
  a real team on a production-restored instance, not this suite.

---
