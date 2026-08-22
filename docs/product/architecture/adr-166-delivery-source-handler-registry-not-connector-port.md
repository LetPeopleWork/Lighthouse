# ADR-166: Delivery sources are a list-shaped HTTP contract over a singular provider on the Jira connector, not members on `IWorkTrackingConnector`

> *Filename note: this file keeps its original `…-registry-not-connector-port` slug so existing
> cross-references resolve. The registry it was named for was withdrawn on 2026-08-22 — see the
> revision note under Decision.*

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slices 01a-01b)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D1 requires 0..n further selection tabs on the Delivery modal, one per source handler the Portfolio's
connection offers, rendered from what the server reports rather than from a client-side test of the
system type (AC-01.2). The first and only handler is Jira Release; four of the five work tracking
systems have no Release concept at all (S13).

D2 said the capability is declared **per connection**, following the established shape on the port —
`SupportsTransitionHistory(connection)` and `SupportsIncrementalSync(connection)` (S7) — and named
`bool SupportsDeliverySources(connection)` as the member. [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md)
established that idiom and it is the right answer to the question it was asked.

The question here differs in one structural way. Incremental sync is **one capability with one shape**:
every connector either can sweep or cannot, and the four sweep members are the same four members for
all of them. A Delivery source is **0..n named concepts**, and D1's explicit point is that a second one
is addable later — a second Jira concept, or an Azure DevOps Iteration.

`IWorkTrackingConnector` carries 15 members across 5 implementations. Expressing "0..n named handlers"
on it requires one of two shapes:

- `GetDeliverySourceKeys(connection)` + `GetOptions(connection, sourceKey)` + `Resolve(connection,
  sourceKey, reference)`, in which case each connector dispatches on a string key internally and the
  second Jira handler is a new `case` inside a 1600-line class; or
- one member per handler, which does not survive the second handler.

ADR-139 rejected an opt-in `IIncrementalWorkTrackingConnector` because **a type test cannot express
per-connection variance** — Jira Cloud and Jira Data Center are one class and must answer differently.
That argument is sound and applies here unchanged. It does **not** rule out a registry: a registry whose
members each answer `AppliesTo(connection)` expresses per-connection variance directly, because the
connection is the argument.

## Decision

**The HTTP contract is list-shaped. The implementation is singular, and follows the house pattern for a
system-specific capability. `IWorkTrackingConnector` is not touched by this Epic.**

> **Revised 2026-08-22 (maintainer ruling, Fork 3).** An earlier draft of this ADR introduced an
> `IDeliverySourceHandler` port plus an `IDeliverySourceRegistry` resolving handlers from a DI-injected
> `IEnumerable`. That draft rejected only a strawman — "a marker interface tested with `is` at the call
> site" — and never evaluated **the pattern this codebase already uses for exactly this problem**:
> `IJiraWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider`
> (`Services/Interfaces/WorkTrackingConnectors/IJiraWorkTrackingConnector.cs:6`), registered at
> `Program.cs:1244` and injected **directly** at `WizardsController.cs:17` with no type test and no
> downcast. The registry was speculative generality: D1 says a second handler is "addable later", and
> no acceptance criterion requires one. It is withdrawn.

Four points:

1. **The capability is a narrow interface on the Jira-specific abstraction**, following
   `IBoardInformationProvider`:

   ```csharp
   public interface IDeliverySourceProvider
   {
       IReadOnlyList<DeliverySourceDescriptor> AvailableSources { get; }
       Task<IReadOnlyList<DeliverySourceOption>> GetOptions(Portfolio portfolio, string sourceKey);
       Task<IReadOnlyDictionary<string, DeliverySourceResolution>> ResolveMany(
           Portfolio portfolio, string sourceKey, IReadOnlyList<string> sourceReferences);
   }

   public interface IJiraWorkTrackingConnector
       : IWorkTrackingConnector, IBoardInformationProvider, IDeliverySourceProvider { }
   ```

   `JiraWorkTrackingConnector` implements it. The other four connectors are untouched — no member, no
   `false`, no `NotSupportedException`. `ResolveMany` rather than `Resolve` is
   [ADR-171](./adr-171-release-membership-by-jql-reference-ids.md)'s batching decision; create-time
   resolution passes a single-element list rather than needing a second method.

