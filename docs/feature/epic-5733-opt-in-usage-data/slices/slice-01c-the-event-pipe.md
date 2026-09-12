# Slice 01c — One event leaves, one click stops it, and the product stops saying it collects nothing

**The emit half of the walking skeleton.** Replaces the superseded `slice-01b-the-first-heartbeat.md`
and occupies the slot ADO #5975 vacated. Slice 01a built a decision nothing acted on; this is what
acts on it — and it acts on events a person generates rather than on a clock.

## Goal

Someone opens a page on the vendor's own instance, the event arrives in the collector the same day
carrying no path and no entity id, the maintainer sees it, revokes from the footer, and the next one
does not happen — with nothing queued anywhere that could outlive the decision.

## IN scope

- **The ingest endpoint**: `POST /api/latest/usagedata/events`, anonymous, carrying the consent token
  in the same header the shipped endpoints use. `204` in every case except a malformed body, so the
  endpoint is not an oracle for which tokens exist.
- **Closed vocabularies on the wire**: `UsageDataEventName` and `UsageDataRouteKey` enums, two bounded
  integers, and **no free `string` property at all**. The server owns the `/teams/:id` text the
  collector sees. A client sending a real path does not deserialise.
- **The gate**: resolves the token, checks the row is `Granted` and live, checks the master switch,
  fail-closed, mints the permit. One indexed read. No cache.
- **The queue and the forwarder**: a bounded in-process channel drained by a hosted service, which
  **re-checks consent before publishing**. Nothing persisted — no outbox, no browser-side queue.
- **Two ceilings**: a `UsageDataIngestPolicy` fixed-window limiter partitioned on the token digest
  with an IP fallback, declared *and configured* in the same commit; and a per-instance daily event
  budget that drops silently above the limit.
- **The instance properties**, attached server-side: version (release-shaped, else the literal
  `unreleased`), deployment mode, licence tier, auth-enabled.
- **The publisher adapter** and its named client, with the collector host as a single constant and
  `$ip: null` / `$geoip_disable: true` on every event.
- **The analytics pseudonym**: one nullable `AnalyticsId` column on `UsageDataConsent`, one additive
  migration per provider, minted only on a grant, never on the wire in either direction.
- **Two deletions**: the instance-identifier minting path (`EnsureUsageDataInstanceId`,
  `GetUsageDataInstanceId`, `AppSettingKeys.UsageDataInstanceId` and the call site with its
  `try`/`catch`), and `IUsageDataConsentRepository.AnyLiveGrantAsync`, which has no production caller
  today and can never acquire one.
- **The browser detector**: an in-memory buffer, a route-key map, a flush on a timer and on
  `visibilitychange` → hidden, and a discard of the pending buffer before a revoke is sent. No storage
  key beyond the consent token already written.
- **Exactly one real product event** — *a Team or Portfolio detail tab was opened*. Not a bare page
  view, and not the cheapest thing to emit. See "The event, and why this one" below for the reasoning
  and the two alternatives. It is demo-able end to end: a real event leaves a real instance and
  appears in PostHog, so the privacy canary and the server-side route-pattern handling are exercised
  against real traffic rather than a test double.
- **The zero-leak harness**: the `DelegatingHandler` assertion **and** the architecture rule
  forbidding ad-hoc HTTP client construction, which is what stops the assertion decaying into a
  tautology.
- **`docs/settings/usagedata.md` rewritten — this is a SHIP BLOCKER, not a follow-up.** The page still
  describes the daily heartbeat, and the shipped dialog links to it as the authoritative account while
  deliberately carrying no field list of its own. **That makes this page the only place the payload
  list exists.** A real event leaves an instance in this slice, so the page must describe that event,
  its route-pattern values and the instance properties **before the slice ships** — not after it, not
  in the next slice. Shipping the event first would mean the product emits data it has not disclosed,
  which is the Epic's one non-negotiable ("an event that ships ahead of its documentation is a
  defect", AC-08.2) failing on the very first event. **The rewrite is inside this slice's definition
  of done.**
- **Copy corrections**: `SurveyNudge.tsx:114` and the CRA self-assessment row 1.7. Both assert
  Lighthouse collects nothing. This is the slice that makes that false, so this is the slice that
  fixes them.

## The event, and why this one

**Decided 2026-09-12: `TeamOrPortfolioTabOpened`** — someone opened a tab on a Team or Portfolio
detail page.
One event name; the route key carries *which* entity type and *which* tab, and never which entity.

