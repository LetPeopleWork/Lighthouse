# SPIKE-00 Findings — which collector, and which direction does the event travel?

Feature: `epic-5733-opt-in-usage-data` · ADO #5833 · Run 2026-08-22 · Timebox 1 day, used ~1h.

Companion document: `vendor-research.md` (full citations, per-question evidence, gaps).

> **The collector recommendation in this document was superseded after the spike closed.** Everything
> here about Plausible stands. The recommendation of a vendor-run Supabase collector does not: the
> data-protection gap that ranked PostHog behind it was closed afterwards, and the decision went to
> **PostHog Cloud EU**. Read `wave-decisions.md` for the decision, the evidence that closed the gap,
> and the two statements below that it corrects. This document is left as the spike's own record.

---

## Verdict

**DISPROVED — "Plausible Growth is the obvious answer" is wrong twice over.** It is not a matter of
paying for a higher tier. Plausible cannot carry this payload from a server at any price, and the
browser path it *can* carry is blocked for this product's audience.

**Recommendation: a vendor-run minimal collector on the existing Supabase project, emitting
server-side.**

Both halves of that were probed, and neither is the fallback the slice brief allowed for — the
collector wins on merit, and the direction follows from it rather than the other way round.

---

## AC-00.1 — The recommendation, and the reason

| | Answer |
|---|---|
| **Collector** | A vendor-run minimal endpoint: one Supabase Edge Function + one table on the project that already backs the lead funnel and the user survey. |
| **Direction** | **Server-side.** The .NET backend posts; the browser never talks to the collector. |
| **Reason, in one sentence** | Only an endpoint we operate lets us guarantee the customer's IP is not stored, and that guarantee is what the consent dialog is promising (AC-02.1, AC-02.2) — with a hosted analytics vendor, IP handling is the vendor's behaviour, not our decision. |

### Why not Plausible

Two independent disqualifiers, both verified from Plausible's own published source rather than from
marketing copy.

**1. Server-side events from a datacenter IP are silently dropped.** From the Events API reference,
verbatim:

> If you forward a server, hosting provider, or CDN IP address instead of the actual visitor IP,
> Plausible's bot filtering will drop the event. The API still returns HTTP 202 with no obvious
> error, but the event is not recorded. You can confirm this by checking for `x-plausible-dropped: 1`
> in the response headers.

Every Lighthouse instance in Docker or Kubernetes *is* a hosting-provider IP. There is no opt-out.
The failure mode is the worst available one: HTTP 202, no error, no data — an installed-base census
that silently reports a fraction of the installed base, and no way to tell which fraction.

Compounding it: `User-Agent` is a **required** header, and Plausible uses it both to compute the
visitor identity and to populate the Browsers / Operating Systems / Devices tabs. A .NET backend has
no browser User-Agent. Whatever we invent either fails `device-detector` recognition or fabricates a
browser dimension that means nothing.

**2. Custom properties are Business-tier, not Growth.** Read directly from
`plausible/analytics:priv/plans_v5.json` — the file that gates the feature in their code, not a
pricing page:

| Plan (10k events/mo) | Sites | Members | Retention | Features |
|---|---|---|---|---|
| starter | 1 | 0 | 3y | `goals` |
| growth | 3 | 3 | 3y | `goals`, `shared_links`, `site_segments`, `site_annotations` |
| business | 10 | 10 | **5y** | the above + **`props`**, **`stats_api`**, `revenue_goals`, `funnels`, `consolidated_view` |

Prices, mapped through `priv/plan_prices.json` by product id (the file carries bare numerals — no
currency field, so the unit is unconfirmed):

| | 10k events/mo | 100k events/mo |
|---|---|---|
| starter | 9 / mo · 90 / yr | 19 / mo · 190 / yr |
| growth | 14 / mo · 140 / yr | 29 / mo · 290 / yr |
| business | **19 / mo · 190 / yr** | 39 / mo · 390 / yr |

