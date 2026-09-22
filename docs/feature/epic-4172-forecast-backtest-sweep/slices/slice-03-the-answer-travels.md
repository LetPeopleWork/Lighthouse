# Slice 03 — The answer travels — SEVERABLE

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: to create under Epic #4172 · **Story**: US-03
**Estimate**: ~4h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Persona**: `forecasting-prospect` primary · **Depends on**: slices 01 and 02
**Severable** — if dropped, the feature still ships.

> **This was slice 04 until 2026-09-22.** The Apply slice that sat between 02 and this one was removed
> entirely — not deferred — when the maintainer dropped the Apply control, and this slice moved up to fill
> the gap. See `feature-delta.md` D4 for the reasoning. Nothing about this slice's content changed; only
> its number and its AC labels (AC-4.x became AC-3.x).

**Reference class**: ADR-172 (a Delivery exports one settled table the caller builds) and ADR-162 (the
export header block as a generic toolbar input). Both are **client-side**; there is no server-side
document renderer in this product, which is what makes this cheap and what makes a durable emailable
Report expensive (D8).

## Goal

The forecaster who was asked "how do you know this is right?" by someone not in the room pastes the whole
check into a message, so the answer is something the reader can interrogate rather than something they
have to take on trust.

## Why this exists at all, stated honestly

C1 makes this feature free, and the Epic is tagged **Community** for the stated reason that it "should
convince people of the method, so they flock to use the tool". That makes conversion a **declared
secondary objective with real weight** (10% of the DIVERGE taste matrix), not a smuggled one. ODI outcome
O5 — *minimize the effort required to show a sceptical stakeholder that probabilistic forecasting held up
on a Team's own history* — scores 11.6, under-served.

This slice is the whole of that. Nothing else in the feature leaves the browser.

It is **last** because a one-pager of a verdict that has not been dogfooded is a liability rather than an
asset, and **severable** because the three honesty requirements — the thing the recommendation is
conditional on — are all built by slice 02.

## IN scope

- A copy control on the verdict card producing a Markdown one-pager holding:
  - the verdict sentence, both clauses;
  - the denominator and non-comparability paragraph, in full;
  - all sixteen rows with their real date spans, forecast values and outcomes;
  - every unevaluable row with its reason;
  - the nominal-rate table;
  - a footer crediting Nick Brown's method **and stating, in the same paragraph, that the three-way
    under / over / within-range verdict is this product's departure from it**.
- Every configurable term rendered from the instance's own terminology.

## OUT of scope

- **Any server-side rendering, any new endpoint, any stored artifact.** D8. The emailable Report is what
  ADR-207 explicitly does not build.
- **Any control that writes a Team setting.** D4 — the feature is read-only end to end, and an export
  slice is not where that gets quietly reversed.
- Emailing, scheduling or sharing by link. C2, and the parked constellation (#4753, #4755) stays parked.
- PDF or image export. Markdown, and optionally CSV for the sixteen rows, per the ADR-172 precedent.

## Learning hypothesis

**Disproves, if it fails**: that the honest reading survives leaving the product. The three honesty
requirements are UI copy inside a card the user is looking at; a one-pager is read cold, by someone with no
context, possibly weeks later, possibly by someone deciding whether to evaluate the product at all. If the
non-comparability paragraph reads as boilerplate when pasted into Slack — or worse, gets trimmed by whoever
pastes it — then the artifact is not honest outside the browser, and the right answer is to make the
paragraph shorter and sharper rather than to drop it.

**Confirms, if it succeeds**: O5 is served, and the honest first step toward the emailable Report exists as
a slice rather than as an abstraction — which is precisely the distinction ADR-207 turns on.

## Acceptance criteria

AC-3.1 through AC-3.4, in `feature-delta.md` under US-03.

## Notes for the implementer

- **R-5 is a hard DELIVER gate that lands here.** The source article was read via a readmedium.com mirror
  because medium.com returns 403. The findings are arithmetic-reconciled and corroborated, **but one human
  page-load of the original is required before anything from it is quoted publicly** — which includes this
  one-pager's footer, the launch post and the docs page. Do not ship the attribution copy until that is
  done.
- Terminology (C6): the words "Epic", "Initiative" and "Story" never appear. "Work Item", "Team",
  "Feature" and "throughput" all render from `TerminologySeeder.cs` defaults or the instance's overrides.
  The feature name "Forecast Reality Check" deliberately contains no configurable term.
- The website hot-links `docs/assets` from `@main` via jsDelivr. If this slice or its docs page adds an
  asset, check the website repo before renaming or deleting it later.
