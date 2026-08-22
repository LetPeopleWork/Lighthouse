# Slice 01a — See what a Jira Release would give you

**Goal**: on a Jira-connected Portfolio, show the project's Releases and preview what one would bring to
a Delivery — its date and its Features — without saving anything.

**Story**: US-01. **Walking skeleton for the Epic.**

## IN scope

- **Precursor commit (first commit of this slice)**: make the Delivery modal's selection tabs render from
  a list instead of the hardcoded two-way branch. Touches ~8 sites in `DeliveryCreateModal.tsx`
  (`ButtonGroup`, `SelectionModeContent`, the validity helper, the payload builder, the edit-hydration
  effect, the reset). Behaviour after this commit is byte-identical for Manual and Rules.
- `GET /api/v1/portfolios/{id}/delivery-sources` — the handlers this Portfolio's connection offers.
  Empty list for AzureDevOps, Linear, ServiceNow, CSV.
- `bool SupportsDeliverySources(connection)` on `IWorkTrackingConnector`, shaped like
  `SupportsIncrementalSync` (per connection, not per connector).
- `GET /api/v1/portfolios/{id}/delivery-sources/{sourceKey}/options` — the Releases with their dates.
- `POST /api/v1/portfolios/{id}/delivery-sources/{sourceKey}/preview` — date plus matching Features for
  one selected Release. Reuses the Feature grid the Rules tab's validate step already renders.
- The Jira adapter behind all three: enumerate project Versions, and match Portfolio Features by
  `fixVersion`, matching on the version id (slice 00 Q1 confirms the field shape).
- Dateless Releases: listed, labelled as having no date set in Jira, **not selectable** (D11). A Jira
  version's release date is optional and two of the three on the demo instance have none, so this is
  the common rendering, not an edge case.
- Premium gate on preview, matching rule-based selection's existing gate.

## OUT of scope

- Persistence of any kind. No migration, no new column, no enum member. Slice 01b does that.
- Any second handler, any second connector.
- The Delivery detail view. This slice lives entirely in the create/edit modal.

## Learning hypothesis

**Disproves the version-matching code** if a Release whose Features *are* tagged still previews zero.
Tagging the Portfolio's Features with the version is a customer-side convention, not something Lighthouse
discovers — a Feature without one is simply not in the Release, the same way a Feature failing a rule is
not in a rule-based Delivery. So an empty preview is a bug in the matching, or a Jira-side gap the
preview should name plainly. It is no longer a threat to the design.

**Confirms** that the registry-driven tab list degrades correctly, which is the property that makes every
later handler cheap.

## Acceptance criteria

AC-01.1 through AC-01.7 in `feature-delta.md`. The two that carry the slice:

- The Jira Release tab is absent from the DOM on the four non-Jira connectors (not hidden by CSS, not
  disabled — absent).
- The empty-match case renders a reason, not a blank grid.

## Dependencies

Slice 00 complete — Q1 confirms the shape the matching code reads. Not gated on Q3 or Q4, which decide
only whether slices 04-05 exist.

## Effort

~6 hours. The fattest slice in the Epic: the precursor refactor is roughly half of it. Documented
exception to the 4-component taste test — see `feature-delta.md`.

## Pre-slice risk

The precursor refactor touches the file that also owns rule-based creation. Rules regression is the
thing to watch; the existing `DeliveryCreateModal` tests are the guard and must stay green unchanged.
