# ADR-190: Usage data events are detected in the browser and forwarded by our own backend, over a wire that can only carry a closed vocabulary

- **Status**: **Proposed** (DESIGN, 2026-09-12)
- **Supersedes**: [ADR-174](./adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md)
  (partially — see "What this inherits from ADR-174")
- **Date**: 2026-09-12
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slice 01c and slice 04)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

ADR-174 designed the gate for a **daily backend heartbeat**. That heartbeat is abandoned: ADO #5975
is Removed, and the instance facts it existed to carry become properties attached to product events
instead. What is left is the thing the Epic was always for — knowing whether a shipped feature is
used — and that signal only exists where a person clicks, which is the browser.

So the event has to start in the browser and the question becomes how it gets out. Three
constraints, none of them negotiable, and they pull against each other.

**No third-party JavaScript.** `posthog-js` would put a vendor's code in the page, and the audience
is engineers: SPIKE-00 established that `i.posthog.com` and `eu.i.posthog.com` are blocked at
whole-domain scope by AdGuard and at ingest-path scope by EasyPrivacy, which uBlock Origin enables
by default (`spike/findings.md:184-192`). A browser-to-vendor path would produce a dataset biased
by exactly the population we most want to measure, it would work only where the browser can reach
the vendor, and it would hand payload control to an SDK.

**The frontend cannot be the enforcement point.** Whatever endpoint the browser posts to is
anonymous — authentication is optional in Lighthouse and impossible in standalone, and with it off
every caller shares the subject `lighthouse|auth-disabled`
(`Services/Implementation/Auth/DisabledAuthenticationHandler.cs:15`). Hiding a button is UX. The
ingest endpoint must assume its caller is hostile and check consent itself, on every request.

**The browser is the one thing that knows the route, and the one thing that must not be trusted with
it.** `/teams/42` is a customer's entity id. A design in which the browser is trusted to send
`/teams/:id` instead is a leak waiting for the first developer who reaches for `window.location`.

There is a fourth thing ADR-174 got right and stated ahead of time: *"the constraint becomes real
only at slice 04, where product events are per-user-action rather than daily. So the question is not
'cache or not', it is 'what may a cache be allowed to get wrong'."* That granularity is now the
**only** granularity, so the question ADR-174 deferred is the question this ADR has to answer.

## Decision

**The browser detects; our backend verifies, enriches and forwards. The wire between them carries
closed enumerations and nothing a customer could recognise.**

```
browser detector  --POST /usagedata/events-->  ingest gate  -->  enricher  -->  drain  -->  PostHog
   (in-memory,        closed vocabularies,      fresh DB       instance      bounded      $ip: null
    no storage)       token in a header         read per        properties   in-process
                                                batch                        channel
```

### 1. The wire carries keys, not strings

The ingest DTO has **no free `string` property at all**. Two closed enumerations and two bounded
integers:

| Field | Type | Why |
|---|---|---|
| `Name` | `UsageDataEventName` enum | The documented event set. An undocumented event is a `400`, not a defect found in review |
| `Route` | `UsageDataRouteKey` enum | A *key*, not a path. The server owns the `/teams/:id` string that PostHog sees |
| `OffsetMs` | `int?` | Milliseconds before flush, for intra-batch ordering. Non-negative, bounded by the flush interval |
| `Sequence` | `int?` | Position in the batch, as a tiebreak |

This is the whole enforcement of the route-pattern requirement, and it is structural rather than
defensive. A client that sends `/teams/42` is not normalised — it does not deserialise, because that
string is not a member of the enum. There is no regex whose completeness the promise depends on, and
no moment at which a real entity id exists inside our own process to be caught by request logging on
the way past. **The bug class "a customer's entity id reached the collector" is not mitigated here;
it is unrepresentable.**

The server holds the key-to-pattern map, so the pattern PostHog receives is a literal in our source.
Widening either enum is a docs change in the same commit — see the enforcement table.