2. **The application layer asks the connector factory for the provider, and an absent one means no
   sources.** The existing `IWorkTrackingConnectorFactory` already resolves a connector per connection;
   a connection whose connector does not implement `IDeliverySourceProvider` yields an empty list. This
   is per connection (D2), because the connector instance is per connection — Jira Cloud and Jira Data
   Center resolve through the same class and can answer differently from `AvailableSources` when slice
   00 Q2's Data Center answer arrives.

3. **No capability flag is added to `IWorkTrackingConnector`.** `SupportsDeliverySources(connection)`
   would be a new member on a 15-member port shared by five adapters, whose answer is already carried by
   whether the resolved connector offers the provider. The `delivery-sources` endpoint returning `[]`
   **is** the capability answer, and AC-01.1's "absent from the DOM" falls out of an empty list rather
   than out of a boolean the client has to interpret.

   This keeps D2's substance — capability answered per connection — and changes the mechanism D2
   assumed. `SupportsDeliveryForecastPublishing` belongs to slice 04 and is deliberately not decided
   here; slice 04 is blocked on the slice-00 SPIKE.

3b. **The HTTP contract is list-shaped on purpose, while the implementation is singular.** This is the
   load-bearing distinction and it is not an inconsistency. `GET .../delivery-sources` returns
   `[{ key, displayName }]` — a list — because that is what makes the tab **absent** on the four
   connectors with no Release concept, which is the requirement D1 actually encodes (AC-01.1, AC-01.2).
   Adding a second source later is then an **internal change with no client change**: a new descriptor
   in `AvailableSources`, a new arm inside the provider. The registry the earlier draft proposed bought
   nothing the list shape does not already buy, and cost an abstraction to carry in the meantime.

4. **The three new routes are Portfolio-nested**, on a new `DeliverySourcesController` carrying both
   `api/v1/...` and `api/latest/...`:

   | Method | Route | Guard |
   |---|---|---|
   | GET | `portfolios/{portfolioId:int}/delivery-sources` | `PortfolioRead` |
   | GET | `portfolios/{portfolioId:int}/delivery-sources/{sourceKey}/options` | `PortfolioWrite` |
   | POST | `portfolios/{portfolioId:int}/delivery-sources/{sourceKey}/preview` | `PortfolioWrite` + Premium |

   The two existing controllers disagree: `DeliveriesController` is `api/v1/deliveries/portfolio/{id}`
   (`:14,30,75`), `DeliveryRulesController` is `api/v1/portfolios/{portfolioId:int}/delivery-rules`
   (`:14-15`).

   **An earlier draft claimed nesting was required for attribute-based RBAC. That is false and is
   withdrawn.** `RbacGuardAttribute` reads `context.RouteData.Values`, so **any** route carrying a
   `{portfolioId}` route value is attribute-guardable — `DeliveriesController.cs:30-31` and `:75-76`
   already are, unnested. The `{deliveryId}`-rooted endpoints use the in-action idiom because they
   carry no `portfolioId` at all, not because they are unnested.

   What survives is a style argument, and it is sufficient but no stronger than that: the preview
   endpoint mirrors `delivery-rules/validate` (`DeliveryRulesController.cs:40`) in shape, response type
   and premium gate, and that sibling lives at the nested address. Both shapes are guardable and both
   are serviceable; this picks the one whose closest relative already exists. A maintainer who prefers
   the `deliveries/...` family for cohesion has a legitimate case and loses nothing mechanical by
   taking it.

   The existing `DeliveriesController` routes are **not** changed. They are a shipped contract consumed
   by Lighthouse-Clients. The inconsistency is recorded here, not repaired by this Epic.

   A new controller rather than more endpoints on `DeliveriesController`, which already carries eight
   constructor dependencies behind an `#pragma warning disable S107` (`:17`).