**Why this one, on structural grounds.** The route table is at `App.tsx:203-226`, and two of its
routes are `/teams/:id/:tab?` and `/portfolios/:id/:tab?`. Those are the **only** routes in the
product carrying both a customer entity id and a user-meaningful second segment, which makes them the
hardest case in the whole table and the one worth proving first. A single event on that route must
simultaneously:

- **drop `:id`** — a real Team or Portfolio identifier, the exact leak this design exists to prevent;
- **keep `tab`** — a genuine user choice, and one of the few things the maintainer actually wants;
- **do both without the browser ever sending a path**, because the route key is an enum whose members
  are `(entity type, tab)` pairs. `/teams/42/metrics` is not normalised server-side; it never leaves
  the browser at all. What leaves is the member `TeamDetail_Metrics`.

If the design gets that one right, every other route is strictly easier. A bare `/features` or `/`
page view would exercise none of it — it has nothing to strip — which is precisely why it is not the
choice.

**Why it is also a real product question, not a page view with a better name.** Several of the things
that ship into those tabs have landed recently and nobody knows whether anyone opens them. The tab is
the unit a person chooses; "which views of a Team do people actually use" is answerable from this
event on day one, and it is the first honest answer this Epic produces.

**The honest caveat.** It is a *navigation* event. It tells you a view was opened, not that anything
in it was used. That is a genuine limitation and slice 04 is where it is fixed.

**The two alternatives that were weighed and declined**, kept here so the choice is not re-opened
from scratch later. Which recently-shipped capability is most worth an answer was a product call
rather than an architectural one; the maintainer took it on 2026-09-12 and chose the tab-open above.

| Alternative | What it exercises that the proposal does not | What it loses |
|---|---|---|
| **A widget's "View Data" dialog opened** | Unambiguously a feature-use event rather than a navigation one | Exercises route handling no harder — it happens *on* the same route — and it needs the widget vocabulary, which is slice 04's decision to make |
| **A forecast was run** | A deliberate, high-intent action; the clearest possible "someone used this" | Same route-handling coverage as the proposal, and it is a narrower question than "which views get opened" for a first event |

The route-key mechanism is the same in all three cases; only the event name and the trigger move, so
neither alternative was declined on cost. Either remains a one-line change if the question this Epic
needs answered first turns out to be a different one.

## Consent: who this actually emits from, verified

**Confirmed against the shipped 01a flow rather than assumed.**

- The backend is the enforcement point: the gate resolves the presented token and requires
  `Decision == Granted` on a live row. A declined or revoked browser is answered `204` and nothing is
  forwarded.
- **Holding a token does not mean having granted.** `useUsageDataConsent.decide` writes the returned
  token for *either* answer — a browser that clicked "No, thank you" also holds one, by design and for
  the ePrivacy reason D2 gives. So the detector must **not** gate on "a token exists". It gates on the
  server-derived `sending` state, which `UsageDataConsentService.GetStateAsync` computes as
  `consent?.Decision == UsageDataDecision.Granted`.
- `TOKEN_STORAGE_KEY` and `readToken` are module-private inside `useUsageDataConsent.ts`, and
  `UsageDataService` takes the token as a parameter (`getState(token)`, `revoke(token)`) rather than
  reading storage itself. **Keep that shape**: one module owns the storage key, the service stays
  storage-ignorant, and `postEvents` takes the token the same way its siblings do.

**The consequence of shipping this before slice 02, stated plainly.** In slice 01a the dialog opens
only when someone clicks the footer indicator — the unprompted ask is slice 02. So at 01c the
consenting population is the dogfood instance plus whoever went looking for the icon. **This slice's
production acceptance is satisfiable** (AC-04.6 has always been a dogfood criterion) **but the field
signal is close to zero until #5835 ships.** That is the cost of the agreed order, it is not a defect
in this slice, and nobody should read an empty dashboard in the weeks after 01c as the pipe being
broken.

## OUT of scope

