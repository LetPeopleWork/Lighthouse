# Job Analysis — Epic 5806 "Visualize Initiatives"

Wave: DIVERGE, phase 1 (JTBD) · 2026-09-29 · builds on `../discover/` and the maintainer's answers
recorded at the bottom of `discover-summary.md`, which are binding.

"Initiative" below is only the Epic's working title for **the level above a Feature**. In SAFe's
Epic → Feature → Story, Lighthouse's Team level holds Stories and its Portfolio level holds Features,
so this is the SAFe Epic. But Lighthouse is deliberately **not SAFe-only**: the same level may be a
SAFe Epic, an Initiative, an Objective or OKR, a Theme, or whatever an organisation uses, and it
may be a different work-item type in different Portfolios. The product word is a configurable term
with a neutral default (see `recommendation.md`); "Epic" or "Initiative" never appears as a fixed
UI word.

## 1. Raw request

> "We have two levels, Team and Portfolio (Flight Level I and II). We may also get the parent of a
> Feature … we can somewhat manage that with Deliveries, but maybe we need something else, e.g. a
> plain table view of Initiatives (Flight Level III)." It should be snappy, an overview across Teams
> and Portfolios, and Premium. (A "plain table view" is a proposed solution; the job follows.)

## 2. From request to job

| Why? | Answer (source) |
|---|---|
| Why a table of Initiatives? | Because the level above Feature is visible only as a Parent column, one Portfolio at a time (DISCOVER, capability X1) |
| Why is that not enough? | Because the question asked about an Initiative is about *all its Features together*: when do they all land, and is that in time. The column answers neither |
| Why is that question asked? | Because the RTE / Delivery Lead has to answer leadership about strategic bets, and today assembles it by hand: one Delivery per Initiative, or a slide (DISCOVER E6, X3) |
| Why does leadership ask? | To decide whether a strategic commitment still holds, and to act early if it does not |
| Why? | To keep promises they made further up. A life-goal answer, so we stop one level below |

| Layer | Statement | Verdict |
|---|---|---|
| Tactical | "A table listing parent items" | rejected, it is the request |
| Operational | "Stop building one Delivery per Initiative by hand" | rejected, a workflow fix |
| **Strategic** | **"Give a defensible answer to 'when will each strategic bet land, and is it on track?' at a moment's notice"** | **the job** |
| Physical | Group work by the commitment it serves, compose the forecasts of its parts, compare with the promise | irreducible function |

Disruption check: the higher job is "leadership trusts the delivery system without asking". Not
reachable by a view; out of scope. The maintainer placed "Signals" (push alerts) and "Reports"
(scheduled artefacts) in other Epics, so this Epic is the *pull* answer only.

## 3. Job story

> **When** leadership, a sponsor or a steering meeting asks about our strategic Initiatives,
> **I want to** see every Initiative in one place with when all of its Features are likely to be done
> and whether that is in time,
> **so I can** answer on the spot with a number I can defend, and show the same screen to the
> executive without cleaning it up first.

Persona: `delivery-lead-rte` (goal: "give leadership a one-glance answer"). The executive is the
**audience**, not the user (maintainer answer).

| Dimension | Statement |
|---|---|
| Functional | Per Initiative: its Features, how many are done, the date by which all of them are likely done, and whether that meets the promised date. All Initiatives on one screen, grouped by the Portfolio they live in |
| Emotional | From "I will get back to you after I rebuild the spreadsheet" to "here it is, 85% by 12 December, and here is why it is at risk" |
| Social | Be the person whose Initiative dates are *computed from the teams' real history*, not typed; the page itself is presentable to an executive |

The job is the same whatever the level is called: a SAFe portfolio's Epics, a Flight Levels
Initiative board and an OKR set all ask "when, and in time?" of the Features beneath them. Scope
(maintainer): only "when?" and "on track?"; mostly one Portfolio per item, several must not break;
Premium, sold through enterprise demos and sponsors.

## 4. Four forces

