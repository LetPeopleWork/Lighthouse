# Slice 05 — Jira and Linear dependencies

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-09 · **Estimate**: ~6h
**Reference class**: every prior connector-parity slice in this repository — epic #5687's per-connector
sweep and epic #5513's mapper work. The pattern is established; only the payload shapes are new.

## Goal

A Jira or Linear instance gets everything slices 01-04 delivered, from its own tracker's links, with
nothing re-entered by hand.

## IN scope

- **Jira**: add `issuelinks` to the `fields=` list (`JiraWorkTrackingConnector.cs:1560`, `:1613`).
  One edge per link where `type.inward = "is blocked by"` **and** an `inwardIssue` is present.
  An entry with only an `outwardIssue` means *this issue blocks that one* and yields nothing (D14).
  The summary is returned inline, so no follow-up request.
- **Linear**: add the `dependencies` connection to the existing GraphQL selection
  (`LinearWorkTrackingConnector.cs:660-726`), beside `parent`. `dependencies` = what blocks you;
  `blocking` = what you block, and is not fetched. Titles inline.
- Both go through the same connector-port method slice 01 introduced, so the ingestion, reconcile,
  honour-ability verdict, warnings and forecast rule are untouched.
- ServiceNow and CSV return an empty edge set and warn about nothing — the absence of a dependency
  field is not an error (AC-9.4).
- The existing AC suites from slices 02-04 are **parameterised over connector**, not duplicated per
  connector (AC-9.5). If that parameterisation is awkward, the ingestion abstraction is in the wrong
  place and this slice is the signal.
- Docs: a dependencies page under the Features documentation, in seeded terminology, with per-feature
  screenshots. This is the epic's `Documentation` tag paid off at the last slice that changes user-
  visible behaviour.

## OUT of scope

- ServiceNow and CSV dependency support (D13). No standard field exists; inventing a convention is a
  different epic.
- Fetching the reverse direction on any connector (D14).
- Any change to the forecast, the warnings or the storage.

## Learning hypothesis

**Disproves** "the connector port slice 01 introduced is the right abstraction" **if** either Jira or
Linear cannot express its edges through it without a special case — most plausibly because Jira's
`issuelinks` carries a link *type name* that varies per Jira instance ("Blocks" is the default name but
is renameable), so `type.inward = "is blocked by"` may not be a reliable discriminator on a customised
instance.

If it fails, the connector port needs a per-connection notion of *which* link type means dependency —
which is a near neighbour of slice 06's per-Portfolio field pointer, and would argue for pulling that
slice forward rather than inventing a second configuration surface beside it.

**Confirms**, if it holds, that adding a fourth connector later is a mapper change and nothing else.

## Verify the premise first (30 min, before writing the mapper)

On `:5169`'s Jira connection, list the configured issue link types and check whether `is blocked by`
appears with that exact inward name. One API call, and it decides whether this slice is a mapper or a
configuration feature.

## Acceptance criteria

AC-9.1 … AC-9.6 verbatim from `feature-delta.md`. The three that carry the slice:

- A Jira link with `inwardIssue` yields an edge; one with only `outwardIssue` yields none (AC-9.1).
- Adding `issuelinks` to the `fields=` list changes no existing mapped value (AC-9.3).
- Slices 02-04's ACs pass parameterised over connector rather than duplicated (AC-9.5).

## Dependencies

Slices 01-04. `:5169`'s Jira and Linear connections with at least one dependency link created on each
— likely to need creating by hand, since the dogfood instance holds only 4 Jira and 4 Linear Features.
Per-connector timing baselines for AC-9.6.

## Dogfood moment

Same day: refresh both connections on `:5169`, confirm the column, dialog, warnings and dates behave
identically to the ADO ones, and regenerate the docs screenshots.

## Commit gate

**No commit without the maintainer's explicit approval.**

## Learning hypothesis verdict

_Not yet run._