**The route table makes this sharper than a toy example suggests.** Lighthouse's routes are declared
at `App.tsx:203-226`, and two of them are `/teams/:id/:tab?` and `/portfolios/:id/:tab?`
(`App.tsx:213,218`). Those are the only routes carrying **both** a customer entity id and a
user-meaningful second segment, so they are the case that decides whether this design works: the `:id`
must be dropped because it identifies a customer's Team, and the `:tab` must be **kept** because which
view someone opened is one of the few things worth knowing. A key set whose members are
`(entity type, tab)` pairs — `TeamDetail_Metrics`, `PortfolioDetail_Settings` — does both at once, and
does them in the browser, so `/teams/42/metrics` is not normalised late; it never leaves the page.

This is why the first event the pipe carries is deliberately one that fires on those routes. Proving
the mechanism on `/features` would prove nothing: there is nothing there to strip.

**Both enums are value types on a `[FromBody]` DTO, and every one is therefore declared nullable
with the absent case rejected explicitly.** `csharpsquid:S6964` has fired on this exact shape three
times in this repository, and the fix that is *not* available is `[JsonRequired]`: adding it to a new
property makes every payload written before it return `400`
(`docs/ci-learnings.md:773-777`). Nullable plus an explicit check at the read site is the house
answer.

### 2. Consent is resolved from the token, on every batch, with no cache

The browser presents the same `X-Lighthouse-UsageData-Token` header the shipped consent endpoints
already use (`API/UsageDataController.cs:25`). The gate resolves it through
`FindByTokenHashAsync` — one lookup on the unique index at `Data/LighthouseAppContext.cs:166-168` —
and permits the batch only when the row reads `Granted` and its `LastSeenAt` is inside the liveness
window ([ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md)). The same
request refreshes `LastSeenAt` through the existing throttled `TouchAsync`, so an emitting browser
keeps its own consent alive for free.

**The cache question ADR-174 deferred is answered: still no cache.** Not because caching is wrong in
principle, but because the number is not there. A browser flushes at most twice a minute; the gate
is one indexed read on a unique index, which is the same read the state endpoint already performs
hourly from every open tab. There is no cost to avoid, and the only failure a cache could buy is
sending after a revoke.

The gate is **fail-closed** and never throws. A database failure, an absent token, an unknown token,
a declined or revoked row, a stale row, the master switch off, a cancellation — every one of them
resolves to "do not forward". `UsageDataSuppressionReason` is a closed enum:
`MasterSwitchOff`, `NoLiveConsent`, `RateLimited`, `BudgetExhausted`, `EvaluationFailed`.

**The response is `204 No Content` in every case, permitted or not.** The endpoint must not tell a
caller whether the token it presented is real; that is the same non-oracle rule the shipped
`DELETE /consent` already follows and for the same reason (`API/UsageDataController.cs:83-86`). A
malformed body is the one exception and answers `400`, because that is a bug in our own client, not
a probe.

**The token is never validated for shape, and `400` is reserved for the body.** This is the rule most
likely to be helpfully broken by someone adding a guard clause, so it is stated rather than left
implicit: a token that is empty, truncated, the wrong length, or not base64url does **not** earn a
`400` and does not short-circuit. It fails to resolve and lands on the same `204` as a token that was
never minted. The moment a malformed token answers differently from an unknown one, the endpoint
becomes an oracle for token *shape*, and shape plus a rate limit is the start of an enumeration.

**The residual is timing, and it is the same residual the shipped state endpoint already carries.**
That controller's own comment says it *"does measurably more work when it is given a token than when
it is not, and an attacker who can ask without limit could average that difference out"*
(`API/UsageDataController.cs:30-33`). Ingest has the same property and a smaller gap — a resolving
token costs a queue write, a non-resolving one does not. It is **not** closed by constant-time work,
which would be disproportionate for a pseudonymous analytics endpoint. It is bounded by the same
thing the state endpoint bounds it with: the rate limit, which is why §4's limiter applies to this
endpoint even though it exists mostly for volume.

**The liveness window is ADR-173's and is currently 30 days**
(`Configuration/UsageDataConfiguration.cs`, `ConsentLivenessWindowDays`). It is named here rather than
only cross-referenced because it is a shared constant across two ADRs: shortening it in ADR-173
shortens the horizon over which a browser may emit without re-presenting recently, and that
consequence lands in this design rather than in that one. A change to the window is a change to both.

