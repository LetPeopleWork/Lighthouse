# Slice 03 — A portfolio over ServiceNow parents (CONDITIONAL)

**Status**: **conditional** — build only if SPIKE Q5 finds a usable parent/child relationship.
If it does not, this slice is **cancelled loudly**: US-03 AC5 requires the docs to state the
team-only limitation, so a prospect learns it before installing.

**D4 (ITSM-first) makes cancellation more likely, deliberately.** ITSM's rollup concept (`task.parent`,
Demand, SPM Project) is weaker and less consistently populated than Agile 2.0's `rm_story`→`rm_epic`.
That is an accepted cost of optimising for the model most prospects actually use — not an oversight.
If Q5 finds Agile 2.0 has a clean hierarchy and ITSM does not, that asymmetry is itself a finding for
the viability verdict and belongs in the docs.

**Goal**: A delivery lead creates a Portfolio over ServiceNow parent records and gets per-feature
child counts and forecasted completion dates.

**Stories**: US-03 (value).

## IN scope
- `GetFeaturesForProject` — parent records → Lighthouse features.
- `GetParentFeaturesDetails` — resolving named parents.
- Child→parent resolution via whichever relationship Q5 identifies.
- `ValidatePortfolioSettings` with actionable messages.
- Whether ServiceNow portfolios need work-item-types (Linear sets `isWorkItemTypesRequired=false`; SNOW likely matches — confirm, do not assume).

## OUT of scope
- Transition history (slice 04). Multi-level hierarchies beyond one parent step. Cross-instance portfolios.

## Learning hypothesis
**Disproves** "ITSM work rolls up to something Lighthouse can forecast as a feature" **if** the only
parent concept is unusable — per-record N+1 at prohibitive cost, or a relationship (`task.parent`)
that exists in the schema but sits empty on real records. That result caps ServiceNow at team scope —
a real narrowing of the market thesis, and exactly the kind of finding this epic exists to produce.
**Confirms** that Lighthouse's headline capability (portfolio forecasting) reaches ServiceNow.

## Acceptance criteria
See US-03 AC1–AC5 in `feature-delta.md`.

## Dependencies
- Slice 02.
- **SPIKE Q5** — hard gate on whether this slice exists at all.

## Effort / reference class
≤1 day. Reference class: `LinearWorkTrackingConnector.GetFeaturesForProject` /
`CreateFeatureFromProject` / `GetInitiativeById` — Linear's project→initiative rollup is the closest
analogue to an unfamiliar hierarchy mapped onto Lighthouse features.

## Pre-slice SPIKE
**Mandatory** — Q5, including the N+1 cost question. A hierarchy that exists but costs one call per
feature still changes the refresh-duration story and must be measured, not assumed.

## Dogfood moment
Same day: seed parents + children in the dev instance, create a Portfolio, confirm feature list and
per-feature forecast render with correct remaining/total counts.
