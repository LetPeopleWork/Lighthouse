# SPIKE Decisions — epic-5733-opt-in-usage-data

SPIKE-00 · ADO #5833 · Probe run 2026-08-22 · Decision taken 2026-08-22, after the probe closed.

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

That verdict stands unchanged and is what `findings.md` records.

## Decision

| | |
|---|---|
| **Collector** | **PostHog Cloud EU** — hosted, Frankfurt residency, free at our volume. |
| **Direction** | **Server-side.** The .NET backend posts to the capture API; the browser never contacts the collector. |

Rejected: **Plausible**, on the probe verdict above. **A vendor-run minimal collector** on the
existing Supabase project — this was the spike's own recommendation and it lost once the PostHog
data-protection gap was closed; it remains the fallback if the DPA read at DoR-9 goes badly.

### How the decision came to differ from the spike's recommendation

The probe ranked PostHog second for one reason: its data-protection position could not be
established. posthog.com is unreachable from this environment — `curl` fails with *"no alternative
certificate subject name matches target hostname 'posthog.com'"*, reproducibly, while
`raw.githubusercontent.com` resolves normally. So the probe recorded the gap rather than guessing at
it, and ranked accordingly.

The gap was closed afterwards from PostHog's own repository, which is where their website — including
the sub-processor register — is version-controlled. `PostHog/posthog.com:src/data/subprocessors.json`
and `contents/docs/privacy/data-storage.mdx` answer it directly:

- **At rest:** *"PostHog Cloud EU … hosted on servers based in Frankfurt"*.
- **EU-cloud sub-processors:** Amazon Web Services (Germany), PlanetScale (Germany), Modal Labs
  (Germany), Wiz (Germany, France). Each entry names Germany explicitly for the EU cloud.
- **In transit:** Cloudflare, as *"Global edge locations (dynamic, worldwide) for data in transit"*.
- **AI sub-processors:** OpenAI, Google, Anthropic, Microsoft — US-located, and each scoped *"for so
  long as AI Features are enabled"*.

## Corrections to the probe's own record

Two statements in `findings.md` were wrong or overstated. Both are annotated in place there; both are
recorded here because they are the reason the decision moved.

1. **The GeoIP inference was backwards.** `findings.md` guessed that discarding the client IP would
   also stop GeoIP enrichment, labelled INFERENCE. PostHog's documentation says the opposite:
   *"Transformations like GeoIP enrichment and bot detection can still use the IP before it is
   discarded."* Suppressing both takes two independent settings, not one.
2. **The transfer objection was leaned on harder than the evidence supported.** "US-domiciled,
   transfer position unestablished" was true as a statement about what had been retrieved, but it was
   used as though it implied a transfer problem. Data rests in Frankfurt and the EU-cloud
   sub-processors are EU-located. The only worldwide element is Cloudflare edge for **data in
   transit** — and a self-operated Supabase endpoint sits behind a CDN too, so it never
   differentiated the two options in the first place.

## Promotion Decision

**DISCARD** — decided at the gate on 2026-08-22, and also what the slice brief mandates: *"OUT of
scope: any production code. Nothing from this SPIKE is merged."*

