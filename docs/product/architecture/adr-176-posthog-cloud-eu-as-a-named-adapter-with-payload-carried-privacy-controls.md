# ADR-176: PostHog Cloud EU behind a named publisher adapter, with the privacy guarantee moved into the payload where CI can assert it, and a canary that reads the vendor back

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slices 01 and 04)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

The collector and the emit direction were settled after SPIKE-00: **PostHog Cloud EU, server-side**.
The backend posts to the capture API; the browser never contacts the collector. Plausible was
disproved on its own published source - `props` is Business-tier, and its Events API silently drops
events forwarding a datacenter IP, returning HTTP 202 and recording nothing. That is not reopened
here.

This ADR exists to answer the question the SPIKE handed forward as new, and to avoid repeating a
failure this project has already had once.

**The failure to avoid.** [ADR-037](./adr-037-analytics-funnel-events.md) named a port, deferred the
vendor, and wrote *"the precise analytics vendor is a DELIVER detail (flagged) - the port makes that
deferral safe."* It was not safe. The ADR named no adapter, and the vendor was chosen at the keyboard.
This ADR therefore names the adapter. A port is still present, because the endpoint must be
redirectable for an air-gapped customer, but the port does not stand in for a decision.

**The question that is new.** With a hosted vendor, "we do not store your IP" stops being a statement
about our code. It becomes two PostHog project settings - *Discard client IP data*, and disabling the
GeoIP enrichment transformation - and the SPIKE established these are independent: PostHog's own
documentation says transformations "can still use the IP before it is discarded", so switching one off
does not switch the other off. A third setting matters too: with AI features enabled, four US
sub-processors attach, which would silently widen the transfer position the legal review signs off.

Nothing in this repository's CI can assert any of the three. The outcome
`OUT-usagedata-payload-purity` is specified as a hard CI gate and the delta's standard is *"enforced
as an invariant, not a review habit"*. This is the one place that standard cannot be met directly,
and the SPIKE was explicit that DESIGN must name a compensating control rather than wave at it.

## Decision

**`PostHogUsageDataPublisher` is the adapter.** Behind `IUsageDataPublisher`, posting to
`{CollectorBaseUrl}/i/v0/e/`, with `CollectorBaseUrl` defaulting to `https://eu.i.posthog.com` and
overridable in configuration, and a project API key in `UsageData:ProjectApiKey`. Registered as a
fourth named `AddHttpClient`. Fire-and-forget, once per day, degrading silently with at most one log
line per attempt.

The compensating control is **four layers, in descending order of how much they are worth**. The
first is the one that actually meets the standard; the rest exist because the first cannot cover
everything.

### Layer 1 - move the guarantee into the payload, where our own CI can assert it

Every emitted event carries `$ip: null` and `$geoip_disable: true` as event properties, in addition to
the five consented fields. If PostHog honours these per-event, then the control stops being a setting
in a vendor's web UI that nobody in this repository can see, and becomes **a property of the bytes we
send** - which a unit test asserts against the serialised body, in CI, on every run. That is the
difference between an invariant and a review habit, and it is the whole reason to prefer this layer.

**This was written as conditional and unverified, and has since been verified.** `posthog.com` is
unreachable from the environment this design was produced in - `curl` fails on certificate subject
mismatch, reproducibly, which is the same wall the SPIKE hit and the reason its data-protection gap
had to be closed from PostHog's GitHub repository instead. The same route answers this question, and
answers it in the affirmative. `PostHog/posthog:nodejs/src/cdp/templates/_transformations/geoip/geoip.template.ts`
begins its transformation with:

```
if (event.properties?.$geoip_disable or empty(event.properties?.$ip)) {
    print('geoip disabled or no ip.')
    return event
}
```

Both switches are read from the event payload, and either one alone returns the event unenriched -
`$ip: null` satisfies the `empty()` branch. Layer 1 therefore rests on the vendor's published code,
not on a hoped-for behaviour, and the CI assertion over our own outbound payload is a genuine
invariant.

A second layer holds without any action on our part: PostHog's data-collection documentation states
that EU organizations "automatically default to IP data capture disabled for GDPR compliance", so the
safe state is the default rather than a setting somebody has to remember.

Two limits, stated rather than glossed. This is the `master` branch of the transformation template,
which establishes the mechanism and the branch logic but not that Cloud EU runs that exact build. And
it speaks only to enrichment - it does not establish whether the raw IP is observed upstream of the
transformation pipeline, only that it is not used for geo-enrichment and, with IP capture off, not
stored. Layer 2's canary therefore stays in the design as behavioural confirmation, but it is no
longer the gate that has to pass before slice 01 may be written.