Four of the five heartbeat fields can only exist as custom properties, so Growth cannot store the
payload at all. **`stats_api` is Business-only too** — worth naming separately, because reading the
data back programmatically (and the block-rate measurement below) needs it.

The price delta is trivial. The point is that the Epic's own suggested plan is structurally unable to
hold the payload, which is exactly the pre-commitment this SPIKE existed to test.

**3. It is keyed on a domain, and ours is the marketing site.** `website:src/lib/plausible.ts` runs
`Plausible({ domain: "letpeople.work" })`. A heartbeat from a customer's server has no URL on that
domain. Synthetic URLs into the same site would pollute the funnel dashboard the Epic-5123 KPIs read,
so a second site is mandatory — and whatever URL we invent becomes the primary dimension of a
product-analytics dataset that has no pages in it.

### Why not PostHog Cloud EU

Technically it fits — `api_key` / `event` / `distinct_id` / `properties`, no browser assumed, IP
capture disableable, properties ungated on the free tier, and ~30k events/month at 1,000 instances is
about 3% of the 1M free allowance. It fails on paperwork and proportion, not capability:

- PostHog Inc. is US-domiciled. Whether choosing Cloud EU removes the EU→US transfer question or
  merely papers it over with SCCs **could not be established** — and it is material, because DoR-9
  is a legal sign-off and this would be the thing being signed off.
- Its DPA terms and sub-processor list could not be retrieved.
- It brings an entire product-analytics platform, its own identity model, and a second processor
  relationship, to carry five fields once a day.

**CORRECTED after the spike closed.** The first two bullets were the reason PostHog ranked second,
and the first one overstated what the evidence supported. The sub-processor list and the residency
position were retrieved from PostHog's own repository, and they answer the transfer question: EU
Cloud rests in Frankfurt and every EU-cloud sub-processor is EU-located. The DPA *terms* remain
unread. See `wave-decisions.md` for the full evidence and what it changed.

### Why the vendor-run collector — and why it is not the fallback

The slice brief listed this as the timebox fallback. It is not being picked as one.

- **It is not greenfield.** `website:supabase/` already carries `migrations/0001..0006` and four Edge
  Functions (`capture-lead`, `submit-survey`, `request-trial`, `stripe-webhook`), plus an internal
  authenticated dashboard (ADR-042/033). This is one more function and one more table on
  infrastructure that already exists and is already an EU processor relationship in the privacy copy.
- **It satisfies the air-gap requirement in the cheapest possible way.** #5015 requires a customer be
  able to redirect the channel. When the protocol is "POST this JSON to this URL", the substitute
  endpoint is anything that accepts a POST. With Plausible the substitute would be a full Plausible
  Community Edition deployment — and CE may well inherit the same datacenter-IP drop, which was not
  established.
- **It is the only option where "we do not store your IP" is a statement about our own code.** With
  any hosted vendor that sentence in the consent dialog is a claim about someone else's behaviour.
- **ADR-037 already named this as the revisit condition.** It rejected a Supabase events table for
  v1 with: *"Revisit if a vendor is declined long-term and richer funnel analysis is needed."* A
  vendor has now been declined.

Cost: we own the uptime, the schema, and the dashboard query. Against a once-a-day fire-and-forget
POST whose failure mode is defined as silent (AC-04.5), that is a small surface.

---

## AC-00.2 — Tier and monthly cost of the recommendation

**Marginal cost: 0.** The Supabase project is already provisioned and paid for by Epic #5123.
At 1,000 consenting instances the heartbeat is ~30,000 rows/month — small on any Supabase tier.
Slice 04's product events raise that; the ceiling should be re-checked at slice 04, not now.

For the record, the options not taken: Plausible **Business, 19/mo** (10k events; unit unconfirmed —
the source JSON has no currency field). PostHog Cloud EU **0/mo** at our volume.

---

## AC-00.3 — What the processor sees beyond the payload, and whether it can be suppressed

