# ADR-207: A report is a response, not a record — the reality check stores nothing, and what a Report *is* gets decided when the second one arrives

**Status**: Accepted (2026-09-22 — Morgan, DESIGN wave, interaction mode PROPOSE). Ratifies decision D8
of `epic-4172-forecast-backtest-sweep`, taken in DISCUSS and confirmed from DIVERGE.

**Feature**: `epic-4172-forecast-backtest-sweep` — ADO Epic #4172, "Forecast Reality Check"
(internal codename "The Full Monte")

**Decider**: Morgan (Solution Architect)

---

## Context

The Forecast Reality Check replays a Team's own How-Many forecast against that Team's own completed
history at four sampling windows and four horizons — sixteen runs — and answers with one sentence. It is
the first thing Lighthouse has built that a user would naturally call *a report*: something you run, read,
and might want to keep, show to someone, or be emailed.

Six ADO Epics sit parked behind exactly that noun: **#1822** (PDF/CSV export), **#4753** (Export to PDF),
**#4754** (In-App Notifications), **#4755** (External Notification Channels), **#4155** (Signal
Notification) and **#4080** (Flow Health Check). None is scheduled. Every one of them would be cheaper if
a `Report` concept existed, and every one of them would constrain what that concept had to be. The
temptation is therefore to build the abstraction now, with one instance in hand, and let the other five
inherit it.

**There is no `Report` concept in the codebase or in this brief today, and that was checked rather than
assumed.** `ctx_search` skips `brief.md` for size, so DIVERGE could not confirm it and escalated the
question. It was resolved here with `grep` against the file directly: four case-sensitive occurrences of
`Report` in 8 747 lines, all of them ordinary English — a mutation report, a field report, surfaces that
"report success". No `Report` entity, aggregate, table, store, repository, kind or payload. The 206
existing ADR titles contain none either. The slate is genuinely blank, which is what makes this a decision
rather than an observation.

One further fact shapes the runner half of the question. `UpdateQueueService` is where any
"run it in the background and tell me when it is done" work would go. That queue holds **one**
`Channel<Func<Task>>` drained by **one** sequential reader that awaits each item to completion before
taking the next. [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) decided to split it
into three lanes; that change shipped and was **reverted the same day** (`f216ef558`) because concurrent
refreshes destroyed Feature ownership. The `story-5877-update-queue-lanes` section of this brief records
the revert. **ADR-195 itself does not** — it still reads `Accepted`, with a Forecast lane, and anyone
reaching for it to reason about queueing a report will reach a false conclusion. ADR-195's own field
report is the other half: a Portfolio refresh ran 77.7 minutes and starved every Team refresh for 3h38m.

## Decision

**Nothing is persisted. The Forecast Reality Check computes inside the request and returns its result as a
response body. There is no `Report` entity, no table, no migration, no `UpdateType` member, no queue work,
no runner registry and no notification seam.**

**The abstraction is deferred to the arrival of the second Report kind — not to a date, and not to a
quarter.** One instance is a feature; two instances are a shape. Six parked Epics are a roadmap, not a
commitment, and designing an abstraction against six imagined callers and one real one is how a `Report`
table acquires four nullable columns nobody can delete.

Three constraints bind the work that this decision does *not* build.

### 1. The accepted consequence, stated rather than discovered

- **"Has this Team ever been checked?" is unanswerable.** There is no record, so there is no history, no
  trend, no "your calibration drifted since June". Every run is recomputed from scratch.
- **Nothing can be emailed.** #4753 and #4755 need something durable to attach or link to, and it does not
  exist. This ADR does not make them harder; it declines to make them easier in advance.
- **A result dies with the browser tab.** Navigating away and returning shows no previous run. The
  synchronous shape is what makes that acceptable: the cost of re-running is a button press and seconds,
  not a queued job and a wait.

### 2. The forward-compatibility constraint the result shape must honour

**The response body must not hard-code a single Team into its top level.** "Check every Team at once" is a
plausible next unit of work, and nothing here may make it expensive.

Concretely: every fact about the checked Team — its id, its name, its current sampling window, its cells,
its verdict — lives inside **one self-describing envelope object**. A future multi-subject endpoint returns
a collection of that same object with no change to its interior. The failure mode this forbids is
flattening the Team's fields into the top level of the response, which turns "also check the other Teams"
into a breaking contract change.

This is **not** an instruction to return a collection of one today. A list with one element is speculative
generality and this project's SOLUTION EFFICIENCY rule rejects it at the first step. The constraint is on
the *shape of the envelope*, not on its multiplicity.