- **Every further named product event** — slice 04 (#5837). This slice ships one; that slice ships the
  rest.
- The unprompted dialog and any cadence — slice 02 (#5835), which runs next.
- The admin master switch and premium gating — slice 03 (#5836). The gate reads the optional feature
  from the start, so 03 is a seed and a toggle rather than new enforcement.
- Making the collector endpoint customer-configurable beyond the base URL it already has.
- **Scheduling `PruneStaleAsync`** — now **owned by slice 02**, which is the slice that makes consent-row
  growth real. Before the unprompted ask, rows accrue only from people who went looking for the footer
  icon.

## Learning hypothesis

**Disproves "a browser-detected, backend-verified pipe can enforce consent with no staleness" if** a
revoked browser's next batch is forwarded, or if the gate cannot resolve a token cheaply enough to run
on every batch.

**Disproves "nothing leaks before consent" if** the zero-consent check finds any traffic to the
collector host.

**Disproves "the wire cannot carry an entity id" if** any body shape gets a real path past the
deserialiser, or if a Team or Portfolio identifier appears anywhere in the stored record at the
collector. This is the hypothesis the chosen event exists to test: it fires on the one route family
that carries both an entity id and a value worth keeping.

**Disproves "same-origin ingest is not blocked" if** the dogfood instance's own events do not arrive
from a browser running the blockers this audience runs. This is the one hypothesis SPIKE-00 could not
test, because it only ever measured vendor domains — see the reconciliation added to
`spike/findings.md` §AC-00.5.

**Confirms** the whole architecture if the maintainer sees the event arrive, revokes from the footer,
and sees the next one not arrive.

## Acceptance criteria

Per AC-03.2 and the rewritten US-04 criteria in `feature-delta.md` — note that four of US-04's seven
no longer describe anything and are listed for back-propagation in `design/upstream-changes.md`. The
four that make or break the slice:

- **AC-03.2** — the next emit after a revoke does not happen, asserted at the emit path. Now asserted
  at three points: the browser's buffer is discarded, the gate suppresses at accept, the gate
  suppresses again at drain.
- **The route field cannot carry a path.** A body carrying `"/teams/42"` returns `400` and forwards
  nothing.
- **An undocumented event cannot be emitted.** The declared enum and
  `docs/settings/usagedata.md` are compared in CI — and for this slice the page must already describe
  the event before it ships. See the ship blocker above.
- **The tab survives and the identifier does not.** A visit to a Team detail tab produces a record at
  the collector naming the tab and containing no Team identifier in any field.
- **Zero consent produces zero requests to the collector host**, across a full flush cycle.

## Production-data acceptance

The maintainer opens the collector dashboard and sees the vendor's own production instance reporting
real tab-open events, on the day this ships, with the tab named and no entity identifier present. No
synthetic events count, and no test double stands in for the collector — the point of shipping a real
event in this slice is that the privacy canary and the route-key handling are exercised by real
traffic.

## Dogfood moment

Same day. The vendor's instance is already consenting from 01a. Open a Team tab, watch the event land
naming the tab and not the Team, revoke from the footer, open another tab, watch nothing land. That
round trip is the demo.

## Dependencies

- **Slice 01a complete.** Shipped 2026-09-12.
- The collector project configured as slice 01b already specified: client IP discarded, no GeoIP
  transformation, autocapture and session replay off, the vendor's AI data-processing consent off at
  organization level, one project, retention confirmed at one year.
- **Recompute the PostHog free-tier headroom.** ADR-176's ~30k events/month was a heartbeat estimate
  and is now wrong by orders of magnitude. A paid plan would widen retention to seven years and
  falsify the published position, so this is a privacy check as much as a cost one.
- **Confirm the ingest route name against EasyPrivacy's and AdGuard's generic path patterns.** Those
  lists match on any domain, not only vendor ones, and a route named for tracking would reintroduce
  the bias this whole design exists to avoid, at larger scale.

## Effort

One to one and a half days — larger than slice 01b because it carries a frontend detector and a
migration that 01b did not, and smaller in one respect because the day key, its seeding and its
Testcontainers test are gone.

## Reference class

Slice 01a itself, for the anonymous-endpoint-plus-migration shape, and for the rate-limit policy which
is a direct copy of `UsageDataConsent`'s (`appsettings.json:68`).

## Pre-slice risk

Two, and they are different in kind.

**The zero-leak proof still needs a harness this repository does not have**, exactly as slice 01b
noted. Building it is part of this slice, and if it cannot be built the invariant is unverified and
the slice is not done. It could not have been written in 01a, where no collector client exists to
observe.

**The ingest endpoint is new anonymous write surface on a product that is frequently
internet-exposed.** It is bounded — a caller with no live grant is answered `204` and forwards
nothing, the limiter partitions per browser, the budget bounds the vendor quota, and the DTO has no
field that could carry customer content — but it did not exist in the abandoned design and it should
be reviewed as new surface rather than as a variation on the consent endpoints.