| | Sees beyond the payload | Suppressible |
|---|---|---|
| Plausible | Server IP (required header), derived country, User-Agent, a daily visitor hash of IP+UA+hostname | No — the IP is not optional, it is the bot filter's input |
| PostHog Cloud EU | Client IP, GeoIP enrichment | ~~IP capture is off by default on new Cloud EU projects; whether that also kills GeoIP is undocumented (INFERENCE: yes)~~ **CORRECTED — see `wave-decisions.md`.** Discarding the IP does *not* stop GeoIP: their docs state transformations "can still use the IP before it is discarded". Two separate settings. |
| **Vendor-run (recommended)** | The TLS peer IP reaches the edge, as it does for any HTTP request | **Yes, by our own code** — the function does not read or persist it, and that is assertable in a test we own |

The third row is the argument. On the first two, "we do not store your IP" is a claim about a
vendor's behaviour that we cannot test. On the third it is a line of our own code and a CI assertion,
which is the standard the rest of this Epic is held to (`OUT-usagedata-payload-purity`).

---

## AC-00.4 — How a customer redirects the channel to their own endpoint

A single configuration value — the collector base URL — with the vendor endpoint as its default. The
contract is one POST of a JSON object with the documented fields, and a 2xx meaning accepted.

That is deliberately the thinnest contract available, because #5015's requirement is that an
air-gapped customer can *substitute* the endpoint, not that we ship them a collector. A ten-line
handler satisfies it. Shipping and supporting a collector image remains out of scope.

Had we chosen Plausible, this requirement would have meant deploying Plausible Community Edition —
and whether CE inherits the datacenter-IP drop was not established, so the substitute might silently
drop everything the same way.

---

## AC-00.5 — Measured block rate for the browser-side path

**NOT MEASURED. This acceptance criterion is not met, and it is the one gap in this SPIKE.**

What was established instead, and it is verifiable rather than estimated:

| Domain | List | Rule scope |
|---|---|---|
| `plausible.io` | EasyPrivacy | the tracker **script** (`plausible.io/js/p.js`) |
| `plausible.io` | AdGuard | **whole domain**, third-party scope |
| `i.posthog.com` / `eu.i.posthog.com` | EasyPrivacy | the **ingest path** (`i.posthog.com/i/`) |
| `i.posthog.com` | AdGuard | **whole domain** |

uBlock Origin's own `privacy.txt` showed no match, flagged as a possibly-truncated non-finding and
near-moot in any case: uBlock ships EasyPrivacy enabled by default. PostHog publishes those endpoints
itself and asks blockers to target them.

So for both hosted candidates the browser path is blocked by the default configuration of the most
common blocker, for an audience of engineers. No percentage is claimed, because none was measured.

**The measurement that would close AC-00.5, and who can run it.** ADR-037 dual-sources the Epic-5123
funnel: a Supabase `responses` row and a Plausible event both record the same act. The gap between
the two counts over the same window *is* a measured block rate for exactly this audience — people who
visit letpeople.work. It needs two numbers:

1. `select count(*) from responses where created_at between <start> and <end>`
2. The Plausible count of the corresponding completion event over the same window.

Neither is reachable from this environment: the website repo holds no Plausible API key (the tracker
needs none to send), and per the plan table above the **Stats API is Business-tier**, so if the
account is on Growth the number can only be read off the dashboard by hand.

This is worth running whichever collector wins, because it is the only honest number anyone in this
project has ever had about how much of its own audience blocks analytics.

---

## Findings outside the acceptance criteria

These came out of the probe and change work elsewhere.

### F1 — The website may be sending custom properties into a void, today

`website:src/lib/plausible.ts` exports `trackEvent(name, props)` and it is live in `trackDownload`,
`Assessment.tsx`, `SizingPoker.tsx`, `SurveyForm.tsx`, `NotFound.tsx`. Property breakdowns are a
`props` feature, and `props` is **Business-only**.

If the letpeople.work account is on Growth, Plausible accepts those events and the properties are not
queryable in the dashboard. ADR-037's KPI 5 (per-band click-through) and KPI 8 (dwell) are sourced
from exactly those properties.

