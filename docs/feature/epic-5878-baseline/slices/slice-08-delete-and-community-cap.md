# Slice 08 — Delete reports; Community's two; Premium unlimited

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-08 (ADO #6166) · **Estimate**: ~1d · **Tier**: Community cap,
Premium unlimited · `job_id: job-flow-coach-show-whether-flow-changed`

## Goal

Editors delete reports. Community holds 2 reports per {Team}/{Portfolio}, counted across every template; Premium is
unlimited; a lapsed licence keeps every report readable and deletable. Deleting a {Team} or {Portfolio} deletes its
reports, and its confirmation says so.

> Revised 2026-10-03: the maintainer confirmed reports go with their owner and asked for the count in the owner's
> delete confirmation (D42).

## IN

- Delete from the report menu, Write only, confirmation naming the report; extra warning when Then now starts before
  the data cutoff: "This Then window can no longer be rebuilt." (D30)
- Cap per D29, copying `AdditionalFieldsHelper` (`CanUsePremiumFeatures() || count < 2`), enforced on the server and
  shown on the disabled Create Report: "Community includes 2 reports per {Team}. Delete one, or use Premium for
  unlimited reports."
- Lapse: every report stays viewable and deletable; create blocked until the count is below 2.
- Owner deletion: the existing {Team} / {Portfolio} delete confirmation adds "and its 2 reports" (the actual count,
  only when there is at least one); the reports cascade with the owner (D42).
- Usage data: `ReportDeleted`, name-only, after the 204 of a report's own delete; never fired when reports go with
  their Team or Portfolio (DEVOPS; G1). No cap-refusal event (maintainer 2026-10-03).

## OUT

Premium gating of metrics, panels or editing (never, DV-7), export tiers (later).

## Learning hypothesis

**This disproves "2 reports is enough for Community"** if dogfood or early instances hit the cap within a month
without ever deleting — the cap then blocks the job rather than inviting Premium. Read K4 with G1.

## Data and dogfood moment

- In a worktree, copy the premium licence fixture from the main checkout before running the cap tests.
- Dogfood: create a third report on a Community dev instance, then delete one.

## Acceptance criteria

AC-8.1 … AC-8.5 in `feature-delta.md` (US-08).

## Dependencies

Slice 01 (reports exist); 06 so the cap covers Portfolios too. Release notes, docs and website copy wait for this
slice (D34).
