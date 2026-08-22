# SPIKE Decisions — epic-5733-opt-in-usage-data

SPIKE-00 · ADO #5833 · Run 2026-08-22.

## Assumption Tested

Two coupled questions, per the slice brief: **which collector**, and **which direction does the event
travel** — server-side or browser-side. The pre-commitment under test was the Epic's own suggestion,
"Upgrade to Plausible Growth Plan for this?".

## Probe Verdict

**DISPROVED.** Plausible Growth cannot carry the payload, and Plausible cannot carry it server-side at
any tier. Two independent findings, both from Plausible's own published source:

- `props` (and `stats_api`) are gated to the **Business** plan in `priv/plans_v5.json`. Four of the
  five heartbeat fields can only exist as custom properties, so Growth cannot store the payload.
- The Events API documents that a request forwarding a server, hosting-provider or CDN IP is dropped
  by bot filtering, returning HTTP 202 with no error and recording nothing. Every containerised
  Lighthouse instance is such an IP.

The browser path, the only one Plausible can serve, is blocked at whole-domain scope by AdGuard and at
script scope by EasyPrivacy — which uBlock Origin enables by default, for an audience of engineers.

## Decision

| | |
|---|---|
| **Collector** | A vendor-run minimal endpoint — one Supabase Edge Function and one table on the project that already backs the Epic-5123 lead funnel and the user survey. |
| **Direction** | **Server-side.** The .NET backend posts; the browser never contacts the collector. |

Rejected: **Plausible** (cannot hold the payload; server-side emit silently dropped);
**PostHog Cloud EU** (technically clean and free at our volume, but US-domiciled with an
unestablished transfer position and unretrievable DPA terms — the precise thing DoR-9 would sign off);
**third-category vendors** (Aptabase is the right shape but its DPA could not be confirmed; Scarf bills
per *tracked company* because its thesis is resolving traffic back to an employer, which contradicts
our consent screen outright).

Reasoning in full: `findings.md`. Evidence and citations: `vendor-research.md`.

## Promotion Decision

**DISCARD** — decided at the gate on 2026-08-22, and it is also what the slice brief mandates: *"OUT
of scope: any production code. Nothing from this SPIKE is merged."*

`findings.md` is the deliverable. No walking skeleton is built here, because slice 01 (#5834) **is**
the walking skeleton and is a separate story — one that additionally may not ship until DoR-9 closes.

Probe artifacts were scratch-only (no repository code was written at any point) and are discarded.

## Design Implications

1. Emit is server-side, fire-and-forget, once per day, degrading silently (AC-04.5). No browser ever
   contacts the collector — so the consent token never travels to the collector as an identifier.
2. The collector base URL is configuration with a default, and the wire contract is a documented JSON
   POST. That contract *is* the air-gap story (AC-00.4, inherited from #5015), so DESIGN must specify
   it precisely enough for a customer to reimplement in ten lines.
3. Because we operate the endpoint, "your IP is not stored" is an assertion about our own code rather
   than a claim about a vendor's behaviour. DESIGN should place it where a test can reach it — the
   same standard `OUT-usagedata-payload-purity` sets for the payload.
4. The deployment-mode value set is a payload-contract decision, not an implementation detail. See
   Constraints below.
5. The instance identifier is ours to define outright. No vendor identity model constrains it —
   neither Plausible's daily IP+UA hash nor PostHog's `distinct_id` is in play.
6. Nothing is left to procure and no DPA needs signing before slice 01. The delta listed vendor
   procurement as a lead-time prerequisite; choosing an endpoint we already operate discharges it.
7. DESIGN returns a **named adapter**. It may put a port in front of it; it may not leave the adapter
   unnamed. ADR-037 did exactly that — *"the port makes that deferral safe"* — and the vendor was
   never chosen, which is the reference class this SPIKE existed to avoid.

## Constraints Discovered

- **Deployment mode cannot be emitted as the payload describes it.** `PlatformService` reports Docker
  for a Kubernetes pod: `IsDocker()` keys on `DOTNET_RUNNING_IN_CONTAINER`, `/.dockerenv` and
  `LIGHTHOUSE_DOCKER`, all true inside a pod, and `KUBERNETES_SERVICE_HOST` appears nowhere in the
  repository. AC-02.1 has the dialog enumerate the payload by name, so the value set is part of what
  the user consents to — widening it later is a re-consent question under AC-08.5.
- **No outbound chokepoint exists.** `GitHubService` builds its own `GitHubClient` rather than using
  `IHttpClientFactory`, and only three named clients are registered. `OUT-usagedata-zero-leak-before-consent`
  must assert an *absence* of traffic with no single seam to observe.
- **The emitter's registration prices a CI cycle.** A hosted service and a fourth `AddHttpClient` both
  edit `Program.cs`, which forces the full backend Integration suite — live connector tests and their
  flake exposure.
- Any future reconsideration of Plausible for this channel starts from the datacenter-IP drop, not
  from the tier price.
- Plausible's Stats API is Business-tier, so the letpeople.work account may have no programmatic read
  at all.

## Unmet Acceptance Criterion

**AC-00.5 — a measured ad-block rate for the browser-side path — was not met.** Filter-list rules were
established per domain and per list; no percentage is claimed, because none was measured, and none can
be measured from this environment.

The measurement that would close it is named in `findings.md`: ADR-037 dual-sources the Epic-5123
funnel, so the gap between the Supabase `responses` count and the Plausible event count over the same
window is a real block rate for this exact audience. It needs a Plausible dashboard read (or a
Business-tier Stats API key) and a Supabase query — neither reachable from here.

This does not block the decision. Both hosted candidates lose on the server-side path regardless, and
the recommended collector is not ad-blocked at all.

## Handoff

**To DESIGN (`nw-solution-architect`)** — the collector and the emit direction are now answered, which
closes handoff questions 1 and 3 from the DISCUSS delta. The three that remain open are unchanged:
where the consent record lives and how the emit path consults it without staleness (D8); how the master
switch reaches the emitter without a per-emit database read (D6); and how the instance identifier is
minted and persisted (S12).