### Layer 2 - a scheduled canary that reads the vendor back

A CI job, on a schedule rather than per-commit, emits one event with a reserved `distinct_id` and
then reads that same event back through PostHog's query API, asserting on the **stored** record:

- no `$ip` property is present,
- no `$geoip_*` properties are present,
- the project's AI features are off.

**It must also emit a second, control event that deliberately omits `$geoip_disable`, and assert the
geo properties **do** appear on that one.** Without a positive control the whole layer can pass
vacuously: if PostHog's query API stops projecting `$ip` on read, or never projected it, an
assertion of the form "property X is absent" is green forever, including on the day the guarantee
breaks. A light wired to be green is worse than no light. The control event proves the assertion is
capable of failing.

Two further practicalities, neither of which the first draft accounted for. Capture-to-query
availability at PostHog is asynchronous, on the order of minutes, so a read-back immediately after
emit will flake - and a flaky scheduled job gets muted, which silently deletes this layer. The
read-back needs a bounded poll. And **"AI features are off" is probably not answerable through the
query API at all** - it is a project-settings read needing different scope - so it is demoted to
layer 3 unless someone confirms otherwise.

The schedule is **daily**. An unset interval in a compensating control is a control that has not been
designed; the interval *is* the exposure window.

This converts an unverifiable vendor claim into a behavioural assertion. If somebody re-enables GeoIP
in the PostHog UI, or a PostHog default changes under us, the job goes red and names what drifted. It is a **detective** control, not a preventive one: it can only tell us the vendor
stopped honouring the arrangement *after* at least one event was enriched. That residual is bounded by
using a reserved canary identifier so the detecting event is synthetic, and by the schedule interval,
which is the exposure window and should be set deliberately.

It needs a PostHog personal API key in CI secrets and egress from the runner. Both are new.

### Layer 3 - the expected state is written down and re-verified at release

The three settings and their required values are recorded in `docs/settings/usagedata.md` alongside
the field list, and re-checked as part of the release checklist. **This layer is the weakest and is
listed last on purpose** - a checklist is exactly the "review habit" the delta rejects. It is here
because layers 1 and 2 cannot cover a setting that has no observable effect on a single event, and
because a documented expected state is what makes layer 2's failure actionable rather than merely red.

### Layer 4 - the dialog may not claim more than the layers can demonstrate

This is the layer that makes the other three honest, and it is enforceable today with a pattern this
codebase already ships. `ArchiveConfirmationDialog.test.tsx` asserts over rendered dialog copy that a
set of forbidden words never appears, precisely so a promise that must never be made cannot drift back
in. The consent dialog gets the same treatment.

What the dialog says is what is true:

- the five fields, by name;
- that no work item titles, queries, names, URLs, email addresses or free text are ever sent;
- that the collector is PostHog Cloud EU, operated by PostHog Inc., with data resting in Frankfurt;
- that **the connection itself reveals this instance's network address to the collector, as any HTTP
  request does**, and that Lighthouse asks PostHog to discard it rather than store it.

The last of those is the sentence this ADR insists on. "Exactly five fields" is true about the payload
and invites a reader to conclude that five fields are all the processor receives. For an audience of
engineers that is a distinction they will notice, and a privacy dialog that gets caught shading it has
spent the credibility the whole feature runs on. **The forbidden-phrase test asserts the dialog never
states the IP is not transmitted, not seen, or never leaves the machine** - claims that are false at
the network layer regardless of what any layer above achieves.

## Alternatives considered

- **Documented settings plus a release-checklist re-verification, as the whole control.** This is what
  the SPIKE named as the *minimum*. **Rejected as the sole control** - it is a review habit by
  definition, and it is the standard the delta explicitly refuses for this Epic's invariants. It
  survives as layer 3 because it is genuinely the only thing that reaches the AI-features setting.

- **Self-operate the collector** (a Supabase Edge Function on the project that already backs the lead
  funnel), which was the SPIKE's own first recommendation. Its whole case was this exact problem: with
  an endpoint we run, "the IP is not persisted" is a line of our code and a test we own. **Not chosen
  here** because the product owner ratified PostHog after the data-protection gap was closed, and that
  is a settled decision. It is recorded because it remains the fallback if the legal review of the DPA
  terms goes badly, and because it is the only option where layer 1 is unconditional rather than
  vendor-dependent.

- **Strip nothing and rely on PostHog's EU residency alone.** **Rejected** - residency answers where
  data rests, not what is collected. An IP and a derived geo-location are personal data in Frankfurt
  just as much as anywhere else, and the dialog is about to promise something specific.