### 3. Revocation is checked at both ends of the queue

Accepted batches go onto a **bounded in-process channel**, drained by a hosted service. Nothing is
persisted: no outbox table, no browser-side queue in `localStorage`. That is not laziness, it is the
answer to "what happens to queued events when consent is revoked mid-flight" — a durable queue
survives the revocation and sends afterwards, which is precisely the behaviour AC-03.2 forbids.

Three rules make the window shut:

- The browser's pending buffer is **in memory only** and is discarded before the revoke request is
  sent. A closed tab loses its unflushed events, which costs nothing.
- The gate runs **at accept**.
- The gate runs **again at drain**, keyed on the token hash the queued batch carries. A revocation
  that commits between accept and drain suppresses the batch. The re-check is one more indexed read
  per drain cycle.

So the drain window is bounded by the drain interval rather than by anything the browser does, and
"the next emit after a revocation does not happen" holds without a restart, without a cycle
boundary, and without a cached copy that could disagree with the row.

**What this does not achieve, stated plainly.** A batch that has already been handed to `HttpClient`
when the revoke commits will complete. The window is one in-flight request, single-digit hundreds of
milliseconds. It cannot be closed by any design that sends over a network, and pretending otherwise
would be the kind of claim ADR-174's honesty note exists to prevent.

**The forwarder's contract, specified rather than described.** "A hosted service that drains" is not
enough for someone to implement against, and it is the component in this design with the largest
declared effect, so it gets a contract shape rather than a sentence:

| | |
|---|---|
| **Type** | `UsageDataForwardingService : BackgroundService`, registered `AddHostedService`. The house precedent for instance-scoped background work — `GracefulShutdownService`, `KeyRingFileWatcher` — not `UpdateServiceBase`, which is per-entity and queue-backed |
| **Contract shape** | **bounded-change** |
| **Universe** | the `Channel<UsageDataEventBatch>` it reads, and one named `HttpClient`. Nothing else. It writes no database row, publishes no domain event, and touches no other service's state |
| **Declared delta** | the channel is emptied; PostHog receives zero or more events |
| **Retry** | **none.** A failed publish drops the batch. Retrying is a second chance to send something whose consent may have changed in the meantime, and a usage event that is lost costs nothing — the same reasoning that rejects a durable outbox |
| **Failure** | never escapes the loop. A publish that throws is caught, counted, and the loop continues. An exception that ends the drain loop would silently stop usage data for the process lifetime |
| **Shutdown** | cooperative and non-blocking. On `StopAsync` the loop exits and whatever is still queued is **discarded**, not flushed. Flushing on shutdown would mean sending after the last chance to check consent, and would put a vendor's latency on a container's shutdown path |
| **Cycle** | drains continuously with a short idle wait when the channel is empty. The interval is not load-bearing — it bounds the accept-to-drain revocation window, so shorter is strictly better and the only cost is wake-ups |

`IUsageDataEventQueue` is the seam the controller writes to, so the controller depends on an
interface rather than on a `Channel<T>`, and a test can assert what was enqueued without running a
hosted service.

### 4. Two ceilings, because they bound different things

**Per browser, per instance**: a `UsageDataIngestPolicy` fixed-window limiter, following
[ADR-005](./adr-005-rate-limiting-middleware.md) and every other anonymous endpoint in this codebase.
It is partitioned on the **digest of the presented token**, falling back to client IP when no token
is present — not on IP alone, because fifty people behind one corporate NAT share an address and
would throttle each other. The digest is computed from the header without a database read, so it
costs nothing at the limiter.

**The trap to avoid when wiring it**: `ConfigureRateLimiting` returns
`RateLimitPartition.GetNoLimiter` for a policy name that has no entry in configuration
(`Program.cs:1159-1162`). A policy declared in `RateLimitingConfiguration` and forgotten in
`appsettings.json` is silently unlimited. The `appsettings.json` entry ships in the same commit as
the constant, and a test asserts the policy resolves to a real limiter.

