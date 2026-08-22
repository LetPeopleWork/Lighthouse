# SPIKE-00 — Which collector, and which direction does the event travel?

**This is a SPIKE, not a slice.** It ships no user value and must not be released as one.
Timebox: 1 day. Output: a written recommendation, not code that survives.

## Goal

Decide the collector and the emit direction before the walking skeleton is built against either.

## Why this cannot be deferred to DESIGN

The two questions are coupled and both have consequences DESIGN cannot undo cheaply:

- **Direction.** Server-emitted events are not ad-blocked but expose the customer's *server* IP to
  the processor. Browser-emitted events expose the *user's* IP instead and will be blocked outright
  for a meaningful share of this product's audience, which is engineers running uBlock. The
  undercount is not estimable from documentation — it has to be probed.
- **Collector.** Plausible is a web-analytics product keyed on domains and pageviews. Its
  server-side Events API wants a URL and a User-Agent it does not have, and performs IP-based
  geolocation and de-duplication. Whether custom properties (the fields the heartbeat needs) are
  available on Growth or only on Business is a plan-tier question with an architectural consequence
  and a monthly bill attached.

## What to probe

Against the slice-01 heartbeat payload — instance identifier, version, deployment mode, licence
tier, timestamp — for each of:

1. **Plausible Growth** (the Epic's own suggestion; already the website's analytics vendor)
2. **PostHog Cloud EU**
3. **A vendor-run minimal collector** (a single endpoint the vendor operates)

Answer for each:

- Can it store and query all five fields, on the tier we would actually buy? Name the tier and price.
- What does it see that we did not send — IP, geo, User-Agent — and can that be suppressed?
- Is it a GDPR processor we can name in a DPA, and where does the data rest?
- Can a customer point the channel at their own endpoint instead (#5015's air-gap requirement)?
- Server-side ingest: does it work at all without a browser context?
- Browser-side ingest: what fraction of a realistic sample is blocked? Measure, do not estimate.

## Learning hypothesis

**Disproves "Plausible Growth is the obvious answer" if** the plan tier that carries five custom
fields is Business rather than Growth, or if server-side ingest without a browser context is
degraded enough that the payload arrives incomplete.

**Confirms** the direction decision, which is the input slice 01 cannot start without.

## IN scope

- Throwaway probe code against all three, sending one real heartbeat payload.
- An ad-block measurement for the browser-side path.
- Pricing and DPA availability, written down with the tier named.
- A one-page recommendation with the trade-offs stated.

## OUT of scope

- Any production code. Nothing from this SPIKE is merged.
- Signing anything. Procurement follows the recommendation, it does not happen inside it.
- Designing the consent record, the indicator, or the emitter.

## Acceptance criteria

- AC-00.1 A written recommendation naming one collector, one emit direction, and the reason.
- AC-00.2 The tier and monthly cost of the recommended option, stated as a number.
- AC-00.3 A statement of what the processor sees beyond the payload, and whether it can be suppressed.
- AC-00.4 A statement of how a customer redirects the channel to their own endpoint.
- AC-00.5 For the browser-side path, a measured block rate — not an estimate.

## Dependencies

None. This is the first thing that happens.

## Effort

1 day, timeboxed. If the answer is not clear at the end of it, the fallback is the vendor-run minimal
collector, because it is the only option with no third-party processor to negotiate and no plan tier
to discover.

## Reference class

The website's own analytics decision (ADR-037) reached "swappable sink, decide the vendor at DELIVER"
and then never decided. That is the failure mode this SPIKE exists to avoid: a port with no adapter
behind it and a KPI that stays unmeasured.
