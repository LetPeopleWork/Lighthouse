# ADR-239: A connection save that moves the fetch fingerprint starts the affected updates

**Status**: Accepted (maintainer, 2026-10-16). The decision to start updates on a connection save was taken by
the maintainer at DISCUSS (2026-10-15); the mechanism below was proposed at DESIGN (2026-10-15) and accepted once
built.
**Feature**: `story-6307-refresh-on-additional-fields-change` (ADO User Story #6307, split out of Bug #6306)
**Decider**: Morgan (Solution Architect); accepted by the maintainer 2026-10-16
**Narrows**: ADR-030 ("expensive ⇒ intentional") for connection saves. **Amends**: ADR-140's consequence "A connection
change fans out … passive".

---

## Context

A connection's additional field definitions are part of every owner's fetch fingerprint (ADR-140). Adding a field
therefore already makes every Team and Portfolio on the connection download everything, but only at its next
scheduled cycle, up to about four hours later with default settings. In that window a rule on the new field matches
nothing and nothing says why (Bug #6306, where the maintainer himself was caught by it).

Two earlier decisions stand in the way of starting those updates at save time:

- **ADR-030** made an expensive recompute wait for the user's click ("expensive ⇒ intentional").
- **ADR-140** chose a *passive* fan-out for connection changes: each owner notices at its own next cycle, keeping
  the connection save free of an N-entity write and of the partial-failure question that comes with one.

At the same time the Team and Portfolio settings saves already start an update of their owner (`refreshOnSave`,
browser-driven). The maintainer ruled on 2026-10-15 that a connection save follows that precedent (story 6307 D1),
immediately by default (D2), on every licence (D4), with no size threshold (D5). The browser asks first, with a
`Save only` choice (sketch decision S1).

Facts that shape the mechanism, read from the code at `17428144f`:

- A connection PUT changes only connection-owned fingerprint inputs (`AdditionalFieldDefinitions`; the PUT never
  assigns `WorkTrackingSystem`). Owner-owned inputs are untouched.
- `IDomainEventDispatcher` runs handlers after commit, each isolated; `TeamDeleted` already carries the ids its
  handler acts on.
- `UpdateServiceBase.TriggerUpdate` enqueues through `UpdateQueueService`, which coalesces a trigger on an admitted
  key into one follow-up run.
- The CLI and MCP clients save connections through the same PUT, so a browser-side trigger would miss them.

## Decision

1. **The connection PUT compares the connection's share of the fetch fingerprint before and after the save.** The
   share is rendered by one pure function, `FetchFingerprint.ForConnection(connection)`, which `FetchFingerprint.For`
   itself uses, with `For`'s output byte-identical to today's. "Before" is the connection as stored when the PUT
   runs, rendered before any mutation of the tracked connection; "after" is taken once `Save()` has returned. If the share moved, every Team and Portfolio on that connection
   moved; if it did not, none did.
2. **When it moved, the PUT publishes `ConnectionFetchShapeChanged(ConnectionId, TeamIds, PortfolioIds)`** on the
   existing domain-event bus. A handler triggers each Team, then each Portfolio, through the existing updaters.
3. ~~**The PUT response reports what it asked for**: the connection as today plus `queuedUpdates { teams, portfolios }`,
   on a response-only DTO derived from the connection DTO.~~ **Superseded by sketch decision S3 (2026-10-15):** after
   Save the page navigates to `/` with no notice, so nothing reads the counts. The PUT response is unchanged.
4. **A save the server refuses publishes nothing** (400 validation, 403 licence, 409 concurrency, 404), because
   publishing happens only after a successful `Save()`.
5. **Reading a connection never publishes.** Predefined-field registration on the connection GET and on the Jira sync
   path are left to each owner's next scheduled cycle, as today.
6. **"Save without updating" travels as the query parameter `startUpdates=false`** (sketch decision S1: it ships).
   Its default is `true`, so the CLI and MCP clients update now; the browser sends `false` when the admin chooses
   `Save only`.
7. **The queue is reused unchanged.** An owner already running gets one follow-up; an owner already waiting keeps its
   one row and runs once more after its waiting run. Only one of those runs is a full download caused by the save.
8. **The connection list read carries `usedBy { teams, portfolios }`** for each connection (sketch decision S4), so the
   browser can ask before Save, in the configured Terminology, with counts. The edit page reads its connection from
   the list and never calls the read by id, which is also the read that registers predefined fields. The counts
   come from one read-only port, because the list controller cannot take two more repositories without exceeding
   the constructor-parameter limit the build enforces. No extra fetch, so no new loading or could-not-load look.
9. **A stored save answers 200, whatever happens after it.** A throwing handler is caught and logged by the
   dispatcher; the controller's own owner lookup and publish after `Save()` are caught and logged the same way.
   Recovery is each owner's next scheduled cycle.

*2026-10-15: the maintainer's UI sketch decisions (S1–S7, story 6307 feature-delta) override DESIGN's DDD-6, DDD-7 and
DDD-13: no `queuedUpdates` response, no after-save notice, navigation to `/` as today; the question is asked before
Save instead.*

## Alternatives considered

- **Keep the passive fan-out (ADR-140 as written), add a visible "waiting for an update" marker.** Consistent with
  both earlier ADRs. Rejected by the maintainer at DISCUSS: it still leaves the window open and the step to remember.
- **Start the updates from the browser after Save, like `refreshOnSave`.** Rejected: CLI and MCP saves would not get
  the behaviour.
- **Call `TriggerUpdate` inline in the controller.** Rejected: it couples the API to the updaters; the maintainer
  prefers the event bus, and the bus already isolates a failing reaction from the committed save.
- **Compare each owner's stored fingerprint with its current one.** Rejected: it would also start owners that owed a
  full fetch for an unrelated reason, which this save did not cause.
- **Compare each owner's full `For()` before and after.** Same answer as the decision, at the cost of loading every
  owner before the merge. Rejected as unnecessary.
- **Make the queue absorb a trigger on a key that is still waiting.** It saves one cheap run per waiting owner, but
  changes behaviour for every trigger source and reverses a hard-won rule in `docs/ci-learnings.md`. Rejected.

## Consequences

**Positive**

- A field added to a connection reaches every Team and Portfolio on it within one queue round of Save, on every
  client that saves connections.
- The decision of *which* owners is the fingerprint's own, so a credential, URL-option, authentication-method, name or
  write-back-only save starts nothing.
- No new technology, migration, permission or licence gate.

**Negative / accepted**

- A label-only rename of a field starts a full download of every owner (the label is part of what is stored). Accepted
  by the maintainer.
- On a large instance one field save can hold the queue for a while. The Task Manager's per-row stop is the relief
  valve; no threshold.
- The browser's question counts the Teams and Portfolios *on* the connection, before anything is queued. A save during
  a database-maintenance window, or a handler that throws, queues fewer updates than that; each such owner still
  downloads everything at its next scheduled cycle, because its stored fingerprint was never rewritten. (Revised
  2026-10-15: the earlier consequence about counts in the PUT response is dropped with decision 3.)
- In a multi-replica deployment a follow-up parked on one pod for a key running on another can be lost (the gap
  recorded under ADR-076). Recovery is the same next scheduled cycle.
- `FetchFingerprint.For` is refactored around `ForConnection`; a pinned-digest test guards that no stored fingerprint
  moves on upgrade.

## Enforcement

- Unit: `ForConnection` changes for an added, removed, re-referenced or relabelled field, not for a re-ordered list.
  A property-based test checks that `For(owner)` changes exactly when `ForConnection` changes, over random
  field-definition edits.
- Unit: a pinned digest for a fixed owner, committed before the refactor.
- Architecture: `FetchShapingPropertyGuardTest` gains the connection-scoped subset, so a new connection-level
  fetch-shaping property cannot be missed by `ForConnection`. The existing ArchUnit rule that handlers do not call
  `GetRequiredService` covers the new handler.
- Acceptance (WebApplicationFactory): PUT to queue, observed through the queue's status store, for each refusal path
  and for coalescing.

## Cross-reference

- [ADR-030](./adr-030-dependent-data-reload-after-autosave-cost-based-split.md): narrowed for connection saves.
- [ADR-140](./adr-140-fetch-fingerprint-on-the-config-aggregate.md): its passive fan-out consequence is superseded
  for saves through the connection PUT.
- [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md): the event bus used.
- [ADR-076](./adr-076-cluster-aware-update-queue.md): coalescing and its cross-pod gap.
- Full analysis: `docs/feature/story-6307-refresh-on-additional-fields-change/feature-delta.md` (Wave: DESIGN).