**Per instance, per day**: a configured event budget, above which events are **dropped silently**
rather than refused. This exists because the thing actually at risk is not a customer's box, it is
the maintainer's single PostHog allowance, which is shared across every instance in the world. A
per-IP limiter cannot see that; forty honest instances can exhaust the quota without any one of them
misbehaving. Dropping rather than refusing is the fail-safe direction and matches the reliability
definition this feature already ships under: silent degradation, no user-visible failure, no retry
storm.

**Silent to the caller is not silent to the operator, and conflating those two would be a defect.**
The drop is invisible to the browser on purpose — there is nothing useful it could do and a `429`
would only invite a retry. But an instance that exhausts its daily budget is either unusually busy or
being abused, and those are indistinguishable from the outside unless the instance says something. So
exhaustion emits **one** log line per day at warning level, naming the budget and the count, and the
day's drop count is included in the operator-facing signal described in §8. Once per day, not once
per drop: a per-drop log is a flood mechanism wearing a monitoring costume, and an attacker who can
trigger it has turned the defence into the attack.

### 5. What the abuse surface actually is, once the gate is in place

It is worth being precise, because "an anonymous ingest endpoint on a self-hosted box" sounds worse
than it is and also better than it is.

- **Not "anyone on the network".** An unauthenticated caller with no token gets `204` and nothing is
  forwarded. To emit, you need a token that resolves to a live grant on that instance — so you have
  either clicked Yes in a browser there, or taken a token out of someone's `localStorage`, which
  requires script execution on the origin and at that point the usage-data endpoint is not the
  interesting thing you have.
- **What a legitimate consenter can do**: emit plausible-but-false events up to their rate limit.
  This is accepted and is not defended against. The dataset is advisory — it informs where to spend
  effort. It is not billing, it is not an audit trail, and no control in the product depends on it.
- **What nobody can do, by construction**: put a customer's entity id, a work item title, a query, a
  URL, an email address or any other free text into the collector, because the DTO has no field that
  could carry one.

### 6. The instance properties, and where the identification line sits

The backend attaches, server-side, to every forwarded event: Lighthouse version, deployment mode
([ADR-177](./adr-177-deployment-mode-is-a-usage-data-owned-closed-value-set.md)), licence tier, and
whether authentication is enabled. All four already exist —
`ILighthouseReleaseService.GetCurrentVersion()`, the resolver ADR-177 specifies over
`IPlatformService`, `ILicenseService.CanUsePremiumFeatures()`, and
`IAuthModeResolver.Resolve().Mode` (`Services/Implementation/Auth/AuthModeResolver.cs:34-72`).

**`auth-enabled` earns its place on a smaller basis than the one it used to have.** It was carried
because auth-off collapses every caller onto one subject, which mattered when counts were to be
derived from the subject. They are not: the unit is a browser pseudonym and the backend subject never
enters a count. What survives is narrower and still worth knowing — it is the only signal of whether
*"one browser ≈ one person"* is plausible on that instance. On an auth-on instance people have their
own logins and probably their own browsers; on an auth-off instance a shared kiosk is a live
possibility. Without it, every per-browser number is uninterpretable.

**These four fields are a residual linkage surface, and that is not the same as being nothing.**
Individually each is low-cardinality: deployment mode is six values, tier is two, auth-enabled is
two. Jointly, across a large population, the tuple is an aggregate. For a rare tuple it is not — a
standalone MacOS instance on an unusual version narrows to a handful of candidates for anyone who
already holds the dataset and independently knows that a particular customer runs that
configuration. It does not *identify*; it *reduces the candidate set*, which is a different and
weaker claim, and the difference is worth stating rather than glossing.

Version is where nearly all of that cardinality lives, and it is the one that is bounded:

- **A released version is shared by everyone on that release**, so it carries no individuating
  information at all. This is the normal case.
- **An unreleased or locally built version string is close to unique.** So the publisher emits the
  version only when it matches the shape of a published release, and emits the literal `unreleased`
  otherwise. That costs nothing anyone wants — nobody is planning a deprecation date against a
  developer's working tree — and it removes the single highest-entropy field for the small
  population most likely to be individually recognisable.

With that bound, everything except released-version is at most twenty-four combinations, and
released-version is a value thousands of instances share.

