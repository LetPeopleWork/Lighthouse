# Slice 08 — Connection health that persists and checks itself

**Epic** #5511 Task Manager · **Story** US-08A–C · **Bug** #6010 (folded in, D22) ·
**Job** `job-config-admin-know-any-credential-is-failing-not-just-oauth`

## Goal

A connection's health survives the refresh that proved it, and Lighthouse finds out by itself for the
connections no refresh ever touches.

## IN scope

- **Health persists** — `RecordRefreshSucceededAsync` records `Healthy` instead of deleting the verdict
  row (D19). This is the *"the health for wts seems to reset on every refresh"* defect.
- **Health tops itself up** — a background prober asks the connections whose verdict is missing or older
  than a staleness threshold, and leaves the rest alone (D20). Startup is its first tick, not a separate
  mechanism.
- **Azure DevOps `Test connection` answers** — Bug #6010's 500 is root-caused and fixed, because the
  prober runs the same path (D22).

## OUT of scope

- **Any new pixel.** The header badge for an unavailable connection already exists and is already tested
  (S30, D21). Two alternative readings — splitting the merged count, and animating the icon while work
  runs as D2 promised — were offered on 2026-09-16 and declined.
- **Rendering a verdict's age.** D20's threshold is what bounds `Healthy`; D14 removed durations here.
- **Probing on popover open or on connection save.** D18 already re-reads the stored verdicts on open.
- **Public Task Manager docs and screenshots** — carried since slice 05 and now the Epic's debt, not a
  slice's.
- **Deferred idea L**, idea G's remaining half, `elapsedMs` on the wire, the eleven surfaces of D11.

## Learning hypothesis

**Disproves that D9's probe-loop prohibition was about cost.**

D9 refused background probing for two stated reasons, both about magnitude: shared tracker rate limits,
and not adding a second scheduler. Both magnitudes are now measured — a probe is one or two requests
(S27) against a refresh that already pages the same connection every sixty minutes (S28), and the
product already runs five hosted services (S29).

If it succeeds, the staleness rule makes the steady-state cost **zero** additional outbound calls on an
instance where every connection is attached to something, and the prober only ever spends requests where
nothing else was looking.

If it fails — a real instance trips a rate limit, or the prober's writes contend with refresh-outcome
writes badly enough to matter — then D9 was right for a reason it did not state, and the honest answer
is that connection health cannot be self-maintaining and `Unknown` is a permanent resting state the UI
has to live with. That is a much worse outcome than the cheap fix suggests, which is why it is named
before the cheap fix hides it.

## Acceptance criteria

See US-08A through US-08C in `feature-delta.md` — 20 ACs. The three carrying the risk:

- **AC-08B.3** — a connection whose verdict is **fresher** than the threshold is **not probed**, asserted
  by the connector *not* being called. A naive "probe everything on a timer" passes AC-08B.1 and
  AC-08B.2 and fails this one, and this one is the whole of why D20 is defensible where D9's loop was
  not.
- **AC-08B.7** — under more than one replica, one pod probes per window, not each. Mechanism is open
  (OQ-08.1); the criterion is not.
- **AC-08A.6** — a broken OAuth grant still reads `AuthenticationFailed` after a successful refresh
  records `Healthy`. `Describe` folds the credential row in, and a refresh succeeding on a different
  authentication method must not paint a tick over a dead grant.

## Dependencies

- Slices 01–07 all delivered and pushed.
- **Bug #6010 is a dependency, and is inside this slice** rather than beside it (D22). A prober shipped
  against an unfixed #6010 runs known-throwing code against every Azure DevOps connection every tick,
  and fails silently into `Unknown` — the one state D9 worked to keep honest.

## Effort

**One day**, unevenly: US-08A about an hour, US-08C unbounded until somebody looks, US-08B most of the
rest. Ship order US-08A → US-08C → US-08B; each lands on its own.

**Split trigger**: if #6010's root cause is not identified within about an hour, US-08C leaves this slice
and becomes its own, and the prober ships with the Azure DevOps gap recorded.

**The trigger did not fire.** DISTILL found the root cause in one test, with no network and no
credential: `AzureDevOpsWorkTrackingConnector.ValidateConnection`'s first line reads a connection option
outside that method's own `try`, and throws `ArgumentException` when the option row is absent. The
Task Manager hits it and the connection screen does not because the screen validates a connection built
from the DTO the browser just sent, while Test connection validates the stored entity. Jira's
`ValidateConnection` opens with the identical unguarded line. The slice does not split.

## Reference class

Slice 05, which built this subsystem. It made *failure* legible for every authentication method and
stopped there; this slice closes the half it left — nothing yet distinguishes *no failure* from *nobody
asked*. Slice 05's own estimate held; its risk was the classification signal, and that signal now exists.

## Pre-slice SPIKE

**None — and the offered one was declined** (D23). A timeboxed probe against real trackers was put up to
settle the rate-limit question before fixing a cadence. S27 and S28 are counts taken from the code rather
than estimates, and under D20's staleness rule the quantity a spike would measure is, in the common case,
zero. There is nothing left to measure.

DESIGN is **not** skipped, and **ran on 2026-09-16**. All five mechanism questions are closed:

- **OQ-08.1 → D26** — the verdict row is the claim. A conditional `UPDATE … WHERE ObservedAt < cutoff`
  picks the replica; the table's existing unique index settles the insert race. No lock, no leader.
- **OQ-08.2 → D25** — the threshold is **derived**: 2 × max(team, features) refresh interval. No
  AppSetting, no migration, no dial that can be set below the interval it has to exceed.
- **OQ-08.3 → D27** — no back-off. A broken connection is asked on the same cadence, so recovery is
  bounded by one window.
- **OQ-08.4 → D26** — the claim means one probe in flight per connection, so the two writers cannot
  collide on anything but two honest observations seconds apart.
- **OQ-08.5 → D29** — the prober is its own hosted service. Behind the single lane, one wedged connector
  would silence health checking exactly when health is what you need.

Design shape: one new `BackgroundService` that owns *when*, one new method on `IConnectionHealthService`
that owns *what*. `ConnectionHealthSingleWriterArchUnitTest` passes **unmodified** — it is a hard gate on
this slice. **No EF migration** — the table, its columns and its unique index all already exist.
