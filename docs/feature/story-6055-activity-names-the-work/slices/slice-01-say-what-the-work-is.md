# Slice 01 — Say what the work is

**Story** #6055 / US-01 · **Job** `job-operator-see-what-lighthouse-is-doing-right-now`

## Goal

Every row in the Task Manager's Activity list leads with what the work is — *Refreshing*, *Forecasting*
or *Removing* — so a Portfolio being refreshed and the forecast that refresh triggers stop drawing two
rows with the same text.

## IN scope

- **A total verb lookup over all five `UpdateTaskType` members** (D2). `Team` and `Features` →
  *Refreshing*, `Forecasts` → *Forecasting*, `TeamDelete` and `PortfolioDelete` → *Removing*. The
  `default:` arm that currently sends three types to one word is the defect and does not survive.
- **The row reads `<Verb> <Kind> '<name>'`** (D1). The kind stays the tenant's configured Terminology;
  the verb is Lighthouse's own word and is never looked up.
- **The `(removal)` suffix is removed**, not kept alongside. *Removing* says it.
- **`docs/settings/taskmanager.md`** — the Activity section names forecasts, and the row-wording table
  matches what the component now emits (D7).
- **`docs/assets/settings/taskmanager.png`** — regenerated from `Screenshots.spec.ts:136-138`. Delete
  the file before the run; a diff under the pixel threshold silently keeps the old one.

## OUT of scope

- The `waitingBehind` clause — slice 02. This slice leaves `Queued behind Ocean Explorer` exactly as it
  reads today, including the self-reference.
- Any backend change. The read model already sends the update type; nothing about the response moves.
- The stop button's `Stop refreshing <name>` label, including on a removal row.
- The `"{UpdateType} {Id}"` fallback name for an entity that has gone.
- Collapsing the refresh row and the forecast row into one — rejected, 2026-09-21.

## Learning hypothesis

**Disproves that a verb can lead the row while the kind stays the tenant's own word.**

The row is one sentence assembled from two vocabularies: Lighthouse's ("Refreshing") and the tenant's
("Squad"). If a tenant who has renamed Team to *Squad* and Portfolio to *Programme* reads
`Refreshing Programme 'Ocean Explorer'` and it does not parse — because the verb implies a Lighthouse
concept the renamed noun no longer matches — then leading with the verb is the wrong shape and the
suffix that was rejected is the right one after all. AC-01.3 and AC-01.5 are that question asked as
tests; the dogfood screenshot is it asked of a reader.

If it succeeds, the reported defect is closed end to end — rows, prose and screenshot — in one slice,
with no contract touched.

## Acceptance criteria

AC-01.1 through AC-01.7 in `feature-delta.md` — 7 ACs. The three carrying the risk:

- **AC-01.1** — the two rows must differ, asserted as *a difference between the rendered strings* rather
  than against two expected literals. An assertion that hard-codes both strings passes while a future
  type quietly joins the pile; this one does not.
- **AC-01.3 / AC-01.5** — the verb and the kind come from different places and must keep doing so. With
  every configurable term renamed, the kind changes and the verb does not.
- **AC-01.7** — **production data.** A real Portfolio refresh on the dogfood instance, showing a
  *Refreshing* row and a *Forecasting* row at the same moment, and that frame is what the docs
  screenshot becomes.

## Dependencies

- **None.** `ActivitySection.tsx` is the only source file, and the `@screenshot` spec that regenerates
  the docs image already exists and already opens this popover.
- The screenshot run needs a premium licence fixture, which is gitignored and absent in a worktree —
  import it from the main checkout before running, and exclude `@auth` so the DB wipe does not lose
  other regenerated PNGs.

## Effort estimate

**~3h of crafter dispatch.** One lookup, one template string, one docs table, one screenshot run.

## Reference class

`epic-5511-task-manager` slice 07 ("the queue reads like a queue") — same component, same test suite,
same docs page, same screenshot spec.

## Pre-slice SPIKE

**None.** Nothing here is unknown; the uncertainty is a taste question, and the honest way to settle it
is the dogfood screenshot the slice already produces.