- **A consumer-driven contract test against the PostHog capture API** (Pact or similar). **Rejected
  for the transport, adopted in spirit for layer 2.** ADR-037 declined a contract test on the grounds
  that a fire-and-forget beacon is not something the app depends on for correctness, and for the
  *transport* that reasoning still holds - a failed emit is defined as silent. But it does not extend
  to the *privacy behaviour*, which is a claim we make to users about a third party. Layer 2 is the
  narrower thing that is actually needed: not "does the API still accept our shape", but "does the
  processor still discard what it said it discards".

- **Emit browser-side so the customer's server IP is never exposed.** **Rejected** - settled by the
  SPIKE. The browser path is blocked at whole-domain scope by AdGuard and at ingest-path scope by
  EasyPrivacy, which uBlock Origin enables by default, for an audience of engineers.

## Consequences

**Positive**

- A named adapter, so the ADR-037 failure does not repeat.
- The strongest available form of the privacy control is in-repo and asserted per commit, if layer 1
  verifies.
- Drift in vendor configuration becomes a red build with a named cause, rather than something nobody
  finds out about.
- The dialog's copy is bound by a test to what the controls actually deliver, so the two cannot drift
  apart silently - which is the failure mode that would cost the most.

**Negative / accepted**

- **Layer 1 is unverified at design time and may not survive contact with the live API.** Named as a
  blocking DELIVER verification item rather than assumed.
- Layer 2 is detective, so a bounded number of real events can be enriched before drift is caught.
- Layer 2 adds a scheduled CI job, a vendor API key in secrets, and an egress dependency - new
  operational surface for a privacy assurance, which is the right trade but is not free.
- The air-gap story is heavier than a bare URL swap: the wire format is PostHog's capture API, so a
  substitute endpoint must speak it. PostHog sunset its Helm deployment and only Docker Compose
  remains maintained. The docs must say this rather than "point it at any URL".
- Cost is zero at the projected volume (~30k events/month against a 1M free allowance) and should be
  re-checked at slice 04, not now.

**External integration note (handoff to `nw-platform-architect`)**: PostHog Cloud EU is an external
integration. A consumer-driven contract test on the capture transport is **not** recommended - the
emit is fire-and-forget and degrades silently by specification. What *is* required is layer 2's
scheduled privacy-behaviour canary, which is a different thing wearing similar clothes: it asserts the
processor's handling of data rather than the API's schema. It needs a secret, an egress allowance and
a schedule.

**Reuse verdict**: `IUsageDataPublisher` -> **CREATE NEW** (no outbound vendor-telemetry port exists).
`PostHogUsageDataPublisher` -> **CREATE NEW**, and **named**, which is the point of this ADR.
`AddHttpClient` -> **EXTEND**, a fourth named client. `GitHubService` -> **UNCHANGED**; it is the
other outbound call, documented rather than altered, and named in the docs page as a separate
pre-existing call this consent does not cover. `TelemetryConfiguration` / `TelemetryConfigurator` ->
**UNCHANGED** and namespace-disjoint: those names are the OpenTelemetry-to-Prometheus scrape and keep
their meaning; everything here is `UsageData*`.
`ArchiveConfirmationDialog.test.tsx` -> **UNCHANGED**, its forbidden-word technique copied for layer 4.

**Enforcement**

| Rule | Mechanism |
|---|---|
| Every event carries the IP and GeoIP suppression properties | NUnit over the serialised request body, per event type |
| The payload carries the five consented fields and nothing else | NUnit: assert the serialised property set equals the declared set exactly - not a subset check |
| No event may carry free text or customer content | ArchUnitNET: the event type's property set is closed and primitive; plus NUnit over every declared event |
| The stored record at the vendor has no IP and no geo enrichment | Scheduled CI canary reading back through the PostHog query API (layer 2) |
| AI features are off on the project | Same canary job |
| The dialog never claims the IP is not transmitted | Vitest forbidden-phrase test over rendered copy (layer 4), in the shape of `ArchiveConfirmationDialog.test.tsx` |
| No product surface claims Lighthouse does not track usage | Vitest forbidden-phrase test across `SurveyNudge` copy, plus a docs check on the compliance self-assessment row |
| An event that ships ahead of its documentation is a defect | CI: the declared event/field set is compared against the list in `docs/settings/usagedata.md` |

Cross-refs [ADR-037](./adr-037-analytics-funnel-events.md) (the unnamed-adapter failure this ADR
exists not to repeat), [ADR-174](./adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md)
(the permit this publisher requires),
[ADR-177](./adr-177-deployment-mode-is-a-usage-data-owned-closed-value-set.md) (one of the five fields,
whose value set is a payload-contract decision).
