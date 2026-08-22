# ADR-174: The emit gate reads the database every time, fails closed, and hands back a permit that nothing else can construct

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slices 01, 03, 04)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Two conditions govern whether anything may leave the instance: at least one browser must hold live
consent ([ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md)), and the
`UsageData` optional feature must be on. The second suspends sending for browsers that already
consented, and turning it back on resumes them silently, so it is enforced at the emit path rather
than by hiding UI - a stale browser tab must not be able to keep sending across a flip.

The DISCUSS handoff and the slice-03 brief both frame the requirement as: *the flag must reach a
background emitter **without a per-emit database read***.

**That framing is a premature optimisation for the slice that actually ships first, and this ADR
declines it.** The slice-01 emitter runs once per day. A per-emit read is two queries per day. There
is no cost to avoid, and avoiding it would buy the one failure this feature cannot afford.

The constraint becomes real only at slice 04, where product events are per-user-action rather than
daily. So the question is not "cache or not", it is "what may a cache be allowed to get wrong", and
the answer has to be decided now because it determines the shape of the gate interface.

There is also a second problem with no obvious home. The zero-leak outcome requires proving that
*nothing* leaves an instance with no consenting browser, and there is no single outbound chokepoint
to observe: `GitHubService` builds its own `GitHubClient` as a field initialiser rather than going
through `IHttpClientFactory`, and only three named clients are registered. An assertion about the
absence of traffic has nowhere to attach.

## Decision

**One gate, read fresh, fail-closed, whose only success value is a permit that the publisher
requires and that nothing outside the gate can construct.**

1. **The gate is consulted on every emit and holds no state.**

   ```
   IUsageDataGate.EvaluateAsync(CancellationToken) -> UsageDataEmitDecision
   ```

   `UsageDataEmitDecision` is either `Suppressed(UsageDataSuppressionReason)` or
   `Permitted(UsageDataEmitPermit)`. Reasons are a closed enum: `MasterSwitchOff`, `NoLiveConsent`,
   `NoInstanceIdentifier`, `EvaluationFailed`.

   Evaluation reads the optional feature and asks `AnyLiveGrantAsync`. Both are indexed reads. At one
   emit per day that is two queries per day, and no cached copy exists that could disagree with the
   database. Staleness is not mitigated; it is **unrepresentable**.

2. **`UsageDataEmitPermit` is the capability, and only the gate can mint one.** It is a sealed type
   with an internal constructor, carrying the instance identifier and the evaluation timestamp. The
   publisher's signature is:

   ```
   IUsageDataPublisher.PublishAsync(UsageDataEmitPermit permit, UsageDataEvent evt, CancellationToken)
   ```

   A caller holding a `Suppressed` decision has no permit, so there is no argument it can pass.

   **What this buys, and what it does not - corrected after peer review.** `Lighthouse.Backend` is a
   single assembly and it carries `InternalsVisibleTo` for the test project, so an internal
   constructor is visible to every type in the backend and to every test. The permit therefore does
   **not** make wrongful emission a compile error, and an earlier draft of this ADR said it did. That
   was wrong, and the phrase "the gate's assembly-visible seam" was hand-waving over the gap.

   What the permit actually buys is narrower and still worth having: there is no zero-argument
   overload, so emission cannot happen by *omission* - a caller has to go and find a permit, which
   means going through the gate or deliberately constructing one. And it collapses the rule into a
   single greppable type that an architecture rule can name. The enforcement is
   `UsageDataEmitSeamArchUnitTest`, which is a test, and it should be described as one.

   Making the original claim true would mean extracting the gate, the permit and the port into their
   own assembly so `internal` means something. That is a reasonable future move. It is not proposed
   for slice 01, which is already oversized.

3. **Every uncertainty resolves to "do not send".** The gate never throws. A database failure, a
   missing optional-feature row, an absent instance identifier, a cancellation - all return
   `Suppressed`, with `EvaluationFailed` for the unexpected ones so an operator can tell a policy
   decision from a fault. The single log line the failure budget allows is spent here.

4. **If slice 04 needs a cache, it goes inside the gate, and it may only ever suppress.** The
   interface above admits an implementation that memoises. The invariant that makes that safe is
   directional: **a stale gate may produce a false negative (declining to send when it could have
   sent) and must never produce a false positive (sending when it should have suppressed).** Concretely
   that means a cache entry is discarded on any doubt, is bounded by a short maximum age so a missed
   invalidation self-heals, and starts cold in the suppressed state at process start. Slice 01 ships
   no cache at all.