### 3. The runner question, answered for this product's queue specifically

**A short, read-only, user-initiated computation that contacts no work tracking system is a different kind
of work from a tracker sync, and it does not belong on the tracker sync's queue.**

This is a statement about `UpdateQueueService` as it exists at HEAD, not a general claim about reports.
Putting the reality check on that queue would make it wait behind whatever is refreshing and block
refreshes behind it, on a button a human is watching. The 3h38m starvation in ADR-195's field report is
what that looks like when it goes wrong, and the three-lane mitigation that would have separated forecast
work from refresh work is reverted and not in the tree.

So the reality check runs in-request. If the sixteen-run budget is missed (the open measurement R-1 /
AC-1.1), **the first contingency is to make the sweep cheaper, not to move it onto that queue** — see the
`epic-4172-forecast-backtest-sweep` section of this brief for the specific cost finding and the
in-request remedy.

## The six questions the eventual Report ADR will have to answer

Written down now, while the one real instance is in view, so the third Report does not have to
renegotiate what the second settled. The Forecast Reality Check's own answer is recorded beside each —
one data point, offered as a data point.

| # | Question | What this one instance suggests |
|---|---|---|
| 1 | **Is a Report an entity or a projection?** Does it have identity and a lifecycle, or is it a derived view recomputed on demand? | A projection. Nothing about this result is worth a row; it is a pure function of the Team's work items and today. |
| 2 | **Is the payload opaque JSON, typed per-kind columns, or a blob reference?** | Typed. This result has sixteen cells, four levels and a verdict, all of which the client renders structurally. Opaque JSON would forfeit the compiler. |
| 3 | **Does the runner reuse the update queue, sit beside it, or run in-request?** | In-request, for the reasons in §3 above. The answer is likely to differ per kind, which is itself the finding. |
| 4 | **Is completion a domain event or a direct push?** | Neither — there is no completion to signal, because the caller is holding the response open. The moment a Report takes minutes this question becomes real. |
| 5 | **What is the retention policy, and who deletes?** | Unanswered and deliberately so. A stored result raises GDPR-adjacent questions about Team names and work item counts that a response body does not. |
| 6 | **Is a Report scoped to a Team, to any entity, or global?** | Scoped to a Team here, but see the forward-compatibility constraint: the shape is written so that "all Teams" is additive. #4080 (Flow Health Check) is the one most likely to be global. |

When the second Report kind arrives, **these six get answered once, in a new ADR, with two real instances
on the table.** That ADR supersedes this one.

## Alternatives considered

### A. Build the `Report` abstraction now — entity, table, `Kind` discriminator, `UpdateType` member, queue runner

**Rejected.** It commits to all six questions above with one caller, and the caller is the least
representative one available: this report is synchronous, needs no runner, wants no retention and has no
completion event. Every answer it would supply is the answer a *notification* report would have to
overturn. The migration is the expensive half — expand-only is this project's rule, so a `Reports` table
shipped now is a table we keep whether or not the shape survives contact with #4754.

The Definition of Done for this Epic makes the rejection checkable: **a migration appearing in this Epic
is a signal that this decision was violated.**

### B. Persist only the last result per Team — no `Kind`, no runner, no lifecycle

The cheap middle. It answers question 1 of the six ("has this Team ever been checked?"), which is the one
consequence that actually stings.

**Rejected**, for two reasons. It still commits to a payload format (question 2) and a retention policy
(question 5) for a shape with exactly one instance, so it buys one answer at the price of two. And the
value is smaller than it looks: a stored result goes stale the moment a work item closes, so "last checked
on Tuesday" invites a user to trust a number that today's data would no longer produce. A reality check
whose whole premise is *checking* should not ship a cache of its own past claims.

### C. Hold the last result in process memory, keyed by Team

**Rejected.** It survives neither a restart nor a second replica, and `epic-5305-k8s-readiness` makes the
second replica a real target rather than a hypothetical. A cache that is correct on a laptop and silently
wrong in a cluster is worse than no cache, because nothing on screen distinguishes the two.

## Consequences

**Positive**

- No migration, no entity, no `UpdateType` member, no queue coupling. The Epic's blast radius is one
  controller action, one service, one policy and the DTOs they return.
- The feature is **read-only end to end**. With nothing stored and no control that writes a Team setting,
  a single permission (Team read) governs the whole surface and there is no differential rendering by
  permission anywhere in it.
