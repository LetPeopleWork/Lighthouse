# Problem Validation — Epic 5806 "Visualize Initiatives"

Wave: DISCOVER (desk research only) · 2026-09-29

## Method and its limits — read this first

- **No customer interviews were run.** Zero Mom Test conversations, zero past-behaviour accounts
  heard first-hand. Gate G1 (5+ interviews, >60% confirming the pain) is **not met** and can't be
  met from a desk.
- **GitHub is not a source.** Checked 2026-09-29: issues are disabled on `LetPeopleWork/Lighthouse`,
  so there is no public issue trail. Customer demand lives in the maintainer's own conversations.
- Sources: product docs, personas, jobs, journeys, feature workspaces, connector docs and the
  frontend code under `/storage/repos/Lighthouse`.

**Evidence grades**

| Grade | Meaning |
|---|---|
| **A** | Past behaviour or observed data: something a customer *did*, or a fact about the product or customers' data |
| **B** | Stated opinion or request: something a customer *said*, first- or second-hand |
| **C** | Our own assumption: maintainer hypothesis or persona synthesis |

## Problem statement (the maintainer's words, not yet a customer's)

> "We currently have 2 levels … team … and Portfolio … Those cover Flight Level I and II. However,
> we may also get the 'parent' of a Feature … We can somewhat manage that with deliveries … but I
> wonder if we need something else (e.g. just a plain table view) of Initiatives (Flight Level III)."

Maintainer additions: the "executive/strategy" level is missing, Features may already have a parent
but little is done with it, the view should be "snappy" (an overview across teams and portfolios),
and it is meant as a **Premium** feature to lift licence sales.

**Current reading of the problem (hypothesis, grade C):** someone who owns strategy-level work
(Initiatives, Objectives, OKRs) can't see, in one fast place, how the Features under each
Initiative are progressing and when they are forecast to land, across every Portfolio and Team.
Today they either assemble that picture by hand or don't get it at all.

**Not validated:** who that person is, whether they open Lighthouse, what question they bring
(status, forecast date or trade-offs between Initiatives), and whether their data has the level.

## Who might have it

| Candidate | Existing persona | Fit |
|---|---|---|
| Delivery Lead / RTE preparing a leadership review | `delivery-lead-rte` (goal: "give leadership a one-glance answer") | Most likely *hands-on* user. Prepares the report |
| Delivery Forecaster reporting dates upward | `delivery-forecaster` (jobs: share a Delivery outside Lighthouse) | Likely user. Already exports for slides |
| Executive / portfolio owner (Flight Level 3) | **none**. No persona exists | The *audience* named in the Epic. No evidence they log in |
| Product Owner who never opens Lighthouse | `product-owner` (variant) | Shows that some consumers are reached only through other tools |

## What already exists (the "current solution" the problem must beat)

| # | Capability | Where | Premium? |
|---|---|---|---|
| X1 | **Parent column** on the Portfolio and Team Feature tables (link to the parent, or "No Parent"; not sortable, no filter). The earlier "Group Features by Parent" toggle was REMOVED on 2025-11-15 (afbefe4d9); `docs/portfolios/detail.md:43-63` still describes it and is stale | `FeatureListDataGrid/columns.tsx:246-261` | No |
| X2 | **Parent column** in the Feature grid (`createParentColumn`, `ParentWorkItemCell`, `useParentWorkItems`) | `Lighthouse.Frontend/src/components/Common/FeatureListDataGrid/columns.tsx:246` | No |
| X3 | **Rule-based Deliveries** can match on `ParentReferenceId`, so "one Delivery per Initiative" already works, with forecast, likelihood and metrics | `docs/portfolios/detail.md:143-167` | Yes |
| X4 | **Features page**: every Feature across all Portfolios, with membership and forecasted start/completion. No parent column or grouping found | `docs/features/features.md:9-11,74-79` | Partly (manual order) |
| X5 | **Parent Override Field** plus parent-from-issue-links, so the hierarchy can be read from custom fields or issue links | `docs/portfolios/edit.md:235-271` | No |
| X6 | **Delivery export** (copy/CSV of header plus Feature grid) for status reports | epic-5698 journey `share-a-delivery-outside-lighthouse` | Yes |

The Epic is therefore **not** "Lighthouse ignores the parent". It is "the parent is visible only
one Portfolio at a time, and there is no roll-up per Initiative across Portfolios except by building
a Delivery by hand for each one".

## Evidence table

