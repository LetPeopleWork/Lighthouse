# SPIKE-00 Vendor Facts: Plausible Analytics vs PostHog Cloud EU

**Date**: 2026-08-22 | **Researcher**: nw-researcher (Nova) | **Epic**: 5733 Opt-In Usage Data
**Scope**: vendor-facts half of SPIKE-00. Procurement/capability probe, not a design.
**Overall confidence**: High on capability and gating; Medium on pricing and legal paperwork —
see "What I could NOT establish".

## Verdict

**No. Plausible Growth is not the obvious answer — it is not even a possible answer, and Plausible
as a whole is the weaker of the two candidates for this payload.**

Three findings, in descending order of how badly each hurts:

1. **Custom properties are a Business-plan feature.** Not Growth, not Starter. This is not an
   inference from marketing copy: the string `props` appears in the Business feature array of
   Plausible's own checked-in plan catalogue, `priv/plans_v5.json`, and in neither of the two cheaper
   plans. Four of our five heartbeat fields exist *only* as custom properties. Growth therefore
   cannot store our payload at all. The cheapest Plausible that works is **Business at 19/month**
   (10k events tier) — versus **PostHog Cloud EU at 0/month**, since our volume is roughly 3% of
   PostHog's free 1M events/month.
2. **Server-side emit to Plausible is documented to be silently dropped.** Plausible's Events API
   states that if you forward "a server, hosting provider, or CDN IP address instead of the actual
   visitor IP, Plausible's bot filtering will drop the event", and its bot filter screens roughly
   32,000 datacenter IP ranges. A customer's Lighthouse instance in Docker or Kubernetes *is* a
   datacenter IP. There is no documented opt-out and no error response — the events just are not
   there. Combined with Q8, Plausible is squeezed from both directions: blocked in the browser,
   dropped from the server.
3. **The identity models point opposite ways.** We mint an opaque instance id; PostHog's
   `distinct_id` is a required caller-supplied field, so our id *is* the identity. Plausible has no
   caller-supplied identity at all — it derives a daily visitor hash from IP + User-Agent + URL
   hostname, so a fleet posting synthetic URLs would collapse and fan out unpredictably, and its
   headline "unique visitors" number would never equal our installed base.

The honest counterweight, and it is not nothing: **Plausible is the better story on data protection
and on the air-gap requirement.** It is EU-domiciled, states plainly that "data never leaves the EU",
provides a DPA to all customers automatically, and its Community Edition is live and **AGPL-3.0**, so
a customer genuinely can run the real thing themselves. PostHog is US-domiciled with an EU region in
Frankfurt, its self-hosted option is **officially unsupported** by its own words, and I could not
resolve whether choosing Cloud EU eliminates the US transfer question or merely covers it with SCCs.

**But the air-gap requirement does not actually need a self-hostable vendor** — it needs an ingest
contract simple enough to re-implement. PostHog's is `api_key` + `event` + `distinct_id` + optional
`properties`: a 100-line minimal-API endpoint satisfies it. Plausible's drags in site registration,
URL semantics and User-Agent parsing that mean nothing to a substitute receiver.

**Recommendation to the DESIGN wave**: if a vendor is used, it is PostHog Cloud EU, emitted
**server-side**, with IP capture disabled (already the default for new EU projects) — and the open
GDPR question in gap 3 must be closed first. But Q7's finding stands and should be taken seriously:
for five fields once a day, a vendor-run endpoint we write ourselves remains the option where the
payload stays five fields and no third party joins the DPA.

## Side-by-side

