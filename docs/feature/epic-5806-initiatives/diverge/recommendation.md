# Recommendation — Epic 5806 "Visualize Initiatives"

Wave: DIVERGE, handoff to DISCUSS · 2026-09-29

"Initiative" is this Epic's working title for **the level above Feature**: a SAFe Epic, an
Initiative, an Objective or OKR, a Theme, whatever the organisation uses, possibly a different
work-item type per Portfolio. In the UI it is a configurable term, written `{Parent}` below.

## Decision

**Build Option A: a Premium page listing every item of the level above Feature.** One table,
grouped by Portfolio. Each row shows when all its Features are 85% likely done and whether that
meets a target date. The row is computed from each Feature's parent, of any type, not a new object
people maintain. A scored 4.25, ahead of Features-page grouping (4.05) and the heatmap (3.80)
(`taste-evaluation.md`, weights re-locked after adding flexibility across org models).

Traceability: the job is "answer when and on-track for every strategic item, presentably"
(`job-analysis.md` §3). The most under-served outcomes are "which are at risk" (O3), "when" (O1) and
"one place" (O4). A is the only direction at 4 or 5 on every criterion. The roll-up reuses the joint
"all Features done" composition Deliveries already use, so it is correct and cheap (no new
simulation). It assumes nothing about SAFe or any other model.

## Precondition: the demo cannot show this today

Verified 2026-09-29: all five demo Portfolio CSVs have an empty `Parent` column, and the CSV
connector returns no parent details, so parents would appear as bare ids with no names. Since the
sale is an enterprise demo, **the first slice makes the demo carry named parents**, before any UI.

