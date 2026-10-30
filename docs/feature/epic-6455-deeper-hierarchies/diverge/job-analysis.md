# Job Analysis — Epic 6455 "Deeper hierarchies"

Wave: DIVERGE, phase 1 (JTBD) · 2026-10-30 · builds on `../feature-delta.md` (DISCOVER) and the maintainer
decisions recorded at its end, which are binding here. Customers are anonymised as Customer A and Customer B.
Tracker words (Story, Epic, Change, Project, Program, Business Epic) are the customers' literal types; SAFe and
OKR words are framework vocabulary; Lighthouse concepts use the default terms (Work Item, Feature, Team,
Portfolio, Delivery).

## 1. Raw request

> ADO #6455: "Deeper hierarchies: portfolio items that are not direct parents of the team's items."
> Maintainer (2026-10-30): "flexibility in modelling, not a fix for one customer's hierarchy"; "If Lighthouse
> could model each org's own hierarchy as named levels (for example Team → Agile Release Train → Program),
> every level could be required to be visible, and no level would need to be jumped over."

Both are proposed solutions ("look two levels down", "named levels"). The job follows.

## 2. From request to job (5 Whys)

| Why? | Answer (source) |
|---|---|
| Why do portfolio items need Work Items that are not their direct children? | Because Customer A plans in Changes and Customer B in Business Epics, both two or more steps above the Stories their Teams work on (E1, E3) |
| Why is that a problem today? | Because a Feature only receives Work Items whose own parent is that Feature (`WorkItemService.cs:703`), so every Change is forecast from a default size, with a warning (H2, H3) |
| Why does a default-size forecast matter? | Because the date looks plausible but is a guess; Customer A could not show it to anyone and stopped (E1, journey step 3) |
| Why do they need a date at that level at all? | Because that level (Change, Business Epic, initiative, Program) is where the organisation makes and checks its promises; the Teams' items are too small to promise on, and the levels in between are bookkeeping to them (E2; maintainer answer: "they do not care about those items") |
| Why? | To keep the promises the organisation made, and to act early when one will not hold. A life-goal answer, so we stop one level below |

| Layer | Statement | Verdict |
|---|---|---|
| Tactical | "Let a Portfolio Feature pick up its grandchildren" | rejected: it is the request, and it fixes one depth |
| Operational | "Stop building one rule-based Delivery per Change by hand" (today's partial workaround) | rejected: a workflow fix, still Premium-only, no Change flow metrics, no Project roll-up |
| **Strategic** | **"Give a defensible answer to 'when does it land, and how is it flowing?' at every level where my organisation promises, whatever that organisation's structure is"** | **the job** |
| Physical | Attach each item to the level above it; compose the Teams' forecasts upwards level by level; read each level's own flow | irreducible function |

The physical function names three separable parts. Options in `options-raw.md` differ mainly in how they do
each part:
1. **Attach**: how an item is known to belong to an item one level up (parent link, field, link type, query).
2. **Compose**: how a level's "when" is built from what is below it (flatten all Work Items into the top
   level, or compose the forecasts of the level directly below).
3. **Read**: what each level answers on its own (its own states give cycle time, throughput and WIP).

## 3. Job statements

**Functional (delivery-forecaster, delivery-lead-rte).**
When my organisation plans and promises at levels above the items my Teams work on (a Change above Epics,
a Business Epic above Features above Epics, a Portfolio Epic, a Program, an Objective),
I want each of those levels to show when its items are likely to land, built from the Teams' real work, and
how that level is flowing,
so I can answer "when, and is it on track?" at the level the question is asked, without building it by hand.

**Functional (config-admin), the setup half of the same job.**
When I connect Lighthouse to a tracker whose structure has more levels than Work Item and Feature,
I want to describe our levels once, in our words, and see straight away whether every item reaches the level
above it,
so the forecasts are right from the first refresh and I do not discover a misconfiguration from a warning on
every Feature.

**Emotional.** From "every Change says no child Work Items were found; I cannot show these dates" (Customer A)
to "this is our structure, and the dates come from our Teams". For the config-admin: from "did I skip a level
without noticing?" to "Lighthouse told me which items do not reach the level above".