**Checkable in ten seconds:** open the Plausible dashboard, click the `Download` goal, and see
whether a property breakdown by edition / platform / format / source is offered. If it is not, this
is a live defect in shipped Epic-5123 work and belongs on the board independently of #5733.

### F2 — Deployment mode cannot be emitted as the payload describes it

`Services/Implementation/PlatformService.cs` produces `SupportedPlatform {Docker, Windows, Linux,
MacOS}` plus a separate `IsStandalone` flag. `IsDocker()` keys on `DOTNET_RUNNING_IN_CONTAINER`,
`/.dockerenv` and `LIGHTHOUSE_DOCKER` — all true inside a Kubernetes pod. `KUBERNETES_SERVICE_HOST`
appears nowhere in the repository.

So a Kubernetes instance reports as Docker. AC-04.2 names deployment mode as a consented payload
field and AC-02.1 requires the dialog to enumerate the payload by name, so the value set is part of
what the user consents to. DESIGN must either define the field as what `PlatformService` can actually
produce, or slice 01 adds Kubernetes detection.

### F3 — There is no outbound chokepoint to assert the absence of traffic against

`GitHubService` constructs `new GitHubClient(...)` directly rather than going through
`IHttpClientFactory`; only three named clients are registered (`Program.cs:346,356,361`).
`OUT-usagedata-zero-leak-before-consent` has to assert that *nothing* leaves, and there is no single
seam through which all outbound traffic passes. DEVOPS already carries this as a missing harness
capability; this is the concrete reason it is missing.

### F4 — Registering the emitter prices a CI cycle

The daily emit hangs naturally off the `BackgroundServices/Update/UpdateServiceBase` pattern, and a
new outbound client is a fourth `AddHttpClient`. Both mean editing `Program.cs`, which forces the
full backend Integration suite — live Jira / Linear / ADO / ServiceNow tests and their flake
exposure. Not a blocker; a known cost of slice 01.

### F5 — ADR-037 is the reference class, stated in its own words

The slice brief cites ADR-037 as the failure to avoid. Its Consequences section reads:
*"the precise analytics vendor is a DELIVER detail (flagged) — the port makes that deferral safe."*
The deferral was not safe. The ADR named no vendor, and Plausible was wired at the keyboard.

This SPIKE therefore returns a **named adapter**, not a port with a decision attached to it. DESIGN
may still put a port in front of it; it may not leave the adapter unnamed.

---

## Design implications

1. The emit path is server-side, fire-and-forget, once per day, degrading silently (AC-04.5). No
   browser ever contacts the collector — which also means the consent token never leaves the browser
   as a network identifier to the collector.
2. The collector base URL is configuration with a default, and the wire contract is a documented JSON
   POST — that *is* the air-gap story (AC-00.4), so it must be specified precisely enough for a
   customer to reimplement in ten lines.
3. Because we operate the endpoint, "the IP is not persisted" is a testable assertion in our code
   rather than a vendor claim. DESIGN should place it where a test can reach it.
4. The deployment-mode value set is a payload-contract decision, not an implementation detail (F2).
   Widening it later is a re-consent question under AC-08.5.
5. The instance identifier is ours to define — no vendor identity model constrains it. Neither
   Plausible's daily IP+UA hash nor PostHog's `distinct_id` is in play.
6. There is nothing to procure and no DPA to sign before slice 01. The prerequisite the delta listed
   as a lead-time dependency is discharged by choosing an endpoint we already operate.

## Constraints discovered

- Server-side emit to Plausible is not merely awkward, it is silently dropped at datacenter IPs. Any
  future reconsideration of Plausible for this channel starts from that fact.
- The browser path is blocked at whole-domain scope by AdGuard and at script/ingest scope by
  EasyPrivacy for both hosted candidates, and uBlock enables EasyPrivacy by default.
- Plausible's Stats API is Business-tier, so the current account may have no programmatic read at all.
- No measured block rate exists for this product's audience, and none can be produced without the two
  numbers named under AC-00.5.