**What is not bounded, and must be disclosed rather than engineered around**: per-event emission
produces an activity trace that a once-daily heartbeat did not. The vendor stamps arrival time, so
rounding our own timestamps would be theatre. Working hours, and therefore an approximate time zone,
are inferable from a browser that emits over weeks. That is a real widening of what the collector
learns compared with the abandoned design, it cannot be removed from our side, and it belongs in the
usage data page in plain words.

### 7. The collector host stays named in exactly one place

Unchanged from ADR-174 point 7 and from
[ADR-176](./adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md):
one constant on `PostHogUsageDataPublisher`, so an architecture rule has a name to point at.
`$ip: null` and `$geoip_disable: true` ride every event, and ADR-176's payload assertion is now
load-bearing at a far higher volume than the one heartbeat a day it was written for.

### 8. What the system says about itself, and what it costs

ADR-174 allowed the emit path *"at most one log line per attempt"*, which was the right budget for
one attempt a day. At per-event granularity that rule inverts: the thing it was protecting against —
log flooding — is now the likely outcome of following it. A revoked browser with a stale tab open
would emit a suppression line every flush, forever, and the instance most likely to fill a disk is the
one whose owner already withdrew.

**So suppression is counted, not logged.** Each `UsageDataSuppressionReason` has a counter. A single
line per reason per day carries the count, at `Debug` for policy outcomes (`NoLiveConsent`,
`MasterSwitchOff`) and `Warning` for the two that mean something is wrong (`EvaluationFailed`,
`BudgetExhausted`). `EvaluationFailed` is the one an operator must be able to find, because it is the
only reason that means the feature is broken rather than switched off.

The forwarder reports the same way: publishes attempted, publishes failed, batches dropped on
overflow, and the last probe outcome. Four numbers, once a day. **Nothing about usage data appears on
the operator's refresh-status surface**, which is for work a person is waiting on.

**The cost, with numbers rather than an assurance — and the number is not flattering, so it is
stated.** One ingest request costs one indexed lookup on the unique `TokenHash` index plus at most one
throttled `UPDATE` (the shipped touch already writes a few times a day per browser regardless of
request count). A flush only happens when there is something to flush, so an idle tab costs nothing;
but a tab being actively used, flushing every thirty seconds, is up to **120 lookups an hour against
that index — roughly 120× the shipped state endpoint's hourly poll**, not a rounding difference on it.

Ten people using one instance at once is therefore on the order of 1,200 indexed reads an hour at the
worst, against a unique index on a table with one row per browser ever asked. That is still a small
number in absolute terms and it is the honest one; the earlier draft of this paragraph said "twice",
which was arithmetic done in the direction of the conclusion.

Two things bound it if it ever matters, neither of which is built now: the flush interval is one
constant, and **a cache, if added, goes inside the gate and may only suppress**, per §4 of ADR-174,
inherited.

The one shape worth watching is SQLite, where writers serialise process-wide. The touch is the only
write on this path and the shipped conditional update already throttles it to a few per browser per
day, so ingest adds **reads** rather than write contention — which is the half of SQLite that scales.

## What this inherits from ADR-174

ADR-174 is superseded, not discarded. The parts that were right about a pipe rather than about a
heartbeat are carried forward verbatim in shape:

| ADR-174 | Inherited? | Note |
|---|---|---|
| §1 Uncached, fail-closed gate consulted on every emit | **Inherited** | Now per batch instead of per day, which is the case ADR-174 said would make the question real |
| §2 `UsageDataEmitPermit`, sealed, gate-minted, required by the publisher | **Inherited** | Including ADR-174's own correction: it prevents emission *by omission*, it is not a compile-time guarantee, and `UsageDataEmitSeamArchUnitTest` is a test |
| §3 Every uncertainty resolves to "do not send" | **Inherited**, reason enum revised |
| §4 A cache may only ever suppress | **Inherited** as a standing invariant; still no cache is built |
| §5 The domain-event bus is not in this path | **Inherited**, with a different reason — see the reuse verdict |
| §7 Collector host named once | **Inherited** |
| §6 Emitter is a plain `BackgroundService`, not `UpdateServiceBase` | **Moot** — there is no scheduled emitter. The drain is a hosted service, and `UpdateServiceBase` is still the wrong base for it, now for a smaller reason |
| §8 `UsageData:LastHeartbeatDay` compare-and-swap, and the whole replica-multiplication problem | **Dropped** — see below |
| `NoInstanceIdentifier` suppression reason | **Dropped** — see [ADR-191](./adr-191-analytics-identity-is-a-per-browser-pseudonym-never-on-the-wire.md) |
| The zero-leak honesty note | **Inherited and still true** |