| # | Evidence | Grade | Source | What it supports / undermines |
|---|---|---|---|---|
| E1 | The Epic text itself: "I wonder if we need something else" | C | ADO Epic 5806 | Hypothesis only. The author is unsure |
| E2 | A Jira DC customer (relayed by Steve) keeps its hierarchy in issue links; every Feature showed "no children" and was sized by default. Lighthouse built parent-from-issue-links in response | A (the customer's data) / B (relayed) | `docs/product/journeys/parent-from-issue-links.yaml:5-9` | Customers **do** invest in hierarchy, but this case is Feature→child, not Initiative→Feature |
| E3 | Linear's connector maps **Initiative → Parent Feature** natively | A (product fact) | `docs/concepts/worktrackingsystems/linear.md:32-43` | At least one tracker models Flight Level 3 first-class |
| E4 | ServiceNow: `task.parent` populated on **0 of 94** records; project/demand tables not exposed to reporting credentials | A (observed data) | `docs/concepts/worktrackingsystems/servicenow.md:275-278` | **Undermines** "data has a parent level" for at least one connector |
| E5 | The Parent column has shipped since 2025-11 (it replaced a Group-by-Parent toggle). **No usage event** records its use (the usage-data catalogue has Portfolio tab-opened and nothing for the parent) | A (fact) / unknown demand | `docs/settings/usagedata.md:41-49` | We can't tell whether anyone uses the parent today. Instrumenting it is cheap |
| E6 | #4309: forecasters **retype forecasts into slides**, hence Delivery export | A (past behaviour, reported) | epic-5698 journey `:105-116` | Upward reporting happens through **artefacts**, not logins. Weakens "execs open Lighthouse" |
| E7 | PO persona variant "never opens Lighthouse at all" and is reached only via write-back to the tracker (#5565) | B | `docs/product/personas/product-owner.yaml` | Same pattern: consumers above the team read other tools |
| E8 | Liz / JLP (Epic 4896): "premium/enterprise customers whose **leadership conversations** live or die on the credibility of the P85 date" | B | `docs/product/jobs.yaml:347,378-380` | Leadership is a real audience for Premium customers, but the ask was forecast honesty, not Initiatives |
| E9 | Free tier = **1 Portfolio, 3 Teams**. Anything spanning Portfolios is already reachable only with a licence | A (product fact) | `docs/licensing/licensing.md:116-118` | Free users can't *experience* a cross-Portfolio Initiative view, so it can't act as a conversion hook by trying it |
| E10 | The self-service price was changed to **one flat price** (CHF 2,000) | A (product fact) | `docs/product/journeys/pricing-adjustment-2026-08.yaml:74-90` | Flat licence: a new Premium feature earns money only through **new conversions or renewals**, never through expansion |
| E11 | The Delivery Lead persona's stated goal is a "one-glance answer" for leadership, and one of its jobs is "leadership review" | C (our persona synthesis) | `docs/product/personas/delivery-lead-rte.yaml` | Plausible user and job, but ungrounded |
| E12 | An in-app survey channel exists (nudges non-Premium users and asks role and team count) | A (channel exists) | `docs/product/journeys/lighthouse-user-survey.yaml` | A cheap way to ask "do you have a level above Features?". It reaches **free** users only |

**Weighing it up.** Nothing grade A shows anyone struggling *today* with a missing Initiative
view. Grade A evidence shows the hierarchy matters to customers (E2), that it is unevenly present
in their data (E3 vs E4), and that reporting upward happens through exported artefacts (E6). The
premium thesis meets two structural facts (E9, E10) that make a cross-Portfolio view a weak
*conversion* lever.

## Job sketch (JTBD, hypothesis)

> When I prepare the quarterly or monthly strategy review, I want to see every Initiative with its
> Features' progress and forecast across all Teams and Portfolios in one place, so I can tell
> leadership which strategic bets are on track and which need a decision, without rebuilding the
> picture by hand.

| Job step | Today | Gap (hypothesis) |
|---|---|---|
| Locate the Initiatives | The tracker, or the Parent column inside each Portfolio (X1) | No cross-Portfolio list of parents |
| Gather Features per Initiative | Rule-based Delivery per parent (X3), set up by hand | Manual, one per Initiative, Premium |
| Monitor progress and forecast | Delivery metrics, when a Delivery exists | No roll-up without a Delivery |
| Conclude / report | Export a Delivery (X6), then a slide | Several exports to stitch together |

## G1 status

| Criterion | Target | Actual | Result |
|---|---|---|---|
| Interviews | 5+ | 0 | **FAIL** |
| Confirmation rate | >60% | n/a | **FAIL** |
| Problem in the customer's words | yes | maintainer's words only | **FAIL** |
| 3+ concrete examples | 3 | 1 adjacent (E2) | **FAIL** |

Remediation is in `discover-summary.md`.
