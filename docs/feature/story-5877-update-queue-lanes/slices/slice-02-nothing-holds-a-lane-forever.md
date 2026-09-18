# Slice 02 — Nothing holds a lane forever

**Story** #5877 / US-02 · **Job** `job-operator-bound-a-refresh-that-will-not-end`

## Goal

A refresh that has run past a sane bound ends itself, frees its lane, and leaves a record saying which
bound it passed — so no single run can hold a lane until someone next restarts the instance.

## IN scope

- **A total wall-time bound per run**, configurable, with a default (D8). Per run, not per key: a
  coalesced follow-up starts a fresh clock (D7).
- **Eviction through the operator-cancel path** (D9). The slice-04 probe measured that a token reaches
  inside a connector call at one page round-trip of granularity, so the bound drives
  `AdmittedCancellations` rather than a new mechanism, and the run reaches the same visible terminal
  state an operator's Cancel produces.
- **A refresh-history record that does not lie about who pressed it.** The `RefreshLog` row names the
  bound as the reason, distinguishably from an operator-initiated cancel.
- **Deletes are exempt** (D6). `TeamDelete` and `PortfolioDelete` are already uncancellable; the bound
  must not make them evictable behind the operator's back.

## OUT of scope

- Lane structure — slice 01.
- A per-phase or per-request timeout. The per-request timeout is 100 s today and stays as it is; this
  bounds the whole run, which is the thing nothing bounds.
- Automatic retry or backoff after a bound-ended run. The next scheduled refresh is the retry.
- Surfacing the bound in the task list as a countdown. The row already shows how long it has been
  running (#5511 slice 03); a second number racing it adds nothing.
- Adapting the bound to observed durations. A fixed configurable number, not a learned one.

## Learning hypothesis

**Disproves that the measured cancellation granularity is good enough to act on.**

The probe measured one page round-trip — 2 087 ms on the post-#5687 Data Center dogfood, against
468 856 ms before that Epic's paging work. If the bound fires on a real refresh and the run does not
stop within that interval (AC-02.4), then cancellation is a request the connector can ignore for
minutes, the bound is a log line rather than a lever, and the honest options narrow to hardening the
paging loops or accepting that some runs cannot be stopped short of a restart.

If it succeeds, the instance has no unbounded state left: the lanes stop one slow thing starving
others, and the bound stops anything holding a lane indefinitely.

## Acceptance criteria

AC-02.1 through AC-02.8 in `feature-delta.md` — 8 ACs. The four carrying the risk:

- **AC-02.2** — the reason must be distinguishable from an operator cancel. An operator who pressed
  nothing must not read a history saying they did; that is how a record stops being believed.
- **AC-02.4** — stops within **one page round-trip** of the bound firing. This is the hypothesis stated
  as a number, taken from the probe rather than from an intention.
- **AC-02.5** — the follow-up starts a fresh clock. Inheriting the elapsed time would kill every
  follow-up of a long refresh instantly — a bound that makes the reported symptom worse.
- **AC-02.8** — **production data.** On Tenant Zero with the bound temporarily lowered, a real refresh
  against a real connector is ended by it, the history records it, the lane frees, and the next
  scheduled refresh runs normally. Bound restored afterwards.

## Dependencies

- **Slice 01.** Not mechanically — the bound would work against a single lane too — but on its outcome.
  If slice 01's dogfood shows teams keep moving, this slice is a tidy-up rather than a rescue and may
  legitimately be dropped. Decide after slice 01 is dogfooded, not before.
- **D8's number**, which DESIGN sets from `RefreshLog`'s duration distribution. Cannot start without it.

## Effort estimate

**~4h of crafter dispatch.** One bound, one reason on an existing log row, one exemption, and the
configuration surface DESIGN picks.

## Reference class

`epic-5511-task-manager` slice 04 — same cancellation path, same terminal states, same test fixtures.

## Pre-slice SPIKE

**None.** The uncertainty this slice carries is not "can it be built" but "does stopping actually
stop", and that is AC-02.4 measured against a real connector — an acceptance criterion, not a probe.
Running it as a spike first would measure the same thing twice.