| | **Plausible Analytics** | **PostHog Cloud EU** |
|---|---|---|
| **Q1 — Custom props / tier / price** | **Business plan only.** `props` absent from Starter and Growth feature arrays. **19/month** at the 10k-events tier (Starter 9, Growth 14). 30 props max per event. Currency unstated in source. | **No feature gate** — `properties` available on every plan incl. Free. **First 1M events/month free**, then ~$0.00005/event. **Our cost: 0.** |
| **Q2 — Server-side ingest** | Works headlessly *in principle*: `POST /api/event`. **Requires** `User-Agent` (used to compute the visitor id), `domain` (pre-registered), `name`, `url`. **`X-Forwarded-For` is the trap**: a server/hosting/CDN IP causes bot filtering to **drop the event silently**; ~32k datacenter ranges filtered; monitoring-tool UAs blocked. | **Purpose-built for it.** `POST /i/v0/e/` or `/batch/` on `eu.i.posthog.com`. Requires only `api_key`, `event`, `distinct_id`. **No `url`, no `domain`, no User-Agent requirement.** Nothing presumes a browser. |
| **Q3 — What the processor sees** | Claims no IPs, fingerprints or persistent identifiers are **stored**, and no personal data collected. But IP is **processed** in flight for bot filtering, visitor hashing and geolocation (`priv/ip` DB in the ingest path; country/region/city dimensions exist). **No documented switch to suppress geo or hashing.** | "IP addresses can be considered personal data under GDPR." **IP capture is disabled by default for all new Cloud EU projects**; controllable at org and project level (Settings → Project → General). PostHog = processor, you = controller, stated explicitly. GeoIP↔IP-suppression coupling not documented (gap 5). |
| **Q4 — Identity / dedup** | **Derived, not supplied.** Daily visitor hash from IP + User-Agent + a rotating salt; URL hostname also participates. A fleet on one synthetic URL + one UA template **collapses** behind shared NAT and **fans out** when egress IPs rotate. Our instance id can only ride along as a property, not as an identity. | **Caller-supplied `distinct_id`** (≤200 chars) — our minted opaque id goes in directly. 1,000 instances = 1,000 ids, deterministically. API events are "identified" by default; `$process_person_profile: false` opts out of person profiles. |
| **Q5 — DPA / residency / sub-processors** | DPA **provided automatically to all customers**, covering processor obligations. "Processed and stored in the EU on servers owned and operated by European companies. **Data never leaves the EU.**" Sub-processors referenced but **I could not enumerate them**; current host unverified. | Cloud EU **hosted in Frankfurt** (provider unnamed on that page). DPA exists at `posthog.com/dpa` and is **counter-signed on request**; a subprocessors page exists; SCCs relied on for UK/EU→US transfer. **I could not fetch any of these** — whether Cloud EU avoids US transfer entirely is **unresolved and material**. |
| **Q6 — Self-host / redirect** | **Community Edition is live and AGPL-3.0**, same repo as the cloud product, so the same Events API. Genuinely self-hostable — but a full Elixir + PostgreSQL + **ClickHouse** stack to receive 5 fields a day. Also likely inherits the datacenter-IP drop (gap 8). | Self-hosting is **"officially unsupported"** (not deprecated) — MIT, "provided without guarantee", aimed at "hobbyists… (like in your basement)", single machine, "unlikely to scale past a couple 100ks events", no support tickets, no data-loss recovery. **Mostly irrelevant to us**: the air-gapped customer re-implements a 4-field POST, not PostHog. |
| **Q8 — Ad-block (browser path)** | EasyPrivacy blocks the **script** (`/js/p.js`, `/js/plausible.*`). AdGuard blocks the **whole domain**: `\|\|plausible.io^$third-party`. | EasyPrivacy blocks the **ingest path**: `\|\|i.posthog.com/i/` matches `eu.i.posthog.com/i/v0/e`. AdGuard blocks the **whole domain**: `\|\|i.posthog.com^`. PostHog itself publishes these endpoints *for* blockers. |

## Q1 Custom properties / tier

### Plausible — custom properties are a BUSINESS-plan feature, not Growth

**Evidence**: Plausible's own docs page for custom properties carries a plan gate reading
"business" — custom properties are not available on Starter or Growth.
> "Custom properties" are a business plan feature. You may include "up to 30 different custom
> properties per event." Property names are limited to 300 characters; property values to 2000
> characters.