**The replica problem dissolves rather than being solved.** ADR-174 §8 existed because a plain
`AddHostedService` runs in every replica, all replicas share one database and therefore one
identifier, and three replicas would emit three heartbeats a day under one `distinct_id` — silently
multiplying every per-day count. A browser-originated event enters through exactly one replica's
ingest endpoint and is forwarded once by that replica. Replica count no longer multiplies anything,
so the day key, its seeding requirement, its two silent failure modes and its Testcontainers test all
go away with it. This also closes the reuse verdict that DESIGN reopened on 2026-09-11 over
`UpdateQueueService` / `IUpdateExecutionLock`: neither is needed, and this time the conclusion
follows from the reason.

## Alternatives considered

- **`posthog-js` in the SPA, browser talks to PostHog directly.** The obvious, cheap option, and the
  one the vendor documents. **Rejected**, and the maintainer settled it: third-party script in the
  page, a dataset biased by ad blockers in exactly this audience, no payload control, nothing to
  enforce an administrator's switch against, and no signal at all from instances whose network
  cannot reach the vendor.

- **Browser sends the real path; the server normalises it to a pattern with regexes.** The
  straightforward reading of "emit route patterns". **Rejected.** It puts real entity ids inside our
  own process, where request logging, an exception message or a future middleware can copy them out
  before the normaliser runs, and it makes the whole privacy claim depend on a regex set being
  complete for every route anyone adds later. The closed key set moves the guarantee from a rule to a
  type.

- **Browser sends the pattern string it believes it matched, server validates against an allow-list.**
  Meaningfully better than the regex, and nearly as good as the enum. **Rejected on a narrow point**:
  a pattern that fails the allow-list still arrived as an arbitrary string, so the same
  logging-before-validation exposure exists, just smaller. The enum makes the arrival itself
  impossible. The cost of the enum over the allow-list is one generated mapping, which is close to
  nothing.

- **Synchronous forward inside the request** — the POST returns after PostHog has answered.
  **Rejected.** It couples a user's browser to a vendor's availability, and AC-04.5 requires no
  user-visible failure and no retry storm. It would also make the ingest endpoint's latency a
  function of a third party's, on a request a person's page is waiting on.

- **A durable outbox table drained by a background service.** The reliable option, and the shape this
  codebase uses elsewhere for work that must not be lost. **Rejected** on the requirement, not on the
  mechanism: a usage event that is lost costs nothing, and durability here is actively harmful. A
  persisted queue survives a revocation and survives a restart, so events accepted before a
  withdrawal would be sent after it. It would also put unsent analytics about customer behaviour into
  the customer's own database, which is a new category of thing for an operator to reason about. The
  right property for this queue is that it forgets.

- **Keep the daily heartbeat alongside the event pipe**, so instance counts survive. **Rejected by
  the maintainer's decision on identity** — see ADR-191. Without an instance identifier in the
  payload there is nothing for a heartbeat to be a heartbeat *of*, and its instance facts are already
  attached to every event.

- **An open event-name set, documented by convention.** **Rejected.** AC-08.2 makes an event that
  ships ahead of its documentation a defect. An open set makes that a review habit; a closed enum
  makes it a compile error in our own client and a `400` for anyone else's, and gives the docs
  comparison check one declaration to read.

## Consequences

**Positive**

- The administrator's switch becomes **enforceable** rather than advisory. With `posthog-js`, OFF
  could hide UI and ask an SDK to stop; a tab open since before the flip would keep sending. With
  ingest through our own gate, a stale tab's POST is accepted and dropped, and the screenshot the
  administrator takes to close their security review is true. This makes story #5836 materially
  stronger than it was.
