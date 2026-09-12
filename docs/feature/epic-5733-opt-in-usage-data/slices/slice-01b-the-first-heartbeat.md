# ~~Slice 01b — One heartbeat arrives, one click stops it, and the product stops saying it collects nothing~~

> # ⚠ SUPERSEDED — 2026-09-12
>
> **This slice will not be built. ADO #5975 is Removed.**
>
> **Why.** The daily backend heartbeat was the only way to answer "how many instances exist and which
> versions do they run" while the emit path had no browser behind it. The maintainer has since decided
> that the payload carries **user and feature counts only, with no instance identifier** — which
> removes the thing a heartbeat would be a heartbeat *of* — and that the instance facts it carried
> (version, deployment mode, platform, standalone, licence tier) become **properties attached to
> product events** instead. With those two decisions there is nothing left for a scheduled emit to
> send that an event does not already carry.
>
> **What replaces it**: `slice-01c-the-event-pipe.md`. The browser detects events, posts them to
> Lighthouse's own backend, the backend verifies a live consent grant on every batch, attaches the
> instance properties and forwards.
>
> **What carried over from this slice, unchanged in substance**:
> - the fail-closed, uncached gate and the emit permit (now per batch rather than per day);
> - the zero-leak `DelegatingHandler` harness **and** the architecture rule forbidding ad-hoc HTTP
>   client construction, still built inside the slice rather than after it;
> - revocation enforcement at the emit path (AC-03.2), now checked twice — at accept and at drain;
> - the usage data page rewrite and the two copy corrections (`SurveyNudge.tsx` and CRA
>   self-assessment row 1.7), which travel with 01c for exactly the reason they travelled with this
>   slice: it is the slice that first makes them false.
>
> **What died with it**: the once-per-day emit, the five-field payload, the
> `UsageData:LastHeartbeatDay` compare-and-swap and its seeding requirement, the
> Testcontainers-backed day-key test, and the whole replica-multiplication problem — which
> *dissolves* rather than being solved, because a browser-originated event enters through exactly one
> replica and is forwarded once.
>
> **The decisions**: `docs/product/architecture/adr-190-*.md` (supersedes ADR-174) and
> `docs/product/architecture/adr-191-*.md` (supersedes ADR-175).
>
> Everything below is the slice as it stood, left as the record.

---

**Second half of the walking skeleton.** Everything that sends, and the gate that decides whether it
may. Slice 01a built a decision nothing acted on; this is what acts on it.

## Goal

The maintainer sees the vendor's own instance arrive in the collector on the day this ships, revokes
from the footer, and sees the next cycle produce nothing — and every place the product previously
promised it collects nothing now says what it actually does.

## IN scope

- The emit gate: reads the database every time, fails closed, hands back a permit nothing else can
  construct.
- The daily heartbeat (US-04): instance identifier, version, deployment mode, licence tier,
  timestamp. Emitted only while at least one browser holds live consent.
- The day key `UsageData:LastHeartbeatDay`, claimed with a conditional update whose affected-row
  count is the verdict, so three replicas emit once between them rather than three times. Seeded, so
  the claim is not permanently unwinnable. Mechanism and its two silent failure modes in ADR-174
  point 8.
- The publisher adapter, the named HTTP client, and the collector host as a single constant.
- Revocation *enforcement* (US-03, AC-03.2): the next emit after a revoke does not happen, asserted
  at the emit path.
- The zero-leak assertion — the `DelegatingHandler` harness **and** the architecture rule forbidding
  ad-hoc HTTP client construction, which is what stops the assertion decaying into a tautology.
- `docs/settings/usagedata.md` **extended**: the GitHub release check named as a separate pre-existing
  outbound call that this consent does not cover, and the CI check that holds the dialog, the page and
  the emitted payload to the same field list. The page itself ships in 01a, because the dialog links
  it.
- **Copy corrections**: `SurveyNudge.tsx` and the CRA self-assessment row 1.7. Both currently assert
  Lighthouse collects nothing. This is the slice that makes that false, so this is the slice that
  fixes them.

## OUT of scope

- The unprompted dialog and any cadence — slice 02.
- The admin master switch and premium gating — slice 03.
- Any product event beyond the heartbeat — slice 04.
- Making the collector endpoint customer-configurable. Hard-wired to the SPIKE-00 choice.

## Learning hypothesis

**Disproves "a browser-scoped consent record can gate a server-side emitter with no staleness" if** a
revoked browser's revocation does not stop the very next emit, or if the emit path cannot consult
consent cheaply enough to do so on every cycle.

**Disproves "nothing leaks before consent" if** the zero-consent check finds any traffic to the
collector host.

**Disproves "one instance means one heartbeat" if** two concurrent claims of the same day both
succeed.

**Confirms** the whole architecture if the vendor's own instance appears in the dashboard and
disappears from it within one cycle of a revoke.

## Acceptance criteria

Per US-04 (AC-04.1…4.7) and AC-03.2 in `feature-delta.md`. The three that make or break the slice:

- **AC-03.2** — the next emit after a revoke does not happen, asserted at the emit path.
- **AC-04.1** — one emit per day per instance, not per replica.
- **AC-04.2** — the emitted payload contains exactly five fields, asserted against the payload.

## Production-data acceptance

**AC-04.6** — the maintainer opens the collector dashboard and sees the vendor's own production
instance reporting, on the day this ships. No synthetic instance counts.

## Dogfood moment

Same day. The vendor's instance is already consenting from 01a; the heartbeat lands, the maintainer
revokes from the footer, and the next cycle produces nothing. That round trip is the demo.

## Dependencies

- **Slice 01a complete.** The gate has nothing to read without the consent record, and the payload
  has no identifier without the minting path.
- The collector project configured: client IP discarded, no GeoIP transformation present (a
  **separate** control from discarding the IP), the four autocapture toggles and session replay off,
  and the vendor's AI data-processing consent off at the **organization** level. Retention is not
  configurable - confirm it reads one year, which is what the free plan gives, per ADR-175 point 7.
- **One project, not two.** The free plan allows one, and buying a second would move retention to
  seven years. The canary becomes `production-sweep` plus a new read-only `schema-sweep` over the
  project's property definitions, with the can-it-fail check demoted to a unit test over a fixture.
  `settings-parity` is dropped. The delta's "canary on one project" section carries the reasoning and
  the residual.
- Docker available on any machine running the backend suite. The day-key claim cannot be tested on
  EF InMemory, which does not implement the conditional update it relies on, so those tests join the
  container-backed set the consent store already needs — and that set carries no `Integration`
  category, so nothing filters it out of an ordinary run.

## Effort

One day. Gate, heartbeat service with its day key, publisher, docs and copy.

## Reference class

`epic-5775` secret encryption — the comparable "new gate plus new outbound path" slice, and the
source of the conditional-update shape the day key reuses.

## Pre-slice risk

The zero-leak proof needs a test asserting the *absence* of network traffic, and this repository has
no harness for that today. Building it is part of this slice, not an afterthought — and if it cannot
be built, the invariant is unverified and the slice is not done. Note that the same assertion is
vacuously green in slice 01a, where no collector client exists to make a request: writing it there
would have proved nothing, which is why it is here.