5. **A source option carries its own selectability and the reason it is not selectable.**

   ```csharp
   public sealed record DeliverySourceOption(
       string Id, string Name, DateTime? Date, bool IsRetiredAtSource, bool IsReleasedAtSource,
       bool IsSelectable, SourceOptionBlockReason? BlockedBecause);

   public enum SourceOptionBlockReason { NoDateSet, RetiredAtSource }
   ```

   **The bindability predicate is one function with one construction site**
   (`DeliverySourceBindability.For(hasDate, isRetired)`), consumed by **two** callers: `GetOptions`,
   which builds the option, and the create path, which refuses a bind that the picker would not have
   offered. Two copies of this rule would let a direct `POST` bind something the UI calls unselectable.

   Not selectable when the source **has no date** *or* is **retired at the source**. Two reasons rather
   than a bool, because they send the reader to different places — "no release date set in Jira" is an
   instruction to go and set one; "archived in Jira" is not, and telling someone to set a date on an
   archived Version would waste their time.

   `IsReleasedAtSource` is carried **for display only and never gates selection**. A Version is routinely
   marked released while the last work finishes, which is exactly when the forecast is worth having.

   *Vocabulary note: the model says `RetiredAtSource`, not `Archived`, because `archived` is Jira's word
   and nothing Jira-shaped crosses the port (ADR-171). The frontend renders "archived in Jira" for the
   Jira provider.*

   D11 requires a dateless Release to be **listed, labelled and not selectable**, with the label saying
   where to fix it (AC-01.3) — and dateless is the *common* case, not an edge one: two of the three
   Releases on the demo instance carry no date. An earlier draft's option shape was
   `(Id, Name, Date?)`, which forced the client to re-derive selectability from a null date. That is
   the wrong place for the rule: the server owns which options may be bound (ADR-170 point 6 refuses a
   `NoDate` bind with a `400`), so the client must render a verdict rather than reach one of its own —
   the same reasoning `DeliveryNoteDto.CanModify` already applies.

   **Ruled 2026-08-22 (maintainer, D13 / AC-01.8).** Jira's `archived` and `released` flags gate
   **bindability only**. An archived Version is listed, labelled and not selectable — it is the dateless
   case in a different costume, since a Version is archived precisely to retire it from planning and its
   date will never move again. A released Version stays selectable. **Neither flag has any effect on a
   Delivery that is already bound**: the binding survives, re-sync continues, and the date simply stops
   moving, which is honest and visible. No special state, no transition. See *The lifecycles are
   independent* under Consequences for why.

## Alternatives considered

- **`bool SupportsDeliverySources(connection)` plus source members on `IWorkTrackingConnector`**, as D2
  assumed and ADR-139 modelled. **Rejected** — it forces string-key dispatch inside the connector for
  the second handler, and it makes four connectors carry members they will never implement. ADR-139
  accepted that cost ("six implementations must exist even though one of them only ever says no") for
  one bool serving one uniform capability. It is a different trade for a member set that grows with
  concepts.

- **A handler port plus a registry** — `IDeliverySourceHandler` with `Key`/`AppliesTo`, and an
  `IDeliverySourceRegistry` resolving handlers from a DI-injected `IEnumerable`. **This was the earlier
  draft of this ADR, and it is rejected.** It bought exactly one thing the chosen design does not
  already have: dispatch across multiple handlers. There is one handler, no acceptance criterion asks
  for a second, and D1 says only that a second is "addable later" — which the list-shaped contract
  (point 3b) already makes cheap. The registry was an abstraction carried in advance of the problem it
  solves, and it created a real cost while doing so: the handler sat outside the connector, so it could
  not reach the connector's `private` `HttpClient` cache (`:63`, `:1853`), and the ADR had to hand-wave
  that gap as an open question. **The house pattern dissolves it** — the provider is implemented *by*
  the connector, which already owns the authenticated path.

  An earlier draft also rejected "an opt-in marker interface tested with `is` at the call site" on
  ADR-139's grounds. **That was a strawman**: it is not what this codebase does.
  `IJiraWorkTrackingConnector : IWorkTrackingConnector, IBoardInformationProvider` is injected
  *directly* at `WizardsController.cs:17` — no `is`, no downcast — and ADR-139's "a type test cannot
  express per-connection variance" simply does not apply, because the connector instance is already
  resolved per connection. The earlier draft rejected a shape nobody proposed while never evaluating
  the shape already in use.

