# Slice 07 — The queue reads like a queue, and a cancel is heard

**Epic** #5511 Task Manager · **Story** #6011 / US-07A–D ·
**Jobs** `job-operator-see-what-lighthouse-is-doing-right-now`,
`job-operator-stop-a-refresh-that-is-doing-harm`,
`job-config-admin-know-any-credential-is-failing-not-just-oauth`,
`job-operator-read-the-warnings-without-reading-the-log`

## Goal

The popover that was built to be glanced at can be glanced at: rows in the order the queue will work
them, progress shown rather than spelled, a stop control that answers, and nothing above the content
that is not content.

## IN scope

- **Order** — `GetTasks` sorts running first, then queued oldest-admission-first, over the `StartedAt` /
  `QueuedAt` moments `UpdateStatus` already carries.
- **Progress shown** — spinner on the running row, a distinct waiting affordance on queued rows.
- **Duration removed** — `Running for 2m 14s` becomes `Running`. Closes the deferred not-live finding by
  deletion.
- **`Stopping…`** — a cancelled row stays, says the stop was asked for, and leaves when the instance
  stops reporting the work.
- **Re-read on open** — a connection added since the tab loaded appears without a page reload.
- **Icons** — beside the three section headings, and replacing the connection-state words.
- **Disclaimer deleted** — the Recent problems caveat goes, and its test goes with it.

## OUT of scope

- **Slice 08's three items** — persisting health across a successful refresh, startup or periodic
  probing, an unavailable-connection badge. All reopen D9.
- **Bug #6010** — Test connection on an ADO connection returns a 500.
- **Ordinal queue position / wait estimate** — deferred idea G's remaining half (D15).
- **Removing `elapsedMs` from the wire contract** — only its rendering goes (D14).
- **Collapsible sections** (D17).

## Learning hypothesis

**Disproves that the Task Manager's problems were about what it shows rather than how it reads.**

Every item here is presentation over data that was already correct and already on the wire. If it
succeeds, the surface is finished and slice 08's health work is the only outstanding capability.

If it fails — the maintainer uses the polished popover and still cannot answer "what is happening and is
it healthy" in a glance — then the problem is the surface's shape, not its wording, and D1's single
popover is what needs revisiting. That would be a much larger and more unwelcome finding, which is
exactly why it is worth naming before the cheap fixes hide it.

## Acceptance criteria

See US-07A through US-07D in `feature-delta.md` — 22 ACs. The three carrying the risk:

- **AC-07A.2** — queued order is stable **across repeated reads**. The defect is non-determinism, so one
  correct read proves nothing; a test that reads once can pass against the bug.
- **AC-07B.5** — `Stopping…` verified against a **real connector refresh**. The behaviour exists only in
  the gap between the request and the next checkpoint, and a double that cancels instantly has no gap.
- **AC-07C.2** — not-checked distinguishable from healthy by **fill and shape, not only colour**. D9
  exists because an icon claimed health from an absence of evidence; an icon set that repeats that is a
  regression wearing a redesign.

## Dependencies

None unshipped. Slices 01–06 are all delivered and pushed.

## Effort

Half a day. The only non-mechanical part is `Stopping…` and its real-connector verification.

## Reference class

Slice 03 (elapsed) and slice 06 (recent problems) — both shipped correct data in a shape the maintainer
then read differently than intended. That is this slice's whole subject, and the reason its hypothesis is
about reading rather than about data.

## Pre-slice SPIKE

None. **OQ-07.1 is settled** (maintainer, 2026-09-16): "this one was asked to stop" is remembered in the
browser that asked. No backend change beyond the sort, and no DESIGN wave for this slice.

The accepted gap is recorded as deferred idea **L**: another tab, another administrator, or a reload
inside the ~11s window sees no acknowledgement, because the fact lives in one browser rather than in the
instance.