**Source**: [Plausible docs — Custom properties: introduction](https://plausible.io/docs/custom-props/introduction)
(source markdown: `plausible/docs` repo, `docs/custom-props/introduction.md`) — accessed 2026-08-22

**Corroboration — the plan-tier page says the same thing in prose:**
> Business: for those who need "funnels, user journeys, revenue tracking, **custom properties**,
> Stats API or Data Studio".

Note the tier list is now **four** tiers, not three: **Starter → Growth → Business → Enterprise**.
Growth is described only as "multiple sites, or need to share dashboards with clients or invite team
members" — i.e. Growth buys *seats and sites*, not *data model*. "All plans start at the same
pageview tiers. The difference is features and team size."

**Source**: [Plausible docs — Subscription plans](https://plausible.io/docs/subscription-plans)
(source markdown: `plausible/docs` repo, `docs/subscription-plans.md`) — accessed 2026-08-22

**Confidence**: High. Two independent vendor pages agree, plus third-party reviews concur
([seline.com](https://seline.com/blog/plausible-analytics-pricing),
[analytics-alternatives.com](https://analytics-alternatives.com/plausible-analytics-review-2026/)).

**So: the spike's stated hypothesis is disproven.** Four of our five heartbeat fields (version,
deployment mode, licence tier, instance id) can only exist as *queryable dimensions* as custom
properties, and custom properties start at Business. Plausible Growth cannot store our payload.

**What Growth/Starter would still give us**: a custom *event name* (goal conversion) is available on
lower tiers, so we could encode a heartbeat as an event with no dimensions, or smuggle dimensions
into the event name (`heartbeat_v26.8.14.1_kubernetes_premium`). That is a cardinality bomb and is
not queryable as separate dimensions — INFERENCE, but a direct consequence of the documented limits.

### Plausible — the price at the volume we would actually buy

Plausible's plan catalogue is checked into its own source tree as `priv/plans_v5.json`, with the
prices in `priv/plan_prices.json` keyed by Paddle product id. At the **smallest** volume tier
(10,000 monthly events) the three self-serve plans are:

| Plan | Product id (monthly, 10k tier) | Price / month | `props` in feature list? | Retention |
|---|---|---|---|---|
| Starter | 910413 | 9 | **No** | 3 years |
| Growth | 910429 | 14 | **No** | 3 years |
| Business | 910445 | **19** | **Yes** | 5 years |

**Source**: [`plausible/analytics` — `priv/plans_v5.json`](https://github.com/plausible/analytics/blob/master/priv/plans_v5.json)
and [`priv/plan_prices.json`](https://github.com/plausible/analytics/blob/master/priv/plan_prices.json) — accessed 2026-08-22

This is the vendor's own machine-readable source of truth, and it is unambiguous: the string `props`
appears in the Business feature array and in neither Starter nor Growth. **Caveat**: the JSON carries
bare numbers with no currency field. Third-party pricing round-ups render them as USD
($9 / $14 / $19), and Plausible has historically billed in EUR via Paddle with the same numerals.
Treat the *number* as High confidence and the *currency symbol* as Medium.

**So: our answer is 19/month, Business plan, at 10k events/month** — the smallest bucket, which our
volume (one heartbeat per instance per day) sits comfortably inside for a long time. INFERENCE: at
1,000 opted-in instances × 1 heartbeat/day ≈ 30k events/month, so we would step up one volume tier;
the *plan* stays Business.

### PostHog — properties are core to the data model on every plan, including Free

PostHog has no "custom properties" feature gate at all. `properties` is an optional field on the
capture API available to every project, and the whole product is usage-priced rather than
feature-tiered for this capability.

**Source**: [PostHog docs — Capture API](https://posthog.com/docs/api/capture) — accessed 2026-08-22

All five heartbeat fields map cleanly: `distinct_id` = instance identifier, and version / deployment
mode / licence tier / timestamp are event properties. All are queryable as breakdown dimensions.

**Price**: PostHog is usage-based with **the first 1,000,000 events per month free**, then
approximately **$0.00005/event** for the 1–2M band, stepping down with volume.

**Source**: I could NOT retrieve `posthog.com/pricing` directly — see "What I could NOT establish".
The free-1M figure is corroborated by PostHog's own self-host guide, which names
"**1M events/month** as the free tier limit on PostHog Cloud"
([PostHog docs — Self-host](https://posthog.com/docs/self-host)), and by several third-party
2026 pricing round-ups ([schematichq.com](https://schematichq.com/blog/posthog-pricing),
[flexprice.io](https://flexprice.io/blog/posthog-pricing-guide),
[cubeapm.com](https://cubeapm.com/blog/posthog-pricing-and-review/)). Confidence: High on the
free-1M allowance (vendor-corroborated), Medium on the exact per-event rate (third-party only).

**At our volume, PostHog Cloud EU costs 0/month.** 1,000 instances × 1 heartbeat/day ≈ 30k
events/month, which is 3% of the free allowance. Even the later product-events payload has to grow
~33× before a bill exists.

## Q2 Server-side ingest without a browser

### Plausible — the Events API works headlessly, but is actively hostile to server IPs

Plausible does expose a server-side ingest endpoint, `POST /api/event`. Its documented contract:

**Required:**
- `User-Agent` header — "is used to calculate the *user_id* which identifies a unique visitor", and
  it is parsed with the device-detector database to populate browser / OS / device dashboards.
- `Content-Type` — `application/json` or `text/plain`.
- `domain` (body) — a site already registered in the Plausible account.
- `name` (body) — `pageview`, or any custom event name.
- `url` (body) — "the page location where the event occurred"; and critically,
  "**The hostname derived from `url` takes part in unique visitor recognition.**"

**Optional:**
- `X-Forwarded-For` — sets the client IP explicitly. If omitted, **the sender's remote IP is used**.
- `props` — custom properties, "maximum of 30 key-value pairs" (Business plan only, per Q1).
- `referrer`, `revenue`, `interactive`.

**Source**: [Plausible docs — Events API](https://plausible.io/docs/events-api)
(source markdown: `plausible/docs` repo, `docs/events-api.md`) — accessed 2026-08-22

**What happens if you send synthetic values, field by field:**

- **`url` synthetic** (e.g. `https://telemetry.lighthouse.local/heartbeat`): accepted. Plausible does
  not fetch or validate the URL. But the *hostname* feeds visitor recognition (Q4), so the choice is
  load-bearing, not cosmetic.
- **`domain` synthetic**: must match a registered site or the event has nowhere to land. Not free-form.
- **`User-Agent` synthetic** (e.g. `Lighthouse/26.8.14.1 (kubernetes)`): accepted, and it will be run
  through device-detector, so our version string gets mangled into a bogus "browser/OS/device"
  reading on the dashboard. Worse — see the next point — Plausible "block[s] known bots and crawlers
  by their User-Agent header, including … **monitoring tools**".
- **`X-Forwarded-For` — this is the killer.** The docs state plainly:
  > "If you forward a server, hosting provider, or CDN IP address instead of the actual visitor IP,
  > **Plausible's bot filtering will drop the event**."

  And the bot-filtering page confirms the mechanism: Plausible filters "traffic from approximately
  **32,000 known data center IP ranges** commonly used by bots and automated tools", plus
  "our own algorithm to detect and exclude unnatural traffic patterns".

  **Source**: [Plausible docs — Bot traffic filtering](https://plausible.io/docs/bot-traffic-filtering) — accessed 2026-08-22

**Assessment (INFERENCE, but a short step from the quoted text):** a Lighthouse instance running in
Docker or Kubernetes at a customer *is* a datacenter IP, by definition. Omitting `X-Forwarded-For`
means Plausible sees the customer's server IP — a datacenter IP — and the documented behaviour is to
drop it. Setting `X-Forwarded-For` to something synthetic is the only escape, and there is no
documented "this is a server, not a bot" opt-out. There is also **no documented error signal**: the
endpoint accepts the request and the event silently vanishes, so a fleet could be reporting nothing
and we would see an empty dashboard rather than an error. This is the single biggest disqualifier
for Plausible in the server-side direction.

### PostHog — identity-based, not URL-based; no browser concept at all

PostHog's capture API is a plain POST-only public endpoint requiring exactly three things:

- `api_key` — the project token
- `event` — the event name
- `distinct_id` — the identifier, max 200 characters

`properties` and `timestamp` (ISO 8601) are optional. There is **no `url` field, no `domain` field,
and no `User-Agent` requirement**; nothing in the contract presumes a browser.

- EU ingest host: `https://eu.i.posthog.com`
- Endpoints: `/i/v0/e/` (single) and `/batch/` (many), body < 20MB

**Source**: [PostHog docs — Capture API](https://posthog.com/docs/api/capture) — accessed 2026-08-22

This is the shape our payload actually has: an opaque identity plus a bag of named properties.
Nothing has to be faked.

## Q3 What the processor sees that we did not send

### Plausible

**IP handling.** Plausible's public position is categorical:
> All information is "processed and stored in the EU on servers owned and operated by European
> companies. Data never leaves the EU." Plausible does not store "IP addresses, device fingerprints
> or any other persistent identifiers", and "no personal data is collected".

**Source**: [Plausible docs — Compliance](https://plausible.io/docs/compliance)
(source markdown: `plausible/docs` repo, `docs/compliance.md`) — accessed 2026-08-22

But "does not **store**" is not "does not **process**". The IP is demonstrably *used* in flight:

1. **Bot filtering** matches it against ~32k datacenter ranges (Q2 citation above).
2. **Visitor hashing** — the raw `User-Agent` "is used to calculate the *user_id*" (Events API,
   above), and Plausible's published method combines IP + User-Agent + a daily-rotating salt.
3. **Geolocation** — INFERENCE from source layout, and strong: the `plausible/analytics` repo
   contains a `priv/ip` directory alongside `priv/ua_inspector` and `priv/ref_inspector`, i.e. a
   local IP-geolocation database sits in the ingest path. Plausible's dashboards show country /
   region / city dimensions, which can only be derived from the connecting IP.
   **Source**: [`plausible/analytics` — `priv/`](https://github.com/plausible/analytics/tree/master/priv) — accessed 2026-08-22

**Can it be suppressed?** I found **no documented switch** to turn off geolocation or visitor
hashing on Plausible Cloud. The only lever is `X-Forwarded-For` — and per Q2, feeding it a synthetic
or datacenter value gets the event dropped rather than anonymised. So the honest reading: with
server-side emit, Plausible would geolocate **our customers' server IPs into a country column we
never asked for**, and we cannot switch it off.

### PostHog

**IP handling on Cloud EU is the standout finding:**
> "IP addresses can be considered personal data under GDPR. You can control IP data capture at both
> the organization and project levels to help maintain compliance." And for PostHog Cloud EU,
> "**IP data capture is automatically disabled by default for all new projects.**"

Manual control lives at **Settings → Project → General**.

**Source**: [PostHog docs — GDPR compliance](https://posthog.com/docs/privacy/gdpr-compliance) — accessed 2026-08-22

**Processor role**, stated by the vendor: on PostHog Cloud, "PostHog" is the data **processor** and
"You" are the data **controller**; self-hosted, you are both. (Same source.)

**Geolocation**: PostHog does GeoIP-enrich events into `$geoip_country_name` and similar properties.
INFERENCE on the coupling: disabling IP capture is the documented control that removes the input, so
suppressing IP should suppress the derived geo properties — but I did NOT find a page that states
that consequence explicitly. Flagged as a gap.

## Q4 Deduplication / identity model

### Plausible — a thousand servers can collapse into a handful of "visitors"

Plausible has no caller-supplied identity. It *derives* one:
- The `user_id` is computed from the raw `User-Agent` (Events API doc, quoted in Q2) together with
  the IP and a rotating daily salt.
- "The hostname derived from `url` takes part in unique visitor recognition" (same doc).

**Consequence for our fleet (INFERENCE, but directly entailed):** if 1,000 customer instances post
to one Plausible site with the *same* synthetic `url` hostname and the *same* User-Agent template,
the only remaining entropy in the hash is the IP. Instances behind one corporate egress NAT, or
several instances in the same cloud region behind the same proxy, would **collapse into one
visitor**. Conversely, an instance whose egress IP rotates (common in Kubernetes with a changing
NAT gateway, and guaranteed daily by the salt rotation) **fans out into several visitors**. Either
way the count is wrong, and it is wrong in a way you cannot correct after the fact, because the
inputs are not stored.

We *do* mint an instance identifier ourselves — but on Plausible it can only be carried as a custom
**property** (Business plan), and a property is not an identity. Plausible's "unique visitors" metric
will never equal our installed base. To count installs we would have to ignore the headline metric
and instead do a distinct-count over a custom property, which is exactly the Business-plan Properties
tab. **INFERENCE**: high-cardinality property values (one per install) are a poor fit for a product
whose docs frame properties as low-cardinality segments like author or plan name; I found no
documented cardinality *cap* beyond the 2000-character value limit, so this is a design-smell
argument, not a documented failure.

### PostHog — `distinct_id` is caller-supplied, which is exactly our model

`distinct_id` is a required, caller-controlled field (max 200 chars). Our minted opaque instance id
goes straight into it. No hashing, no derivation, no collapse, no fan-out: 1,000 instances are 1,000
distinct ids because we said so.

One wrinkle worth naming: events sent via the API are **"identified events" by default**, which
creates a person profile per `distinct_id`. To avoid person profiles, add
`"$process_person_profile": false` to properties.

**Source**: [PostHog docs — Capture API](https://posthog.com/docs/api/capture) — accessed 2026-08-22

**INFERENCE**: for an installed-base count we probably *want* the person profile (one "person" =
one install is a genuinely useful abstraction here), but it is worth checking the billing
implication, since PostHog historically prices person profiles differently from anonymous events.
I did not establish the 2026 billing treatment — see gaps.

## Q5 DPA + data residency

### Plausible

- **DPA**: "Plausible automatically provides a Data Processing Agreement to all customers" covering
  "GDPR processor obligations."
- **Residency**: "processed and stored in the EU on servers owned and operated by European
  companies. **Data never leaves the EU.**"
- **Sub-processors**: the compliance page points at a Security Overview that "addresses
  subprocessors", but the overview page itself did not render a named list through the route I
  could reach. **I could not enumerate Plausible's sub-processors.**

**Source**: [Plausible docs — Compliance](https://plausible.io/docs/compliance) — accessed 2026-08-22

Plausible's marketing has long named a specific EU host (historically Hetzner in Germany, later
Falkenstein/Nuremberg), but I could not verify the *current* provider from a page I could fetch
today, so I am not asserting it.

### PostHog Cloud EU

- **Residency**: PostHog Cloud EU is "a managed version of PostHog that's hosted on servers based in
  **Frankfurt**." No cloud provider is named on that page.
  **Source**: [PostHog docs — Data storage](https://posthog.com/docs/privacy/data-storage) — accessed 2026-08-22
- **Processor role**: explicitly stated (PostHog = processor, you = controller) on the GDPR page
  cited in Q3.
- **DPA**: PostHog publishes a DPA at `posthog.com/dpa`, but **I could not fetch it** (see gaps), so
  I cannot confirm today whether it is click-to-sign, counter-signature, or paid-plan-gated. The
  GDPR docs page I *could* read does not mention the DPA at all.
- **Sub-processors**: not named on any page I could reach.

## Q6 Self-host / redirect

### Plausible Community Edition — alive, AGPL, same ingest API

Plausible CE is the same codebase (`plausible/analytics`) that runs the cloud service; the plan
catalogue, ingest pipeline and Events API all live in that one repo, which is why the `priv/plans_v5.json`
citation in Q1 is authoritative for the cloud product. Because it is the same code, **CE accepts the
identical `POST /api/event` contract** — INFERENCE, but a strong one: there is no separate
cloud-only ingest implementation in the tree, and the Events API doc is not marked cloud-only.

**Licence**: Plausible relicensed the project to **AGPL-3.0** when it introduced Community Edition.
I am citing the repo itself rather than a blog post, but note I did not open the `LICENSE` blob in
this pass — flagged in gaps as a verify-before-quoting item.
**Source**: [`plausible/analytics` on GitHub](https://github.com/plausible/analytics) — accessed 2026-08-22

For our air-gapped requirement this is genuinely good: a customer can run CE and we point the
channel at it. The catch is that CE is a full Elixir + PostgreSQL + **ClickHouse** stack — a heavy
thing to ask a customer to stand up merely to receive five fields a day.

### PostHog self-hosted — officially unsupported, and PostHog says so in its own words

This confirms the suspicion in the brief, though with an important correction of vocabulary: it is
not "deprecated", it is **unsupported**.

> "self-hosted deployments are [officially unsupported]"
> "We don't offer customer support for product, infrastructure, or other questions for self-hosted
> instances."
> "you assume all responsibility and risk for your use of the product and the stack"

**Source**: [PostHog docs — Self-host](https://posthog.com/docs/self-host) — accessed 2026-08-22

The open-source disclaimer is blunter still:

> MIT licensed and "provided without guarantee."
> Intended for "hobbyists or hosting PostHog in weird and wonderful ways (like in your basement)."
> It "runs on a single machine, and thus is unlikely to scale past a couple 100ks events without
> significant effort in scaling."
> "Because hosting environments can vary so widely, we cannot answer tickets and cannot help debug"
> — and they are "unable to support recovery from data loss."

Self-hosted also lacks a long list of cloud features (group analytics, data pipelines, extended
retention, SAML/RBAC/SSO, audit logs, and more).

**Source**: [PostHog docs — Open-source disclaimer](https://posthog.com/docs/self-host/open-source/disclaimer) — accessed 2026-08-22

**Assessment**: for *our* purpose the unsupported status matters far less than it looks. We are not
asking the air-gapped customer to run PostHog. We are asking them to receive a POST. And PostHog's
capture contract (`api_key`, `event`, `distinct_id`, optional `properties`) is trivially
re-implementable — a 100-line ASP.NET minimal-API endpoint satisfies it. Plausible's contract is
*also* re-implementable, but it drags along `domain` registration, `url` semantics and User-Agent
parsing that mean nothing to a substitute receiver.
## Q7 Is there a third category worth naming?

Short answer: **there is a category, it is real, and neither candidate beats a 100-line vendor-run
endpoint for our specific case.** I am naming two and then arguing against both.

### Candidate 1 — Aptabase

Aptabase is open-source, privacy-first analytics built explicitly for **installed apps** (mobile,
desktop, Electron, Tauri) rather than for websites. That framing is much closer to ours than
Plausible's: it assumes no browser, no referrer, no page, and it ships SDKs — including a .NET/NuGet
one — that model exactly "an app instance emits a named event with properties".

**Sources**: [aptabase.com](https://aptabase.com/), [`aptabase/aptabase` on GitHub](https://github.com/aptabase/aptabase),
[NuGet — aptabase profile](https://www.nuget.org/profiles/aptabase) — accessed 2026-08-22

**Pricing**: a free tier exists; paid plans are event-volume based and third-party reviews put the
entry paid plan at about **$14/month**. **Confidence: Low** — aptabase.com would not load for me and
I am relying on a single third-party review ([toolradar.com](https://toolradar.com/tools/aptabase)).
Do not quote this number without checking the page.

**Why it still does not win**: it is a small vendor. The GDPR question in this Epic is not "can I get
analytics" but "can I name a processor in a DPA and defend where the data rests". A publicly
available, counter-signable DPA and a named sub-processor list are table stakes, and I could not
confirm Aptabase has either. Self-hosting Aptabase solves the air-gap requirement — but if we are
running the receiver anyway, we are back to writing our own endpoint with extra steps.

### Candidate 2 — Scarf

Scarf is the closest thing to a purpose-built "phone-home for distributed software" vendor. It was
built for open-source maintainers who want to know who runs their software, it explicitly supports
**custom telemetry** via "unauthenticated requests to Scarf Gateway", and its whole reason to exist
is measuring an installed base you cannot see.

**Sources**: [Scarf docs — Custom telemetry](https://docs.scarf.sh/custom-telemetry/),
[about.scarf.sh](https://about.scarf.sh/) — accessed 2026-08-22

**Pricing**: a free starter plan with **3 monthly tracked companies**; additional seats ~**$30/month**
and additional tracked companies ~**$3 each** on the self-serve plan, with a newer model built on
"Company Unlocks" and "Runs". **Confidence: Medium** — from Scarf's own pricing and new-pricing-model
posts, but retrieved through search summaries rather than a page I rendered myself.
**Sources**: [Scarf — Pricing](https://about.scarf.sh/pricing/),
[Scarf — Introducing Scarf's new pricing model](https://about.scarf.sh/post/introducing-scarfs-new-pricing-model/)

**Why it actively disqualifies itself**: read the pricing unit. Scarf bills per **tracked company**,
because its core value proposition is resolving the traffic it receives back to the *organisation*
that sent it — IP-to-company enrichment. That is a de-anonymisation product. Our consent screen will
tell a customer we collect five fields and nothing identifying; routing that through a vendor whose
business model is naming their employer is a contradiction we would have to explain, and the honest
version of that explanation loses us the opt-in. Also note the pricing scales with *companies*, i.e.
with exactly the number we are trying to grow.

### The blunt part

The honest answer to "does either beat a 100-line vendor-run endpoint" is **no**.

Our requirements are unusual in a way that keeps defeating the market: single-digit fields, ~one
event per instance per day, opt-in, no personal data, a hard requirement that a customer be able to
substitute their own receiver, and a GDPR story simple enough to write on one page. Every vendor in
this space — web analytics, product analytics, and the installed-software niche alike — is priced
and designed for **seat or session volume**, and each one adds a named third party to our DPA in
exchange for a dashboard we could approximate with a SQL query. The vendor products bring
value we are not buying (funnels, replays, cohorts, company enrichment) and costs we are
(sub-processor disclosure, ad-block exposure, a schema that is not ours).

The one genuine argument *for* a vendor is that we do not want to run and secure an internet-facing
ingest endpoint, retain data, and build the dashboard ourselves. That is a real cost and it is not
zero. But it is a cost we can size, and it is the only option where our five fields stay five fields.

## Q8 Ad-block reality for the browser-side path

Method: rather than estimate, I read the filter lists themselves from their canonical repositories
and searched for the two vendor domains. Findings are per-list and per-domain. **No blocked-user
percentage is offered, because none is verifiable from a filter list.**

### EasyPrivacy — `easyprivacy/easyprivacy_thirdparty.txt`

Both vendors are present.

Plausible (script paths, not the ingest endpoint):
```
||plausible.io/js/p.js
||plausible.io/js/plausible.
||plausible.server.hakai.app^
||plausibleio.workers.dev^$third-party
```

PostHog (including the ingest path):
```
||app.posthog.com/e/?compression=
||app.posthog.com/e/?ip=
||app.posthog.com/static/array.js
||app.posthog.com/static/recorder-v2.js
||i.posthog.com/*&ip=
||i.posthog.com/*/?ip=
||i.posthog.com/i/
||i.posthog.com/static/array.js
||posthog.com/*/?retry_count
```

**Source**: [`easylist/easylist` — `easyprivacy/easyprivacy_thirdparty.txt`](https://github.com/easylist/easylist/blob/master/easyprivacy/easyprivacy_thirdparty.txt) — accessed 2026-08-22

Two things matter here:

1. `||i.posthog.com/i/` matches **`https://eu.i.posthog.com/i/v0/e`**, because `||` anchors at a
   domain boundary and therefore also matches subdomains of `i.posthog.com`. EasyPrivacy blocks the
   exact EU capture endpoint we would use, by path.
2. For Plausible, EasyPrivacy blocks the **tracker script** (`/js/p.js`, `/js/plausible.*`) — I did
   **not** see a rule for `plausible.io/api/event` in this list. A browser-side integration that
   loads Plausible's script dies at script load; a `fetch()` straight to `/api/event` would survive
   *this* list specifically. It does not survive AdGuard (below).

### AdGuard — `SpywareFilter/sections/tracking_servers.txt` (AdGuard Tracking Protection / Base)

Both vendors are present, and here the rules are **whole-domain**, not path-scoped:

```
||plausible.io^$third-party
||plausible.vidsonic.net^
||plausible.scimago.es^
||i.posthog.com^
```

**Source**: [`AdguardTeam/AdguardFilters` — `SpywareFilter/sections/tracking_servers.txt`](https://github.com/AdguardTeam/AdguardFilters/blob/master/SpywareFilter/sections/tracking_servers.txt) — accessed 2026-08-22

`||plausible.io^$third-party` blocks **every** request to plausible.io from a third-party context —
script *and* `/api/event`. `||i.posthog.com^` blocks all of `i.posthog.com` and its subdomains,
including `eu.i.posthog.com`, unconditionally. For AdGuard users the browser-side path is dead for
both vendors, with no clever endpoint choice available.

### uBlock Origin's own filters — `filters/privacy.txt`

Neither `plausible` nor `posthog` appeared in the portion of this file I retrieved. **Stated as a
non-finding, not as an absence**: the file is large and I cannot rule out truncation in the fetch, so
treat this as "not confirmed present" rather than "confirmed absent".

**Source**: [`uBlockOrigin/uAssets` — `filters/privacy.txt`](https://github.com/uBlockOrigin/uAssets/blob/master/filters/privacy.txt) — accessed 2026-08-22

**This barely matters, and here is why**: uBlock Origin enables **EasyPrivacy by default**. Whether
uBlock's *own* supplementary list names these domains is close to irrelevant, because the default
uBlock install already carries the EasyPrivacy rules quoted above. INFERENCE, but low-risk.

### PostHog agrees, in writing

PostHog does not contest any of this — it publishes instructions *for* blockers:

> "Ad and tracking blockers should target these endpoints": `https://us.i.posthog.com/i/v0/e` and
> `https://eu.i.posthog.com/i/v0/e`. They ask that `/flags` and `/static/` be left alone "at risk of
> breaking site functionality".

**Source**: [PostHog docs — Ad blockers](https://posthog.com/docs/privacy/ad-blockers) — accessed 2026-08-22

### What this means for the emit-direction decision

The browser-side path is blocked, by default, on at least two of the three major list ecosystems, for
both vendors, at the exact endpoints we would use. Our audience is engineers running a forecasting
tool for software teams — the population with the highest ad-blocker density there is. I will not put
a number on it, but the direction is not in doubt.

That leaves server-side. And server-side is precisely where **Plausible's datacenter-IP bot filter
silently drops the event** (Q2). Plausible is squeezed from both sides: blocked in the browser,
dropped from the server. PostHog is blocked in the browser but entirely happy server-side, since its
capture API never assumed a browser in the first place.

Secondary consideration, unresolved: both vendors offer proxying (Plausible has a `proxy/` docs
section; PostHog documents reverse proxies) to evade blocking. I deliberately did not pursue it. A
product asking for explicit opt-in consent should not then route the traffic around the user's own
blocker — the reputational downside if that were noticed vastly exceeds the data's value. Recording
it here as a deliberate non-recommendation rather than an oversight.

## What I could NOT establish

Blunt list. Each of these is a real gap, not a rounding error.

1. **PostHog's exact per-event price, from PostHog.** `posthog.com/pricing` was unreachable from this
   environment (TLS chain error, see note below). The **free 1M events/month** figure *is*
   vendor-corroborated via the self-host doc, so the "PostHog costs us nothing at our volume"
   conclusion is safe. The `$0.00005/event` overage rate is third-party only. **Verify before quoting
   in any commercial document.**
2. **Plausible's currency.** `plan_prices.json` gives bare numerals 9 / 14 / 19 with no currency
   field. USD and EUR are both plausible readings (Plausible bills through Paddle). The *relative*
   fact — Business is the cheapest plan with `props` — is unaffected.
3. **PostHog's DPA terms.** Search results indicate PostHog will **counter-sign** a DPA generated
   through their process, maintains a **subprocessors page**, relies on **SCCs** for UK/EU→US
   transfers, and names a DPO. I could not fetch `posthog.com/dpa` or `posthog.com/subprocessors`
   myself, so I am not quoting terms. Critically unresolved: **whether choosing Cloud EU means no US
   transfer occurs at all**, or whether SCCs are still in play because PostHog Inc. is US-domiciled.
   That question is material to the GDPR story and must be answered before selection.
4. **Plausible's sub-processor list.** The compliance page points to a Security Overview that
   "addresses subprocessors" but I never rendered a named list. Likewise I could not verify the
   *current* EU hosting provider.
5. **Whether disabling IP capture on PostHog also disables GeoIP enrichment.** Logically it should —
   no IP, no geo — but I did not find a page stating it. Worth a five-minute check in a trial project:
   send one event and inspect it for `$geoip_*` properties.
6. **PostHog's 2026 billing treatment of person profiles vs anonymous events.** Relevant only if our
   volume ever approaches the free ceiling, which is far away.
7. **Plausible CE's `LICENSE` blob, read directly.** The GitHub licence API reports **AGPL-3.0** for
   `plausible/analytics`, which I am treating as sufficient, but I did not open the file text.
8. **Whether Plausible CE's ingest applies the same datacenter-IP bot filter.** Same codebase implies
   yes, but if a customer self-hosts CE as their substitute endpoint, they would inherit the exact
   drop behaviour that makes Cloud unusable for us. Unverified and important.
9. **uBlock Origin's own `privacy.txt`** — see Q8; a non-finding, possibly a truncated fetch.

### A note on retrieval, because it shaped this document

**Neither `plausible.io` nor `posthog.com` could be fetched from this environment** — both failed
with "unable to verify the first certificate", as did an `r.jina.ai` fallback (401). Every vendor
fact above was therefore retrieved from the vendors' **own public source repositories** on GitHub
(`plausible/docs`, `plausible/analytics`, `PostHog/posthog.com`), which is arguably a *better* class
of evidence for the questions that mattered — `priv/plans_v5.json` is the code that gates the
feature, not marketing copy about it. But it means **pricing pages, DPAs and legal pages — the
content that does not live in a docs repo — are systematically the weakest-sourced parts of this
document.** The gaps above cluster there for exactly that reason. Citations point at canonical vendor
URLs; the retrieval path was the corresponding repo file, named inline wherever it differs.