- The six questions are on the record at the moment they were cheapest to see, rather than reconstructed
  later from a table's column list.
- The response shape is transport-agnostic: because it is a complete, self-contained envelope with no
  partial or streaming semantics, the same body can be returned synchronously today or as a completed
  job's payload later, without a field changing.

**Negative**

- No history, no trend, no "has this been checked". Accepted knowingly.
- Nothing to email, link to or attach. The export story (slice 03) mitigates this by building a Markdown
  one-pager **in the client** — following the client-side export precedent of
  [ADR-172](./adr-172-delivery-export-is-one-settled-table-the-caller-builds.md) and
  [ADR-162](./adr-162-export-header-block-as-generic-toolbar-input.md) — which is exactly why that
  mitigation is cheap and the durable emailable Report is not.
- The second Report kind pays the abstraction cost in full, including any retrofit of this one. That is
  the deliberate trade: one instance pays nothing, two instances pay once.

**Neutral**

- Re-running is idempotent in the only sense that matters here: it writes nothing. Two runs seconds apart
  may differ if a work item closed between them, and that is the correct behaviour for a check.

## Relationship to existing ADRs

| ADR | Relationship |
|---|---|
| [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) (update queue is three lanes) | Cited for its field report and for the single-lane reality at HEAD. **ADR-195 is stale**: its three lanes shipped and were reverted (`f216ef558`), the `story-5877-update-queue-lanes` brief section records that, and the ADR carries no status note saying so. Read the brief section, not the ADR, when reasoning about the queue. A status correction is owed to ADR-195 in its own right — the same correction ADR-127 received. |
| [ADR-194](./adr-194-sle-risk-is-a-number-per-item-never-a-background-ladder.md) (SLE risk is a number per item, never a background ladder) | Governs how a cell that cannot be evaluated renders. On this product's charts a blank region already means "too little history", so a calm result and an unevaluable one must never render alike, and "leave it empty" is the option that ADR already ruled out. Not amended. |
| [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md) (forecast data sufficiency is a backend signal) | The shipped bar this composes with. `ForecastDataSufficiencyPolicy.HasEnoughData` is called per cell, unchanged, and `MinimumActiveDays = 5` is neither changed nor duplicated. Not amended. |
| [ADR-172](./adr-172-delivery-export-is-one-settled-table-the-caller-builds.md) + [ADR-162](./adr-162-export-header-block-as-generic-toolbar-input.md) | The export precedent, and the reason it is client-side. There is no server-side document renderer in this product. **Caveat recorded in the brief**: the shipped `useDataGridExport` path is premium-gated, and this Epic is a Community feature, so the precedent is followed in *placement* (the client) rather than reused as code. |
| [ADR-181](./adr-181-update-activity-is-a-read-through-the-status-store.md) / [ADR-182](./adr-182-update-moments-in-a-sibling-hash.md) / [ADR-186](./adr-186-live-header-summary-sections-fetched-on-open.md) (update activity, moments, live header summary) | The prior art a future Report *status* surface should reuse rather than reinvent. `UpdateStatus` already carries `QueuedAt`/`StartedAt`, and `UpdateController` already answers "what is running / what is waiting / what is it waiting for / stop that one". Untouched here. |
| [ADR-046](./adr-046-survey-submission-and-team-notification.md) (survey submission and team notification) | The nearest existing precedent for an asynchronous completion notification. Worth reading before #4754 is designed. **Not** touched by this ADR. |
| [ADR-145](./adr-145-writeback-notification-suppression-visibility.md) (write-back notification suppression visibility) | Precedent for making an absent or suppressed signal visible rather than silent — the same instinct applied to a cell whose history cannot support a check. |
| [ADR-127](./adr-127-team-settings-advisory-channel.md) (the advisory channel reaches team settings) | **Not used.** Story #5612 deleted the channel; ADR-127 now carries a SUPERSEDED-BY-EVENTS status note recording that. Named here only so a later reader does not reach for it. |

**Not touched**: ADR-095 (migration before API) — there is no migration, which is the point.

## Revisit trigger

**The second Report kind.** Not a date, not a release, not a headcount.

A "Report kind" means: a user-initiated or scheduled computation whose *result* a user would expect to
keep, revisit, share or be told about. The six parked Epics name candidates; #4080 (Flow Health Check) is
the most likely to arrive first and the most likely to be global rather than Team-scoped, which is why
question 6 is on the list.

When it arrives, the six questions get answered once, with two real instances on the table, in an ADR that
supersedes this one.
