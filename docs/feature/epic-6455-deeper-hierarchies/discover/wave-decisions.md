# DISCOVER Wave Decisions — epic-6455-deeper-hierarchies

2026-10-30. Narrative, evidence table and citations: `../feature-delta.md`. Customers are anonymised as
Customer A (Jira Cloud; Story ← Epic ← Change ← Project ← optional Program) and Customer B (Jira;
Story ← Epic ← Feature ← Business Epic).

## Decisions

**[D1] Two problem shapes, kept apart.** Shape (a): the forecast level sits two or more steps above the
Team's Work Items, so its Features get no Work Items. Shape (b): Features map cleanly, but the strategic
level above them (Flight Level III) is missing.
Rationale: different evidence, different persona, different failure (a wrong forecast vs. a missing view).
Merging them would let (b)'s weaker evidence ride on (a)'s.
Source: maintainer, 2026-10-30.

**[D2] Conditional GO to DIVERGE for shape (a); shape (b) recorded as its own opportunity (O7).**
Rationale: G1 count is not met (2 of 5), but the failure is a structural certainty in the code
(`WorkItemService.cs:703`), observed in two real instances. Prevalence and the right fix are what remain
uncertain, and DIVERGE builds nothing. Conditions before DISCUSS: OA1 (uniform depth) and OA3 (prevalence)
answered; Customer B asked about their needs.
Source: E1, E3, E6.

**[D3] The solution must be a Lighthouse concept, not tracker-specific.** Jira, Azure DevOps, Linear and
CSV in scope; ServiceNow must not be precluded.
Source: maintainer, 2026-10-30.

**[D4] Relay level, extra levels, configurable N levels, combinations, and a query-defined scope go to
DIVERGE as candidate directions.**
No direction is recommended here.
Rationale: weighing them is DIVERGE's job; each carries open questions listed in the feature delta.
Source: ADO #6455; Epic 5806 competitive research (query-scoped forecasting tools).

**[D5] "Forecasts are empty" is corrected to "forecasts are default-size guesses with a warning".**
Rationale: a Feature with no matched Work Items is extrapolated (`Models/Portfolio.cs:190-193`,
`WorkItemService.cs:726-755`); the warning text is `featureWarningSentences.ts:36`. This is worse than
empty because the date looks plausible.
Source: code.

**[D6] Making a skipped level visible at setup is part of the problem, not a nice-to-have (O2).**
Source: E1 (stopped), E3 (struggling), docs say nothing about direct children except for CSV.

**[D7] Epic 5806/6119 is reused selectively.** D5, D9, D12, D13, D17 hold; D14 is invalidated for (a); D4
and D8 are open; the page, RBAC and status decisions are parked with (b). The 5806 NO-GO is partly reversed
for (b): demand is now maintainer-reported from customers, still unnamed.
Source: `../feature-delta.md` reuse section.

**[D8] All workspace files are anonymised.** No company, person or host names.
Source: coordinator, 2026-10-30.

**[D9] The aim is flexible modelling on clear Lighthouse concepts, not one customer's hierarchy.**
Lighthouse defines what a level is, what it rolls up and what it can answer (forecast, flow metrics,
roll-up); organisations configure their structure on that. Test cases: Customer A, Customer B, SAFe (optional
Capability, Themes as links), OKR-style (goal levels, many-to-many links), and today's two-level setup
unchanged. DIVERGE answers the eight structure questions in `../feature-delta.md`.
Rationale: today Lighthouse is flexible about which tracker type plays which role and about naming, but
fixed at two roles joined by one direct parent (inventory). Only Customers A and B are customer evidence;
SAFe and OKR are framework knowledge plus maintainer direction.
Source: maintainer, 2026-10-30.

## Constraints