- Revocation gets *better* than the abandoned design managed. ADR-173 had to admit that clearing
  browser storage stopped the heartbeat only after the liveness window, because the heartbeat had no
  browser behind it. Here the browser **is** the emitter: clearing storage stops emission instantly.
  AC-03.5's "clearing browser storage is equivalent to never having decided" is satisfied in full
  rather than in two respects out of three.
- Ad blockers stop biasing the dataset. The POST is same-origin to the customer's own Lighthouse.
- One event class, one pipe. Slice 04's marginal event is an enum member and a docs line.

**Negative / accepted**

- **A new anonymous write endpoint on every instance.** Bounded as described above, but it is new
  surface on a product that is frequently internet-exposed, and it did not exist in the abandoned
  design.
- **Unflushed events are lost on tab close, on drain-queue overflow, and on shutdown.** Deliberate.
- **The activity trace is finer than a daily heartbeat's**, and its time-zone inference cannot be
  removed from our side. Disclosed, not engineered around.
- **Per-event volume makes ADR-176's payload purity assertion and its canary matter more.** Both
  already exist; neither was sized for this volume and the free-tier allowance must be re-checked
  before slice 04 chooses its event set.
- **The frontend gains a detector that must not become a general-purpose tracker.** It is one module
  with a closed vocabulary and no storage, and the enforcement below is what keeps it that way.

**Reuse verdict**: `IUsageDataConsentRepository.FindByTokenHashAsync` / `TouchAsync` → **REUSE AS-IS**,
they are exactly the gate's two operations. `AnyLiveGrantAsync` → **DELETE**: it has no production
caller today (the shipped `UsageDataConsentService.GetStateAsync` reads `consent?.Decision` directly,
and the two `UsageDataConsentServiceTests` setups at `:62` and `:74` exist to assert the service
*ignores* it), and this design removes the only caller it was ever going to have. Its doc comment
says *"This is the whole of the question the emitter asks"*, which will be false the moment this
ships. `PruneStaleAsync` (`UsageDataConsentRepository.cs:63`) → **KEEP, and it too has zero production
callers** — interface, implementation and tests only, independently verified 2026-09-12. Without a
caller the consent table grows one row per browser ever asked, without bound. **It is not left
unowned**: it is assigned to the slice that introduces the unprompted ask, because that is the slice
that makes the growth real — before it, rows accrue only from people who went looking for the footer
icon; after it, every browser shown the dialog writes one, refusals included.
`RateLimitingConfiguration` + `ConfigureRateLimiting` → **EXTEND**, one policy, with the
unconfigured-means-unlimited trap named above. `IDomainEventDispatcher` → **UNCHANGED**, rejected as
the ingest-to-drain transport — not on its swallow-and-continue policy this time, which would
actually be tolerable for an event whose loss is free, but because it is a *domain*-event bus: routing
analytics through it would put a vendor adapter on the domain fan-out path that every future handler
pays for. `UpdateQueueService` / `IUpdateExecutionLock` → **UNCHANGED, verdict now closed**: the
replica-multiplication problem that reopened it does not exist here. `UpdateServiceBase<TEntity>` →
**UNCHANGED**, still the wrong base and now for a smaller reason: the drain has no entity to iterate.
`IAuthModeResolver` → **REUSE AS-IS** for the `auth-enabled` property.
`ILighthouseReleaseService.GetCurrentVersion()` → **REUSE AS-IS**.
`ILicenseService.CanUsePremiumFeatures()` → **REUSE AS-IS**. `useRbac` → **UNCHANGED, named as the
anti-pattern**: it fails **open** via `PERMISSIVE_SUMMARY` on purpose, and the ingest gate must fail
**closed**. A crafter meets both within a few files of each other.

**Enforcement**