Two different gates, not to be confused:
- **Demo readiness** (slice 1) is a hard prerequisite for the sales story, and fully in our hands.
- **Customer data readiness** (DISCOVER assumption A3: do customers' Features carry a parent?) is
  unmeasured. It does **not** block building, because the page must degrade honestly when parents
  are sparse (the "N {features} have no {parent}" line). It **should** be measured before release
  copy and pricing claims are written. DISCUSS decides whether it becomes a release gate
  (open decision 7).

## v1: the thinnest shape

| Slice | Content | Answers |
|---|---|---|
| 1 Demo carries parents | Demo Portfolio CSVs get parents with names, deliberately of **different types** in different Portfolios (e.g. one Portfolio's parents typed as Epics, another's as Objectives): 2-3 per Portfolio, one spanning two Portfolios, one that cannot be forecast | Enables the demo; shows flexibility |
| 2 The page, "when" | New Premium nav entry. One row per parent, any type: name (tracker link), Portfolio(s), Features done/total, "likely by (85%)" date, or "cannot forecast: {team} has no throughput". Row expands to its Features with their own dates. Footer: "N {features} have no {parent}" | O1, O4, O6 |
| 3 "On track" | Target date per parent, set inline on the row (stored by parent reference). Likelihood that all Features land by it, and a status word: On track / At risk / Off track / No target | O3 |
| 4 Name | New configurable term (singular and plural) with a neutral seeded default. Used by the page title, nav, columns **and** the existing Parent column on Portfolio and Team pages, so one word covers the level everywhere | O7, Terminology rule |

Rules carried into DISCUSS:
- **Never sum or average percentiles.** The row's date and likelihood come from the joint composition
  over all its Features; a Feature row inside it stays marginal ("this one alone").
- **Never assume one parent type or one methodology.** Rows are keyed by parent reference; the
  tracker's type name may be shown as a secondary label, never used to filter parents in or out. No
  SAFe structure (portfolio Epics, PIs, ARTs) is modelled.
- One un-forecastable Feature makes the row "cannot forecast", and the row names why (same rule as
  Deliveries).
- Grouping by Portfolio is the default layout (maintainer: mostly one Portfolio). A parent whose
  Features span Portfolios appears once, in a "Spans {portfolios}" group, with all its Features in
  the forecast.
- Status words always sit next to colour, so the page reads correctly in print and to a colour-blind
  executive.
- Snappy: one request returns all rows, computed from stored forecasts; no simulation on page load.
- Premium boundary: the page and target dates are Premium. The Parent column stays free. Free
  instances see the nav entry with the usual Premium prompt.

## v1 screen sketch

`{Parents}`, `{Features}`, `{Portfolio}` stand for configurable terms; an organisation might see
"Epics", "Initiatives" or "Objectives" in their place.

```
 {Parents}                                                               Premium
 Every {parent}, when all its {features} are likely done, and whether that is in time.
 {Portfolio}: [ All         v ]    Show: [ Not done v ]           Forecasts updated 07:40

 {PARENT}                     {FEATURES}   LIKELY BY (85%)   TARGET        STATUS
 -- Project Ocean Explorer ---------------------------------------------------------------
 > Deep Sea Mapping           3/7 done     12 Dec 2026       30 Nov 2026   At risk   62%
 > Reef Restoration           1/4 done     20 Oct 2026       31 Dec 2026   On track  96%
 > Marine Tracking            0/2 done     Cannot forecast   15 Jan 2027   Team Zenith has
                                                                           no throughput
 -- Project Apollo -----------------------------------------------------------------------
 > Mars Colonization          0/2 done     02 Mar 2027       [Set target]  No target
 -- Spans Apollo, Orion ------------------------------------------------------------------
 v Stellar Navigation         2/5 done     18 Jan 2027       31 Jan 2027   On track  88%
     {FEATURE}               TEAMS          LIKELY BY (85%)   BY 31 JAN (this one alone)
     Star Chart Engine       Equinox        04 Nov 2026       99%
     Nav Beacon Relay        Pulsar, Zenith 18 Jan 2027       90%
     ...                     All 5 {features} by 31 Jan 2027: 88%

 14 {features} have no {parent}.
```

## Deferred (explicitly not v1)

| Deferred | Why not now | Where it would go |
|---|---|---|
| Heatmap / timeline view (Option E) | Largest build; add once the table is used | Later slice on the same page |
| Trend: "85% date moved +3 weeks since last month" | Needs a daily per-parent snapshot | Later slice; Delivery snapshot pattern exists |
| Target date read from the tracker | Connector work across Jira, ADO, Linear | See open decision 2 |
| Export / copy / PDF, scheduled brief | "Reports" is another Epic | Reports Epic |
| Alerts when a parent turns at risk | "Signals" is another Epic | Signals Epic |
| Levels above the parent (grandparents) | Only one level up is fetched today | Future Epic |
| Parent WIP, parent cycle time, share of work with no parent | Flight Level 3 metrics, beyond the two questions | Future Epic |

## Dissenting case: the heatmap (Option E)

The sale happens in a demo, and a board of strategic items across months, shaded by likelihood, is
the more memorable screen (no researched tool shows one with per-team probabilistic roll-up). If the
maintainer judges the table will not win the sponsor conversation, lead with E and accept a bigger
first slice and the risk that cumulative shading is read as "this month only". E is the natural
second view of the same rows, so A does not close it off. Runner-up by score, Features-page
grouping (4.05), wins only if the aim becomes validation rather than sales.

## Open decisions for DISCUSS

1. **Name of the level** (configurable term, decided in principle; default open). New Terminology
   key, singular and plural. **Recommended default: "Parent" / "Parents"**: methodology-neutral and
   already the word on Portfolio and Team pages. Alternatives: "Goal", "Strategic Item". Rejected as
   defaults: "Epic" (SAFe; a Feature-level item in Jira), "Initiative" (Jira, Linear, Flight Levels),
   "Objective" (OKR). Check that "Parents" reads well as an executive-facing page title.
2. **Where the target date comes from.** Entered in Lighthouse (recommended for v1, Premium), read
   from the tracker's parent (no typing, connector work, different field per tracker and type), or
   both with the tracker winning. Do parents ever carry their own target date in the model?
3. **Auto vs manual Deliveries.** Keep parents and Deliveries separate (recommended), or let
   "Track as a {delivery}" on a row create a rule-based Delivery (gaining notes, timeline, export)?
   A Delivery belongs to one Portfolio, which breaks parents that span two.
4. **Status thresholds.** Which likelihoods mean On track / At risk / Off track: reuse the bands
   Deliveries colour by, or new ones?
5. **What counts as "done" for a parent**: all current Features done, or the parent's own state in
   the tracker (whose state mapping differs by type)?
6. **CSV parents.** Should the CSV connector resolve parent names from rows in the same file? This
   decides whether slice 1 is a demo-only change or a product change for CSV users.
7. **Customer data readiness as a release gate?** DISCOVER's A3 is unmeasured. Recommended: build
   v1 without waiting, and run the data count (dev instance plus friendly Premium instances: share of
   Features with a parent, parent types per Portfolio) before release copy is written.
8. **Usage-data event** (for DEVOPS): for example, a name-only "parents page opened".

## Maintainer decisions (2026-09-29) — binding for DISCUSS

| Decision | Choice | Note |
|---|---|---|
| Direction | **A: new Premium page**, a table of every parent grouped by Portfolio | The heatmap (E) stays a candidate for a later Epic, not v1 |
| Default term for the level above Feature | **Initiative / Initiatives** | Overrides the "Parent" recommendation. A new Terminology key seeded with "Initiative", renamable like every other term. Docs and UI fallbacks use "Initiative" |
| Target date source | **Entered in Lighthouse** | Stored by Lighthouse per parent; no tracker field mapping in v1 |
| Automatic Deliveries per parent | **Keep separate** | The page reuses the completion-forecast calculation; Deliveries stay hand-made milestones |

Still open for DISCUSS to propose, with a recommendation each: On track / At risk / Off track
thresholds, what "done" means for an Initiative, whether the CSV connector resolves parent names,
whether customer data readiness gates the release. The usage-data event is decided in DEVOPS.
