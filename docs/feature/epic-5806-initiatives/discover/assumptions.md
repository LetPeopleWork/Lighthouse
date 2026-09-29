# Assumptions — Epic 5806 "Visualize Initiatives"

**Scoring:** Risk = Impact×3 + Uncertainty×2 + Ease-of-test×1, each factor rated 1–3.
- Ease-of-test: 3 = months, 1 = days.
- More than 12 = test first. 8–12 = test soon. Below 8 = test later.

Evidence IDs (E#) refer to `problem-validation.md`. Everything below is desk research. No
assumption has been tested with a customer.

## Ranked

| # | Assumption | Cat. | I | U | E | **Risk** | Evidence for / against | Cheapest test |
|---|---|---|---|---|---|---|---|---|
| A1 | **This feature drives Premium purchases** (new licences or renewals that wouldn't happen otherwise) | Viability | 3 | 3 | 3 | **18** | Against: free tier is capped at 1 Portfolio (E9), so free users can't try a cross-Portfolio view. The flat price (E10) rules out expansion revenue. For: leadership is a real audience for Premium buyers (E8, grade B) | Ask the last 5 buyers and churned trials: "What made you buy / not buy?" Count any unprompted mention of strategy-level or Initiative reporting |
| A2 | **Executives open Lighthouse themselves** (vs. a Delivery Lead preparing a report for them) | Value / Usability | 3 | 3 | 2 | **17** | Against: forecasts are retyped into slides (E6, #4309). Some consumers never open Lighthouse (E7). No executive persona exists. For: none found | Ask 3–5 Premium customers: "Last leadership review — who looked at what, in which tool?" Check the Viewer-role counts on known Premium instances |
| A3 | **Customers' data actually has a populated parent above Feature** | Value / Feasibility | 3 | 3 | 1 | **16** | For: Linear models Initiatives natively (E3). A Jira DC customer invested in hierarchy (E2). Against: ServiceNow 0/94 (E4). Jira needs Premium/Plans hierarchy levels or a custom field for a level above Epic (our knowledge, C) | Count distinct non-null parent references per Portfolio on instances we can see (dev instance, demo data, 3 friendly customers). Nothing in-product measures parent use today (E5) |
| A4 | **The executive question is "when will each Initiative land?" (forecast), not "what is in flight / is it on track?" (status)** | Value | 3 | 3 | 1 | **16** | None either way. Flight Level 3 literature leans towards *balancing WIP and priorities across Initiatives*, which is neither | Put the question in interviews: "What decision did the last strategy review make, and what did it look at?" |
| A5 | **The existing tools are not enough** (Parent column per Portfolio, rule-based Delivery per parent, the Features page) | Value | 2 | 3 | 1 | **13** | The maintainer says "we can somewhat manage with deliveries" (E1, C). No complaint about X1/X3 found (GitHub issues are disabled on the repo) | GitHub issues are disabled, so no public trail. Walk one Premium customer through "one Delivery per Initiative" and time it |
| A6 | **Initiatives span several Portfolios** (otherwise the per-Portfolio Parent column already answers the question) | Value | 2 | 3 | 1 | **13** | Unknown. The Features page already lists Features that belong to several Portfolios (X4) | Same data count as A3: how many parents have children in 2+ Portfolios? |
| A7 | **One consistent parent type across Portfolios** (all "Initiative"), not a mix of Initiative, Objective, Theme or nothing | Usability | 2 | 3 | 1 | **11** | The Parent Override Field exists precisely because hierarchies differ (X5) | Same data count, grouped by the parent's work-item type |
| A8 | **A roll-up forecast per Initiative is meaningful** (the joint likelihood of its Features) | Feasibility / Value | 2 | 2 | 2 | **12** | Multi-team and joint-delivery rollups already exist (epic-5459, delivery-joint-likelihood) and could be reused | Technical spike: treat each parent as a synthetic Delivery |
| A9 | **"Snappy" works at instance scale** (every Initiative, Feature and Team on one page) | Feasibility | 2 | 2 | 1 | **9** | The Features page already renders the whole instance (X4) | Measure Features-page load time on the largest instance we can reach |
| A10 | **Premium gating won't cost goodwill** (free users see the parent today via X1/X2) | Viability | 2 | 2 | 2 | **12** | Gating something built on a free capability risks looking like a paywall on existing value | Keep X1/X2 free. Gate only the cross-Portfolio roll-up |

## Test-first set (Risk > 12)

A1, A2, A3, A4, A5 and A6. A3 and A6 share one cheap data count and should run first: if the
parent level is empty or never crosses Portfolios, A1, A2 and A4 stop mattering.

## Hypotheses for the top three

**A3 (data)**
- We believe Premium customers' Portfolios carry a populated parent above Feature.
- TRUE if at least 3 of 5 sampled instances have more than 50% of open Features with a resolvable
  parent, and at least one parent spans 2+ Portfolios.
- FALSE if 2 or fewer of 5 do.

**A2 (audience)**
- We believe the strategy-level reader opens Lighthouse directly.
- TRUE if at least 3 of 5 customers name an executive who has logged into Lighthouse in the last
  quarter.
- FALSE if the answer is "I send them a slide or export". In that case the solution is an
  *export or write-back artefact*, not a page.

**A1 (revenue)**
- We believe an Initiative view converts or retains Premium customers.
- TRUE if at least 2 of 5 recent buyers or evaluators mention strategy-level reporting unprompted,
  or a fake-door "Initiatives (Premium)" nav entry on the free tier gets clicked by at least 5% of
  active free instances within 30 days.
- FALSE if there are no unprompted mentions and clicks stay below 1%.