| Rule | Mechanism |
|---|---|
| No path-shaped value can reach the wire | Compiler: the ingest DTO's route field is an enum. NUnit: a body carrying `"/teams/42"` returns `400` and forwards nothing |
| The published pattern for each route key is a literal we own | NUnit over the key-to-pattern map: every member maps, and no mapped value contains a digit |
| No event may carry free text | ArchUnitNET: the ingest DTO and the emitted event type expose no `string` property that is not enum-backed |
| An undocumented event cannot be emitted | Compiler for our own client; `400` for anyone else's. CI compares the declared enum against the list in `docs/settings/usagedata.md` |
| Value-type DTO properties are nullable, never `[JsonRequired]` | Sonar `S6964` plus a NUnit test posting a body that omits each field |
| The ingest policy is actually limited | NUnit asserting the resolved policy is not `NoLimiter`, because an unconfigured name silently is one (`Program.cs:1159-1162`) |
| Permitted and suppressed are indistinguishable to the caller | NUnit: a live grant, an unknown token and no token produce byte-identical responses |
| A revocation between accept and drain suppresses the batch | NUnit: accept, revoke, drain, assert the publisher was not called |
| Nothing publishes without a permit | Compiler (no zero-argument overload) plus `UsageDataEmitSeamArchUnitTest`. Not a compile-time guarantee — one assembly, `InternalsVisibleTo` |
| The gate never throws | NUnit: a repository throwing on every call yields `Suppressed(EvaluationFailed)` |
| Zero consent produces zero requests to the collector host | `DelegatingHandler` on the named client, plus the ArchUnitNET rule forbidding ad-hoc HTTP client construction that stops the assertion decaying into a tautology |
| The browser persists no event queue | Vitest: emit without flushing, assert `localStorage` and `sessionStorage` are untouched |
| Revoking discards the browser's pending buffer | Vitest: buffer events, revoke, assert no POST carries them |
| An unreleased version string never leaves | NUnit over the publisher: a non-release-shaped version emits `unreleased` |
| The per-instance daily budget drops rather than refuses | NUnit: exceed the budget, assert `204` and no publish |
| A malformed token is answered identically to an unknown one | NUnit parameterised over empty, truncated, over-long and non-base64url tokens: every response is byte-identical to the unknown-token response, and none is a `400` |
| Budget exhaustion is visible to the operator but cannot be used to flood | NUnit with a capturing logger: exceeding the budget a thousand times in a day produces exactly one warning line |
| Suppression is counted, not logged per event | NUnit with a capturing logger: a revoked token flushing repeatedly produces no per-request line |
| `EvaluationFailed` is findable | NUnit: a throwing repository produces a `Warning`, distinguishable from the `Debug` policy outcomes |
| The forwarder's loop survives a publish that throws | NUnit: a publisher that throws on every call leaves the service running and the channel draining |
| Shutdown discards rather than flushes | NUnit: enqueue, call `StopAsync`, assert the publisher was not called |
| The forwarder touches nothing outside its declared universe | ArchUnitNET: it may depend on the queue and the publisher port, and on no repository, no `DbContext` and no domain-event dispatcher |
| A failed publish is not retried | NUnit with a counting publisher double: one batch, one attempt |
| The whole revocation path holds end to end | Integration: grant, emit, assert published; revoke, emit, assert not published — without a restart and without a cycle boundary |
| The ingest DTO survives hostile bodies | Fuzz-style NUnit over the deserialiser: out-of-range enum ordinals, negative and oversized integers, absent fields, and a body an order of magnitude over the batch limit all produce `400` or a clamp, never an unhandled exception |
| ArchUnitNET fluent slice fields are concrete `GivenTypesConjunctionWithDescription` | Otherwise `CA1859` fails the Sonar gate — it has fired six times at once in one such file |

Cross-refs [ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md) (the record
the gate reads, confirmed as surviving),
[ADR-174](./adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md) (superseded; the
inheritance table above says which parts),
[ADR-176](./adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md)
(the adapter the permit is spent on, and the payload-carried privacy controls this raises the volume
of), [ADR-177](./adr-177-deployment-mode-is-a-usage-data-owned-closed-value-set.md) (one of the
instance properties, confirmed as surviving),
[ADR-191](./adr-191-analytics-identity-is-a-per-browser-pseudonym-never-on-the-wire.md) (the
`distinct_id` this pipe carries),
[ADR-005](./adr-005-rate-limiting-middleware.md) (the limiter, and its per-instance in-memory
residual).