| Force | Content |
|---|---|
| **Push** (today hurts) | The Parent column is per Portfolio, not sortable, not filterable. "One Delivery per Initiative" works but is built and maintained by hand, and lists them among real releases. Executives ask about Initiatives, not Features |
| **Pull** (new is attractive) | One screen, every Initiative, a joint date that is correct by construction, presentable as-is. For sales: a Flight Level 3 answer none of the 17 tools researched gives probabilistically per team (`competitive-research.md`, gap G1) |
| **Anxiety** (fear of new) | The level may be empty or inconsistent in the customer's data (DISCOVER A3, still unmeasured). A red "cannot forecast" row in front of an executive. Executives anchoring on one number. What the level is *called* on screen: an OKR shop shown "Epics", or a SAFe shop shown "Initiatives", reads the tool as built for someone else |
| **Habit** (old is comfortable) | Slides; the tracker's own roadmap (Jira Plans, ADO Delivery Plans, Linear's Initiative health); manual Deliveries that already work |

## 5. ODI outcome statements

| # | Outcome |
|---|---|
| O1 | Minimize the time it takes to state when all Features of a given Initiative are likely to be done |
| O2 | Minimize the likelihood of reporting an Initiative date that treats some of its Features as already certain |
| O3 | Minimize the time it takes to identify which Initiatives are unlikely to meet their promised date |
| O4 | Minimize the number of places the lead must visit to see every Initiative's status |
| O5 | Minimize the effort required to make the Initiative overview presentable to an executive |
| O6 | Minimize the likelihood of an Initiative silently missing from the overview because its data is incomplete |
| O7 | Minimize the likelihood that the overview names the organisation's strategic level with another methodology's word |

## 6. Opportunity candidates

Estimates reasoned from DISCOVER evidence and the maintainer's answers. **No survey exists; do not
quote these as measured.**

| Outcome | Imp. | Sat. | Score | Status | Basis |
|---|---|---|---|---|---|
| O1 when per Initiative | 9 | 3 | 15 | Under-served | Needs a hand-built Delivery today |
| O2 joint, not optimistic | 8 | 4 | 12 | Served *inside* Deliveries only | Joint likelihood already exists for Deliveries (`job-delivery-likelihood-covers-every-feature`) |
| O3 which are at risk | 9 | 2 | 16 | Under-served | No per-Initiative target anywhere; parents carry no target date in the model |
| O4 one place | 8 | 2 | 14 | Under-served | Parent column is per Portfolio |
| O5 presentable | 7 | 3 | 11 | Borderline | Delivery export exists, per Delivery |
| O6 nothing silently missing | 6 | 4 | 8 | Low, but a trust guard | Mirrors the "unmapped item vanishes silently" lesson |
| O7 their own word | 7 | 3 | 11 | Borderline, a constraint | The UI says "Parent" today; nothing renames it |

Brainstorm against O3, O1 and O4. O2 is a constraint (do not add percentiles; reuse the joint
composition), not an opportunity.

## 7. Facts that constrain every option (verified in code, 2026-09-29)

- A Feature has one parent reference. Parents are stored as Feature rows flagged as parents, only
  one level up, not attached to any Portfolio. **Nothing constrains the parent's type**: each
  connector resolves it its own way (Jira parent or link, ADO parent, Linear initiative), so one
  instance can hold SAFe Epics in one Portfolio and Objectives in another. No option may assume a
  single type.
- **Parents carry no target date.** "On track" therefore needs a source for the promise.
- The joint "all these Features done" distribution already exists and takes any list of Features
  (`Models/Forecast/DeliveryCompletionForecast.cs`, used by Deliveries): per-team minimum within a
  team, product across teams, "cannot forecast" if any Feature cannot be forecast. No new Monte Carlo
  is needed; the roll-up composes stored forecasts, which is what makes "snappy" cheap.
- **Demo data has no Initiatives.** All five demo Portfolio CSVs have an empty `Parent` column, and
  the CSV connector returns no parent details at all (`CsvWorkTrackingConnector.GetParentFeaturesDetails`
  returns an empty list), so even with values filled in, parents would show as bare ids without
  names. The enterprise demo cannot show this feature until both change.
- There is no configurable term for this level (`TerminologySeeder` seeds Feature, Portfolio,
  Delivery and others, nothing above Feature).