| # | Constraint | Evidence source |
|---|---|---|
| C1 | Lighthouse concept across Jira, Azure DevOps, Linear, CSV; ServiceNow not precluded | Maintainer, 2026-10-30 |
| C2 | Nothing goes silently missing; a skipped level is visible | Epic 5806 D13; E1, E3 |
| C3 | Parent resolution stays in one pure selector owned by the Team/Portfolio; connectors only read | `ARCHITECTURE.md:63`; `ParentSourceSelector.cs` |
| C4 | Any new level has a configurable term | Epic 5806 D9; project terminology rule |
| C5 | One-level Portfolios behave exactly as today | Installed base |
| C6 | Expand-only migrations | Project rule |
| C7 | No customer identifiers in committed files | Coordinator, 2026-10-30 |
| C8 | Tracker-agnostic Lighthouse concept; org structure is configured, not hard-coded. Customer A, Customer B, SAFe (optional Capability), OKR-style and the two-level setup must be expressible | Maintainer, 2026-10-30 |

## Validated assumptions

| # | Assumption | Confidence | Evidence |
|---|---|---|---|
| VA1 | Shape (a) makes the Portfolio's forecasts unusable | High | Code (E6); E1; E3 |
| VA2 | Shape (a) occurs at real customers with different vocabularies | Medium | E1, E3; more maintainer-reported (E4) |
| VA3 | Shape (a) is an onboarding failure | Medium | E1, E3; setup docs silent |
| VA4 | Nothing in Lighthouse reaches two levels up on Jira, Azure DevOps or CSV | High | Inventory H2, H5 |
| VA5 | The customers' trackers hold the full chain | Medium | E1 explicit, E3 implied |
| VA6 | Shape (b) exists at customers | Low-Medium | E5, maintainer-reported, unnamed |

## Invalidated assumptions

| # | Was | Evidence |
|---|---|---|
| IA1 | Forecasts are empty | Default-size extrapolation (D5) |
| IA2 | Epic 5806 D14 "one level up only" suffices | Shape (a) is the step beyond one level |
| IA3 | The problem is a page for the level above Feature | Customer evidence points first at configuration and forecasting depth |
| IA4 | Change-level cycle time and throughput are blocked by depth | They read the Features' own states (`PortfolioMetricsService.cs:276`); only Team Feature WIP counts direct parents (`TeamMetricsService.cs:899-908`) |

## Open assumptions (risk-scored, highest first)

OA1 uniform depth (16) · OA2 forecasting through un-broken-down intermediate items is meaningful (15) ·
OA3 shape (a) recurs beyond two customers (14) · OA4 one concept serves every connector (14) · OA5 the level
above the forecast level is one opportunity for (a) and (b) (14) · OA6 the intermediate level is only looked
through (13) · OA7 fetching it is affordable (9). Details and cheapest tests in `../feature-delta.md`.

## Maintainer answers (2026-10-30, after review)

- OA3: four customers have shown shape (a) or (b); in the shape (a) cases they skip the intermediate level because they do not care about it. Maintainer-reported, unnamed. (see: ../feature-delta.md, Maintainer answers)
- OA1: validated for Customer A. Every Story reaches a Change through an Epic, never directly.
- Customer B's top-level need: session on 2026-11-02; still a condition before DISCUSS.
- Maintainer direction for DIVERGE: model each org's hierarchy as named levels (e.g. Team → Agile Release Train → Program) with every level visible, so no level needs to be jumped. This is a preference to weigh, not a decision.

**Maintainer decisions (2026-10-30, second round).** These close the DISCOVER conditions so DIVERGE can start:

- **Prevalence is treated as common.** The maintainer judges deeper hierarchies to be a common use case that Lighthouse must account for. G1's case count is accepted on that judgement plus the four reported customers (OA3 closed by decision).
- **Jumping over a level is not Lighthouse's problem.** Items that skip a level can already be handled with the Parent Override Field. The new concept may require every item to go through all configured levels, and another option can be worked out later from feedback. O2/O6 shrink accordingly; the relay level is no longer required.
- **Customer B is assumed to have the same problem as Customer A** until the 2026-11-02 session says otherwise.