- **Reuse the rule engine** — configure `fixVersions` as an additional field and write
  `equals "2026 Q4"` (S10). **Rejected.** Feature-delta D3 gives four reasons and all four hold against
  the code: `RuleEvaluator` compares one stored string case-insensitively against what is an array
  (S11); the rule carries a name where a Jira-side rename must not break the binding; a rule yields no
  date at all, which is the entire point of the Epic; and there is no enumeration, so the user
  hand-types a version string with no feedback until validate. A fifth reason emerged at DESIGN:
  `IDeliveryRuleService.RecomputeRuleBasedDeliveries`'s signature is pinned by
  [ADR-012](./adr-012-rule-engine-generalisation.md)'s reflection test, and
  [ADR-163](./adr-163-archived-deliveries-excluded-by-narrowed-port.md) already spends that pin once.
  Routing source binding through the rule service would spend it a second time, in a second in-flight
  epic, which is how a guard gets disabled rather than updated.

- **One handler per connector with the concept as a parameter.** **Rejected** — this is the first
  bullet of point 1 wearing a different name.

- **Auto-archiving a Delivery when its Release is marked `released` or `archived`.** **Rejected** — see
  *The lifecycles are independent* under Consequences.

**Raised and deferred — prompting the user to archive (2026-08-22).** The better end state is not
auto-archiving but *asking*: when a bound Release is marked released, surface "this Release is marked
released — archive this Delivery?" and let a human decide. That keeps the closure snapshot under human
control while removing the need to notice the flag by hand. It is **out of scope, recorded not
scheduled**, for two reasons. It needs **both features live** — archiving (#5698, still unbuilt) and
source binding (this Epic) — so it cannot be built until the second of them ships. And it needs a
**prompting surface that does not exist**: nothing in the Delivery UI today asks a user to confirm a
state change discovered by a background refresh, so this would be a new interaction pattern rather than
a checkbox on an existing screen. It belongs to whichever feature builds that surface first.

## Consequences

**Positive**

- Adding a second source is one arm inside the existing provider and no new type — no port change, no
  connector change, no frontend change. AC-01.2 holds by construction rather than by test.
- Four connectors are untouched. The shared-contract blast radius ADR-139 had to accept (grep every
  usage, extend every test double before the first implementation lands) does not arise.
- `AvailableSources` is answered per connection through `IWorkTrackingConnectorFactory`, so the Jira
  Cloud / Data Center split is a one-line change inside one class when slice 00 Q2's Data Center answer
  arrives — the same rollout-expressed-as-data property ADR-139 valued. (The earlier draft credited this
  to `IDeliverySourceHandler.AppliesTo`; that type is withdrawn, and the property survives without it.)

**Negative / accepted**

- A second seam to learn. A reader asking "what can this connector do" now finds capability in two
  places: `Supports*` on the shared port, and the Jira-specific capability interface. Contained by that
  interface being the only place Delivery sources are ever asked about.
- Registering the provider in `Program.cs` pulls in the full backend Integration suite — a longer run and
  wider flake exposure, as the feature-delta already records.

### The lifecycles are independent

Anyone reading this later will ask why a Jira Version marked `archived` or `released` does not archive
the Lighthouse Delivery bound to it. It does not, by decision (D13), and the reason is not obvious from
the flags:

- **The closure snapshot is not re-derivable.** Archiving pins one, and the pinned numbers *are* the
  record ([ADR-160](./adr-160-delivery-closure-pin-as-one-row-per-delivery-table.md)). If `released`
  flipped at 09:00 and the team finished at 17:00, propagation would freeze a half-done Delivery
  permanently. Letting a remote flag choose the freezing moment is letting it choose what the permanent
  record says.
- **This is not D4 applied consistently.** D4 gives the remote system the Delivery's date and
  membership, which are facts about the plan. Archive status is a decision about Lighthouse's own
  record. Propagating it would let a Jira administrator tidying up old Versions silently freeze a batch
  of Lighthouse records, with no override short of unbinding.

So the flags gate **bindability only**. `RetiredAtSource` makes an option unselectable; `released` is
carried for display and gates nothing. A Delivery already bound to a Version that later gains either
flag keeps its binding and keeps syncing — its date simply stops moving, which is honest and visible.

The consequence for the resolution type is deliberate and load-bearing: `DeliverySourceResolution` stays
**lifecycle-blind at four arms**, and bindability is applied on top of a successful resolution, at create
only. A fifth `Retired` arm was rejected because re-sync would then carry a case it must *always*
ignore — and a case that must always be ignored is one somebody eventually handles, probably by freezing
the Delivery, which is precisely what D13 forbids.

**Reuse verdict**: `IWorkTrackingConnector` → **UNCHANGED** (this Epic adds nothing to it).
`IJiraWorkTrackingConnector` → **EXTEND** — one further capability interface, exactly as
`IBoardInformationProvider` was added. `JiraWorkTrackingConnector` → **EXTEND** — it implements the new
interface and reuses its own authenticated HTTP path. `IWorkTrackingConnectorFactory` → **REUSE AS IS**
— it already resolves a connector per connection, which is what makes point 2 per-connection.
`DeliveryRulesController` → **UNCHANGED**, used as the route and response-shape precedent only.
`IDeliverySourceProvider`, `DeliverySourcesController`, `DeliverySourceDescriptor`,
`DeliverySourceOption` → **CREATE NEW**: a capability interface with no existing equivalent, a
controller (justified by `DeliveriesController`'s eight dependencies and `S107`), and two wire records.
**`IDeliverySourceHandler` and `IDeliverySourceRegistry` are withdrawn** — the earlier draft's two
largest CREATE NEWs, deleted rather than defended.

**Enforcement**

| Rule | Mechanism |
|---|---|
| Nothing outside the Jira namespace depends on the Jira connector's concrete type | ArchUnitNET: the controller and application layer depend on `IDeliverySourceProvider`, never on `JiraWorkTrackingConnector` — and no `is`/downcast reaches it |
| Every work tracking system has a declared source set | NUnit parameterised over all five `WorkTrackingSystems` members: `["jira.release"]` for Jira, `[]` for the other four. A sixth connector fails the test until classified |
| A connector with no sources answers, rather than failing | Integration: `GET .../delivery-sources` on an Azure DevOps Portfolio returns `200` with `[]`, never `404` |
| An unknown or unoffered source key is refused | Integration: `options` and `preview` return `404` for a key the connection does not offer |
| The contract stays list-shaped even with one provider | Integration: the Jira response is a JSON **array**, not an object or a bare string — so a second source is additive for every client |
| A dateless Release is listed, labelled, and cannot be bound | Integration: `options` returns it with `IsSelectable = false` and a reason; a `POST` naming it returns `400` (ADR-170 point 6). Vitest: it renders in the picker, disabled, with the label naming Jira as where to fix it (AC-01.3) |
| A Release matching zero Features previews a reason, not a blank grid | Integration: `preview` returns `200` with an empty Feature list and an explicit empty-reason discriminator — **not** the `400` that `delivery-rules/validate` returns for an empty match (`DeliveryRulesController.cs:64-67`), because "no Feature carries this Release yet" is a Jira-side gap to name, not a client error. Vitest asserts the reason renders (AC-01.5) |

Cross-refs [ADR-139](./adr-139-incremental-sync-capability-probe-on-connector-port.md) (the idiom this
deliberately does not extend, and why), [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md)
D3 (typed-`IEnumerable` handler resolution), [ADR-012](./adr-012-rule-engine-generalisation.md) and
[ADR-013](./adr-013-rule-match-semantics.md) (the rule engine this does not reuse),
[ADR-170](./adr-170-broken-source-as-recorded-verdict.md) (the result type `Resolve` returns).