5. **The invalidation must not be the domain-event bus.** `DomainEventDispatcher` deliberately
   swallows handler exceptions:

   > `#pragma warning disable CA1031 // one failing handler must not abort the others or lose the committed fact; recovery is the next re-sync`

   That is correct for metrics invalidation, where a dropped reaction costs a stale chart until the
   next refresh. For a consent gate the same dropped reaction means the instance keeps sending after
   a revoke, silently, until something else happens to evict the entry. The bus's own failure policy
   is therefore evidence *against* using it here, not for it. Project convention prefers the event
   bus; this is a named exception with a cited reason.

6. **The emitter is a plain `BackgroundService`, not an `UpdateServiceBase`.** See the reuse verdict
   below.

7. **The collector host is named in exactly one place**, a constant on the publisher adapter, so an
   architecture rule has something to point at.

8. **One emit per day per *instance*, not per replica.** This codebase runs multi-replica on purpose:
   when a Redis connection string is present, `Program.cs` registers `RedisUpdateStatusStore`,
   `PostgresUpdateExecutionLock` and `RedisUpdateCompletionNotifier`. Every replica shares one
   database and therefore one instance identifier, so a hosted service running in each of three
   replicas emits three heartbeats a day under an identical `distinct_id`. The census survives that -
   distinct identifiers still count one - which is exactly why the defect is easy to miss, while every
   per-day event count is silently multiplied by the replica count and the free-tier volume estimate
   is wrong by the same factor.

   The emit is therefore guarded by a **day key**: `UsageData:LastHeartbeatDay` in `AppSettings`,
   written as a conditional update whose affected-row count is the verdict, in the same
   compare-and-swap shape [ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md)
   argues for and that `DeliveryMetricSnapshot`'s `RecordedDay` already uses. Whichever replica wins
   the day emits; the others see zero rows affected and skip. This is preferred over
   `IUpdateExecutionLock` because it also survives a restart mid-day - a lock released at process exit
   would let the next start emit again - and because it reuses the `AppSettings` extension
   [ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md) already
   introduces rather than pulling in the update-queue machinery.

## An honesty note about the zero-leak outcome

`OUT-usagedata-zero-leak-before-consent` is worded as *"exactly 0 bytes leave an instance that has no
consenting browser"* and designated a hard CI gate. **Nothing in this design measures that, and it is
important to say so rather than let a green build imply it.**

The `DelegatingHandler` assertion observes the named client the publisher uses - the one call site
that already requires a permit. It cannot observe `GitHubService`, which builds its own `GitHubClient`
as a field initialiser; it cannot observe a future `new HttpClient()`; it cannot observe non-HTTP
egress. So on its own it proves that the code path guarded by the permit does not fire when the gate
suppresses, which is close to a tautology.

Two things follow, and both are required rather than optional:

- The ArchUnitNET rule forbidding ad-hoc HTTP client construction outside a whitelist is what stops
  the assertion decaying into a tautology as call sites are added. It is listed in the enforcement
  table as a peer of the handler test, not as a nice-to-have.
- **The outcome should be rescoped to "zero requests to the collector host"**, which is what can
  actually be asserted, with the packet-level check on a clean instance kept as the acceptance-time
  activity it was always specified as. Claiming "0 bytes" against a codebase with a known second
  unconsented outbound call is a claim the gate cannot support.

## Alternatives considered

- **Push the switch into a singleton at startup and update it when the toggle is written.** The
  fastest possible read, and the shape the slice brief implies. **Rejected**: correctness depends on
  every write path publishing the update, and on every publication being delivered. Miss either and a
  privacy control fails open with no symptom. The failure is invisible precisely because the switch
  reads "off" on the screen the administrator is looking at while the emitter still holds "on".

- **`IOptionsMonitor`-style configuration binding.** **Rejected** - the switch is a database row
  owned by an administrator at runtime, not configuration. Modelling it as configuration would put it
  in a second place and invite the two to disagree.

- **Consult the switch, but let the emitter cache consent (or the reverse).** **Rejected** as the
  worst of both: it produces a design where one half of the gate is fresh and the other is not, and
  nobody can state the resulting staleness window without reading both implementations.

- **Ride the `UpdateServiceBase` pattern for the daily emit.** **Rejected on inspection** - see the
  reuse verdict. This is a direct contradiction of the SPIKE's own design note.

- **Route all outbound HTTP through `IHttpClientFactory` so the zero-leak assertion has a chokepoint.**
  Correct, and out of scope. It means changing `GitHubService`, which is a pre-existing unconsented
  call this Epic documents rather than alters. Raised as a board item. The permit type plus the
  single-constant rule give this Epic what it needs without it.

## Consequences

**Positive**

