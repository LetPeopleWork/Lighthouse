# ADR-197: The wall-time bound is an in-process deadline on the run's own token, defaulting to 180 minutes

**Status**: Accepted (DESIGN, 2026-09-18; interaction mode PROPOSE)
**Date**: 2026-09-18
**Feature**: story-5877-update-queue-lanes (ADO User Story #5877, slice 02)
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

Nothing bounds an update's total wall-time. A refresh that never returns holds its lane until the
process restarts, and the reader's backstop logs `"Error processing update task"` naming nothing
(`UpdateQueueService.cs:610-620`). A grep for a timeout anywhere in the update pipeline returns the
two periodic-refresh delays and nothing else.

When this was written, the intended remedy was a watchdog that logs. Epic #5511's slice-04 probe
changed that: a cancellation token set by the queue immediately before it invokes an update task is
readable inside a connector call, through the queue, the updater, the data service and the work-item
service, at a granularity of one page round-trip
(`docs/feature/epic-5511-task-manager/slices/slice-04-stop-a-refresh.md:97-154`). The bound can
therefore *act* through the path an operator's Cancel already drives, rather than only observe.

Four facts decide the shape.

*The moments are best-effort.* ADR-182 puts `QueuedAt` / `StartedAt` in a sibling Redis hash written
outside the frozen advance scripts, and states that absent is a legitimate state — a replica mid-upgrade
records nothing. Anything that reads `StartedAt` to decide whether to act is therefore a mechanism
that silently does not fire for some runs.

*`RefreshLog` records `Cancelled` but not why.* The column was added on 2026-09-15 and the refresh
history subtracts cancelled runs from the success-rate denominator, precisely so that a refresh
somebody stopped does not read as a broken connection. A run the *instance* stopped would land in that
same bucket and tell an operator who pressed nothing that they did.

*The reason already exists and is thrown away.* `SyncOutcome.Reason` is populated in the updaters'
catch blocks and reaches only the Serilog line; the `RefreshLog` row records mode and record counts
and drops it.

*Instance settings are DB rows parsed from strings.* `AppSettings` holds eleven keys; the refresh
intervals live there, in whole minutes, read per call with no cache. `OptionalFeature` is a boolean
and carries no value. Nothing in the refresh pipeline is `IOptions`-bound.
`GetRefreshLogRetentionRuns` is the one existing "numeric instance setting read by key, parsed, with
an in-code fallback and a clamp" — its row *is* seeded, and the fallback is a second line of defence
behind the seed rather than the primary path.

*The seeder has a trap.* `AppSettingSeeder` inserts rows with hardcoded identifiers, and
`RemoveObsoleteSettings()` deletes identifiers 9 through 42 on every boot. A new seeded row taking an
identifier in that band is removed on the next start, silently.

## Decision

**Five parts.**

### 1. The deadline is armed in the process that runs the work, not observed from the store

When a run starts, it creates a `CancellationTokenSource` whose delay is the bound, linked with the
per-key operator source `AdmittedCancellations` already owns. The linked token is what
`ExecuteUpdateTask` publishes as the ambient cancellation context, so the existing reach and the
existing terminal handling apply verbatim. The run's own terminal path writes `Cancelled` and frees
its lane; nothing new decides anything.

The delay is armed through the injected `TimeProvider` — already registered and already faked in
tests with `FakeTimeProvider` — so the bound is testable without waiting.

**The clock starts when the execution starts, after the per-key execution lock is acquired and the
status advances to `InProgress`.** Queue wait is not counted: work that did nothing wrong must not be
killed for having waited, and with lanes queue wait is no longer the pathology. Time spent blocked on
another replica's advisory lock is not counted either — that is not this run's doing.

**The bound is per run, not per key.** A coalesced follow-up creates its own source and starts a fresh
clock. Inheriting the elapsed time of the run it follows would kill every follow-up of a long refresh
the instant it started, which makes the reported symptom worse.

### 2. Deletes are exempt, structurally

`TeamDelete` and `PortfolioDelete` never get a deadline. They are already uncancellable on the operator
route, and a half-done delete leaves the caller told an entity is gone while its row is in the
database. The exemption lives in one place — the function that decides a run's bound — and is asserted
for both members rather than inferred.

### 3. The bound is one instance-wide `AppSettings` key, and it is not seeded

`Update:MaxRunMinutes`, in whole minutes, matching the unit convention every other refresh setting
uses. Read through `IAppSettingService` in the code shape `GetRefreshLogRetentionRuns` established:
read by key, parse, fall back, clamp.

**Unlike that one, no row is seeded.** An absent row *is* the default, so "absent or unreadable falls
back to the default" is a property of the read rather than a consequence of a seeder having run — and
the read is the thing under test. Not seeding also sidesteps the seeder's identifier trap entirely: a
new row taking an identifier between 9 and 42 is deleted on the next boot without a word. The write
path upserts, in the shape the survey-nudge settings already use, so the row appears the first time an
operator changes the value and never before.

The decision itself is a pure function of the stored string:

- absent, unparseable, or not positive → the default
- otherwise clamped to [5 minutes, 24 hours]

Zero and negative fall back to the default rather than clamping up to the floor. Clamping a typed `0`
to five minutes would give an instance that cannot complete any refresh at all; the floor exists to
catch a low-but-deliberate value, not to reinterpret a value that means nothing. This is a deliberate
divergence from `GetRefreshLogRetentionRuns`, which clamps a zero to its floor of ten runs, where the
consequence is harmless.

It is editable in Settings → Configuration, on the existing app-settings controller under the
System-Administrator guard every other write there carries. A setting only changeable by editing a
row in a SQLite file is not configurable for the self-hoster running a desktop build, and that is the
person who reported this.

### 4. The default is 180 minutes

Three anchors, in order of weight.

**180 minutes is already this product's statement about staleness.** `RefreshAfter` ships at 180
minutes for both Teams and Portfolios: data older than three hours is due a refresh. A run still going
after three hours has outlived the window its own result was meant to close — whatever it eventually
writes is already stale by the product's own definition. That makes 180 a number with a meaning rather
than a round one.

**It clears every run on record.** The reported incident's longest run was 77.7 minutes, 2.3× under
the bound. Epic #5687's pre-optimisation Data Center measurement was 468 seconds — 7.8 minutes, 23×
under. No refresh this product has recorded would have been ended by this default.

**The bound is the backstop, not the fix.** The lanes are what answer the reported complaint; this
ends runs nobody is watching. A default tight enough to second-guess a legitimately slow tracker would
trade a visible failure for an invisible one. An operator whose tracker is genuinely slower raises it.

It is **not** derived from `RefreshAfter` at runtime. Coupling them would mean an operator lowering
`RefreshAfter` to thirty minutes silently starts killing forty-five-minute refreshes.

The number is a hypothesis until it meets real data — see Earned Trust.

### 5. `RefreshLog` gains a nullable reason column, and the history distinguishes the two cancels

One nullable string column, expand-only, added with the existing `CreateMigration` script across all
supported providers. It is not a special case for the bound: it is the persisted home for
`SyncOutcome.Reason`, which all three updaters already compute and hold in scope at the very `finally`
that writes the row.

The bound writes a token beside the existing `"configuration-changed"`, not a sentence. User-facing
prose is rendered in the browser from the tenant's configured terminology; a sentence composed in the
backend would hardcode words the tenant may have renamed. An unrecognised token renders verbatim, so
the reasons already in the code can be surfaced later without another migration.

The refresh history splits its single *Cancelled* figure into runs an operator stopped and runs the
instance stopped at the bound. Both stay out of the success-rate denominator: a run the instance
stopped says nothing about whether refreshing works, and reporting it as a failure sends the operator
looking for a broken connection. What changes is that they are no longer the same number.

**And the log line says so, at warning level.** The existing cancel path logs "…was cancelled" at
information, which reads as an operator action and is below the default reporting level. An instance
ending its own run is the thing an operator is most likely to go looking for afterwards, and the
recent-problems popover surfaces warnings and errors only.

## Alternatives Considered

**A watchdog that scans the status store for `InProgress` work whose `StartedAt` is older than the
bound.** The obvious shape, and the one the story was written against. **Rejected on ADR-182**:
`StartedAt` is best-effort and absent is legitimate, so the watchdog silently never fires for any key
whose moment write failed or that was admitted by a replica mid-upgrade — the failure mode being
silence, in a mechanism whose whole purpose is to break a silence. It also needs a timer, a scan
interval that bounds its own accuracy, and a rule for which replica acts. The in-process deadline
cannot miss, because the process that runs the work is the process that arms the clock.

**A watchdog that logs rather than acts.** What the story originally proposed, before the slice-04
probe. Rejected: the probe showed a token reaches inside a connector call, so a log line would be a
deliberate choice to watch an instance stay wedged. It would also leave the lane held, which is the
actual complaint.

**Record the reason as a flag written when the stop happens**, instead of asking which source fired.
Rejected: two stops can land in the same instant — an operator pressing Cancel on a run that is
already at its bound — and a written flag has to pick a winner at write time, with no way to be sure
it was the last writer. Deriving the reason from which sources are cancelled, with the operator taking
precedence, is race-free and says the truthful thing: if they pressed it, they pressed it.

**A per-phase or per-request timeout.** The per-request timeout is 100 s today and stays. Rejected as
the answer here: the thing nothing bounds is the whole run, and the full-fetch path has no phase
boundary inside the connector to place a phase timeout at.

**A fourth field on `RefreshSettings`**, gaining the existing form, route and DTO for free. **Rejected
as unworkable**: `RefreshSettings` is per refresh kind, and `ForecastUpdater.GetRefreshSettings()`
throws `NotSupportedException` because forecasts have no periodic refresh at all. A bound living there
could not cover the Forecast lane. It would also change a `[JsonRequired]` DTO and its frontend schema
in lockstep, for a value that is not per kind.

**Adapt the bound to observed durations** — a multiple of the entity's own recent `DurationMs`
percentile. Rejected: it makes the bound unpredictable exactly when an operator needs to reason about
it, and the first slow run teaches it that slow is normal.

**Retry or back off after a bound-ended run.** Rejected: the next scheduled refresh is the retry, and
a bound that triggers an immediate retry of the thing that just overran is a loop.

**Surface a countdown in the task list.** Rejected: the row already shows how long it has been
running; a second number racing it adds nothing.

## Consequences

**Positive.** No state in the instance is unbounded any more: lanes stop one slow thing starving
others, and the bound stops anything holding a lane indefinitely. The reason column closes a standing
gap in which the product computed an explanation for a failed refresh and then threw it away.

**Negative / accepted.**

- A schema change, which this story's scope assessment said it would not need. One additive nullable
  column, expand-only, guarded by the existing migration guard. Recorded as a changed assumption.
- The refresh history's statistics change, so the DoD's "screenshots not applicable" no longer holds
  for that surface. Also recorded as a changed assumption.
- A bound-ended run is reported to the browser as `Cancelled`, the same terminal state an operator's
  Cancel produces. That is deliberate — it is the same mechanism and the same outcome — and the
  distinction is carried in the history rather than in the terminal state, so `UpdateProgress` keeps
  every ordinal it has.
- The bound cannot stop a run faster than the connector's next page round-trip. That is the measured
  granularity and it is asserted as a number, not assumed.

## Earned Trust

The dependency that can lie here is the connector: it is handed a token and may take an unbounded time
to look at it. The whole slice's hypothesis is that it does look, within one page round-trip.

| Assumption | Probe |
|---|---|
| A run that passes the bound is cancelled and reaches `Cancelled` | Acceptance test with a held-open connector double and a faked clock advanced past the bound. |
| **It stops within one page round-trip of the bound firing** | Measured against a real connector on Tenant Zero with the bound temporarily lowered. If it does not hold, the bound is a log line rather than a lever and the slice has failed its own hypothesis — which is a result, not a defect to work around. |
| The lane is free afterwards | The next queued work of that type starts. |
| The history does not say an operator pressed something they did not | The row carries the bound's reason, and the refresh history shows instance-stopped runs separately from operator-stopped ones. |
| A coalesced follow-up starts a fresh clock | After a run ends at the bound, its follow-up runs to completion rather than being killed immediately. |
| Deletes are never ended by the bound | Both delete members asserted directly against the function that decides a run's bound. |
| An absent, unreadable, zero or negative value falls back to the default | The decision is a pure function of the stored string, so this is a parameterised test over every degenerate input, with no database. |
| A configured value outside the sane band is clamped, not obeyed | Same test, covering below the floor and above the ceiling. |
| **180 minutes clears real traffic** | Before slice 02 ships, `RefreshLog.DurationMs` on Tenant Zero over 30 days is read and the maximum compared against the default. The number above is derived from two recorded points and one product constant; it becomes a measurement here or it changes. |
| The reason survives the wire | The refresh-log route returns the entity raw, so the column is exposed automatically — and that is exactly why it is asserted: an unvalidated client model silently ignores fields it does not declare. |

## Cross-reference

- [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) — the cancellation path this
  bound drives. Nothing new is introduced; the only new thing is who pressed it. Its recorded
  invariant, that a cancelled run must still flush or explicitly abandon its round and release
  anything held behind its key, applies unchanged to a bound-ended run.
- [ADR-182](./adr-182-update-moments-in-a-sibling-hash.md) — best-effort moments, and the reason the
  deadline is armed in process rather than read from the store.
- [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) — the lanes. The bound is only
  worth having if the lanes did not already answer the complaint, which is why it is sequenced second
  and may legitimately be dropped on slice 01's dogfood outcome.
- [ADR-076](./adr-076-cluster-aware-update-queue.md) — the per-key source and the per-replica queue.
  The deadline is per run in the process that runs it, so it needs no cross-replica coordination and
  adds none.