`findings.md` plus this document are the deliverable. No walking skeleton is built here, because slice
01 (#5834) **is** the walking skeleton and is a separate story — one that additionally may not ship
until DoR-9 closes. Probe artifacts were scratch-only and are discarded.

## Design Implications

1. **Emit is server-side** to PostHog's capture API, fire-and-forget, once per day, degrading
   silently (AC-04.5). The API is identity-based rather than URL-based, so it needs no browser
   context and no synthetic URL — which is precisely what Plausible could not offer.
2. **The instance identifier becomes the `distinct_id`.** It stays ours to define: a random opaque
   value minted on first consent, derived from nothing (AC-04.4). No vendor identity derivation is in
   play — unlike Plausible's daily IP+UA hash.
3. **The privacy guarantee is project configuration, not code, and that is the cost of this choice.**
   Two independent PostHog project settings carry it: *Discard client IP data*, and disabling the
   GeoIP enrichment transformation. Neither is assertable by a test in this repository.
   `OUT-usagedata-payload-purity` is specified as a CI gate and the delta's standard is *"enforced as
   an invariant, not a review habit"* — this is the one place that standard cannot be met directly.
   DESIGN must name a compensating control: at minimum the settings are documented with their
   expected state, and re-verified as part of the release checklist rather than trusted.
4. **AI features must remain off on the project.** Four US sub-processors attach while they are
   enabled, and enabling them would silently widen the transfer position that DoR-9 signs off.
5. **The consent dialog must name PostHog** (AC-02.3 requires naming the collector and its operator),
   and `docs/settings/usagedata.md` should carry the residency and the sub-processor position rather
   than a bare vendor name. A user consenting to "we send five fields" is also consenting to who
   holds them.
6. **The air-gap story is heavier than it would have been, and must be documented honestly.** The
   host is configurable, so #5015's requirement is met in form — but the wire format is PostHog's
   capture API, so the substitute endpoint is either PostHog's open-source Docker Compose deployment
   or something that speaks the same protocol. PostHog sunset its Helm/Kubernetes deployment; only
   Docker Compose remains maintained. Do not describe this as "point it at any URL".
7. **Cost is zero at our volume and should be re-checked at slice 04**, not now. The free allowance is
   1M events/month; a daily heartbeat from 1,000 instances is roughly 30k. Slice 04's product events
   are what could move that.
8. **The deployment-mode value set is a payload-contract decision**, not an implementation detail —
   see Constraints.
9. **DESIGN returns a named adapter.** It may put a port in front of it; it may not leave the adapter
   unnamed. ADR-037 did exactly that — *"the port makes that deferral safe"* — and the vendor was
   never chosen, which is the reference class this SPIKE existed to avoid.

## Constraints Discovered

- **The privacy configuration is runtime state, not code.** Nothing in CI can prove the IP is being
  discarded or that GeoIP is off. This is new with the PostHog decision and did not exist under the
  self-operated option.
- **Deployment mode cannot be emitted as the payload describes it.** `PlatformService` reports Docker
  for a Kubernetes pod: `IsDocker()` keys on `DOTNET_RUNNING_IN_CONTAINER`, `/.dockerenv` and
  `LIGHTHOUSE_DOCKER`, all true inside a pod, and `KUBERNETES_SERVICE_HOST` appears nowhere in the
  repository. AC-02.1 has the dialog enumerate the payload by name, so the value set is part of what
  the user consents to — widening it later is a re-consent question under AC-08.5.
- **No outbound chokepoint exists.** `GitHubService` builds its own `GitHubClient` rather than using
  `IHttpClientFactory`, and only three named clients are registered.
  `OUT-usagedata-zero-leak-before-consent` must assert an *absence* of traffic with no single seam to
  observe.
- **The emitter's registration prices a CI cycle.** A hosted service and a fourth `AddHttpClient` both
  edit `Program.cs`, which forces the full backend Integration suite — live connector tests and their
  flake exposure.
- Any future reconsideration of Plausible for this channel starts from the datacenter-IP drop, not
  from the tier price.

## Open Items Carried Forward

- **DoR-9 must read the DPA terms.** Residency and the sub-processor register are established; the
  agreement text is not, and it is not in PostHog's open-source repository. If that read goes badly,
  the fallback is the self-operated Supabase collector, whose case is written up in `findings.md` and
  loses nothing by waiting.
- **AC-00.5 is not met.** No ad-block rate was measured and none is invented. The measurement that
  would close it is named in `findings.md`: the gap between the Supabase `responses` count and the
  Plausible event count over the same window, from the Epic-5123 funnel. It does not block this
  decision — server-side emit is not ad-blocked — but it is the only real number this project could
  have about its own audience.

## Handoff

**To DESIGN (`nw-solution-architect`)** — the collector and the emit direction are answered, which
closes handoff questions 1 and 3 from the DISCUSS delta. The three that remain open are unchanged:
where the consent record lives and how the emit path consults it without staleness (D8); how the
master switch reaches the emitter without a per-emit database read (D6); and how the instance
identifier is minted and persisted (S12). Design implication 3 above adds a fourth: the compensating
control for a privacy guarantee that lives in vendor configuration.