- The revocation-latency outcome is met by construction: there is no cached copy to be stale.
- Emission has one call site behind one named type, so the rule is greppable and machine-checkable
  rather than a convention spread across the codebase.
- A reader can answer "what decides whether we send" by opening one class.

**Negative / accepted**

- Registering a `BackgroundService` and a fourth `AddHttpClient` both edit `Program.cs`, which pulls
  in the full backend Integration suite and its live-connector flake exposure. Known and accepted as
  a slice-01 cost; it is one CI cycle, not a design problem.
- Two database reads per emit is trivial at slice 01 and is a real number at slice 04. The gate
  interface is shaped so that becomes an implementation change behind one method rather than a
  redesign.
- The permit type is a small amount of ceremony for a one-call-site feature today. It pays for itself
  the moment slice 04 adds more call sites, which is exactly when someone would otherwise emit
  without asking.

**Reuse verdict**: `UpdateServiceBase<TEntity>` -> **UNCHANGED**, assessed and **rejected as the
base class**. It is `where TEntity : class, IEntity`; it iterates `repository.GetAll()`, asks
`ShouldUpdateEntity(entity, refreshSettings)` per row, and enqueues per-entity work through
`UpdateQueueService` keyed by an `UpdateType` that feeds the SignalR update-status hub. The heartbeat
is instance-scoped and has no entity to iterate, no per-entity refresh settings, and no business
appearing in the operator's refresh-status surface. Riding it would mean inventing a fake entity and
two `NotSupportedException` overrides. The codebase's own precedent for instance-scoped background
work is a plain `AddHostedService` - `GracefulShutdownService` and `KeyRingFileWatcher` - so this is
the established pattern, not a new one. *This contradicts the SPIKE's design note that the emit
"hangs naturally off the `BackgroundServices/Update/UpdateServiceBase` pattern".*
`IDomainEventDispatcher` -> **UNCHANGED**, assessed and rejected as the invalidation channel on its
own documented swallow-and-continue policy. `UpdateQueueService` -> **UNCHANGED**.
`IUpdateExecutionLock` -> **assessed; superseded by the day-key below**. An earlier draft dismissed it
with "cluster-wide single execution is not needed... the identifier is per instance rather than per
replica". The second clause is true and the first does not follow from it: all replicas share one
database and therefore one identifier, and a plain `AddHostedService` runs in every replica, so a
three-replica deployment emits three heartbeats a day under one `distinct_id`. See the cluster note. `AddHttpClient` registrations -> **EXTEND**,
one named client. `GitHubService` -> **UNCHANGED** (the pre-existing outbound call is documented, not
altered).

**Enforcement**

| Rule | Mechanism |
|---|---|
| Nothing can publish without a permit | Compiler: `PublishAsync` requires `UsageDataEmitPermit`, whose constructor is internal to the gate's assembly-visible seam |
| Only the gate constructs a permit | ArchUnitNET (`UsageDataEmitSeamArchUnitTest`): no type other than the gate may depend on the permit's constructor |
| The collector host is named once | ArchUnitNET: only the publisher adapter may reference the host constant |
| The gate never throws | NUnit: a repository that throws on every call yields `Suppressed(EvaluationFailed)`, not an exception |
| A revoke stops the very next emit | NUnit: revoke, then evaluate, assert `Suppressed(NoLiveConsent)` - no restart, no cycle boundary |
| The master switch stops an already-consenting browser | NUnit: live grant present, feature off, assert `Suppressed(MasterSwitchOff)` |
| Zero consent produces zero requests **to the collector host** | NUnit + `DelegatingHandler` on the named client, failing the test on any request across a full emit cycle with no consent. **This is narrower than the outcome's wording — see the honesty note below** |
| No type may construct its own HTTP client | ArchUnitNET: `new HttpClient(` and `new GitHubClient(` are forbidden outside an explicit whitelist, so the assertion above cannot be bypassed by a future call site |
| A cache, if added, can only suppress | NUnit (slice 04 only): a stale entry saying "permitted" must still be re-validated before a permit is minted |

Note for whoever writes the ArchUnitNET fixtures: fluent slice fields must be declared as the
concrete `GivenTypesConjunctionWithDescription`, not `IObjectProvider<IType>`, or CA1859 fails the
Sonar gate - it has already fired six times at once in one such file.

Cross-refs [ADR-173](./adr-173-consent-as-a-server-side-record-with-a-liveness-window.md) (the consent
set this gate queries),
[ADR-175](./adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md) (the
identifier the permit carries, and the `NoInstanceIdentifier` reason),
[ADR-176](./adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md) (the
publisher the permit is spent on),
[ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) (the event bus
whose failure policy is cited here as the reason not to use it).