**Social.** Be the person whose top-level dates are computed from the Teams' history in the organisation's
own vocabulary, in front of a sponsor or steering group. For a prospect (Customer A): an evaluation that
succeeds instead of one that stops in onboarding.

## 4. Disruption check

Higher job: "the organisation trusts its delivery promises without asking". A tool cannot reach it alone, so
out of scope. A different disruption is in scope and shapes the options: **if the levels are modelled, "the
forecast level is two steps above the Teams' items" stops being a special case.** Under maintainer decision 4
(every item passes through every configured level) shape (a) becomes "Epics as Features, plus levels above
them", which is shape (b) with more levels. So one direction can serve both shapes; `options-raw.md` tests
which options do.

## 5. Outcome statements (ODI)

Each traces to the DISCOVER OST (`../feature-delta.md`, O1-O7). Scores are 1-10, carried from DISCOVER and
adjusted only where the maintainer's decisions changed them; they are the discoverer's and this agent's
judgement, not customer ratings.

| # | Outcome statement | From | Imp. | Sat. | Score | Status |
|---|---|---|---|---|---|---|
| OS1 | Minimize the time it takes to learn when an item at the promise level (Change, Business Epic) is likely to land, counting every Work Item beneath it | O1 | 9 | 1 | 17 | Under-served |
| OS2 | Minimize the likelihood of a promise-level forecast being built from a default size when real Work Items exist beneath it | O1, IA1 | 9 | 1 | 17 | Under-served |
| OS3 | Minimize the likelihood that an admin finishes setup with a level that no item reaches | O2 (shrunk by decision 4: skipping is not ours to solve, but noticing it is) | 7 | 2 | 12 | Appropriately served (borderline) |
| OS4 | Minimize the time it takes to see when a level above the promise level (Project, Program, initiative) lands | O3, O7 (merged, see 6) | 7 | 1 | 13 | Under-served |
| OS5 | Minimize the number of steps between the levels an organisation uses and the levels Lighthouse shows | maintainer frame, C8 | 7 | 2 | 12 | Appropriately served (borderline) |
| OS6 | Minimize the likelihood that a Team's simulated parallel work is counted at a level the Team does not work at | O4 | 6 | 2 | 10 | Appropriately served |
| OS7 | Minimize the time it takes to read cycle time, throughput and WIP at a level above the Feature | O5, E2 | 6 | 3 | 9 | Over-served today for the Feature level; under-served above it |

Notes:
- OS2 is listed apart from OS1 because the failure is worse than an empty answer (IA1): an option that shows
  a real forecast for some items and a silent default for others fails OS2 while passing OS1.
- OS3 importance drops from 8 to 7 because decision 4 makes skipping a level the Parent Override Field's job;
  what remains is that the admin *sees* it.
- OS6 is the guard on `TeamMetricsService.cs:899-908` and `SimulatedRun.cs:110-115`: the simulation's
  "Features at once" must stay at the level Teams actually work at.
- OS7 at the Feature level is already met (`PortfolioMetricsService.cs:276`). Above the Feature nothing is
  computed, so for a new level it is under-served.

## 6. Opportunity candidates

1. **OS1 + OS2 (17)**: the forecast at the promise level, from real Work Items. Every option must pass this.
2. **OS4 (13)**: a level above the promise level. DISCOVER asked whether Customer A's Project/Program roll-up
   (O3) and the shape (b) initiative view (O7) are one opportunity (OA5). **Answer here: yes, structurally.**
   Both are "a level whose 'when' is composed from the level below it". They differ only in which level is
   the lowest one forecast from Work Items, which is a configuration, not a different job.
3. **OS3 + OS5 (12)**: setup that names every level and shows which items do not reach the next one.
4. **OS6 (10)**: not an opportunity to build for, but a constraint every option must respect.

**Gate G1**: job at strategic level (and physical function stated): pass. No feature named in the job
statement: pass. ODI statements: 7 (minimum 3): pass.
