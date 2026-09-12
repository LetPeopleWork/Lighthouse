# Feature Delta — epic-5733-opt-in-usage-data

ADO Epic #5733 "Opt-In Telemtry" (New, Priority 2, tag `Community`, forecasted delivery 2026-09-08).
No children yet. Related: #5015 "Opt-in Product Telemetry" (Epic, state `Removed` — superseded by this
one, but its non-negotiables are inherited verbatim). Successor link: #5511 Task Manager (no bearing).

Wave DISCUSS run 2026-08-22. No DISCOVER or DIVERGE artifacts existed — this is a cold DISCUSS
grounded in an ADO read, a code reality check (see Current-State Surface Inventory) and a live
decision session with the product owner.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this Epic |
|---|---|
| `lighthouse-maintainer` | Primary beneficiary. Ships features and today cannot tell whether any of them landed. Never touches the UI this Epic builds. |
| `privacy-decider` | Primary actor. Any human using Lighthouse, in the moment they are asked whether the product may learn from them. New persona — see SSOT Updates. |
| `config-admin` | Primary actor for the veto. Owns the instance-wide switch and has to answer a security review about it. |
| `community-respondent` | Secondary. Already carries the recorded frustrations this Epic must not repeat — see S3, S7. |
| `platform-operator` | Not involved. This is not the Prometheus surface (D1). |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-maintainer-know-if-a-shipped-feature-landed` | When I have shipped a feature, I want to see whether anyone turned it on and kept using it, so I can decide to invest further, fix it, or retire it instead of guessing from three support tickets. |
| `job-maintainer-see-the-installed-base` | When I am planning a breaking change or dropping support for something, I want to know how many instances exist and which versions they run, so I can pick a date that does not strand people. |
| `job-user-decide-once-whether-lighthouse-may-learn-from-me` | When Lighthouse asks to send usage data, I want to see exactly what would leave my machine and decide once, so I can help improve the tool without feeling watched — and change my mind later without hunting for the setting. |
| `job-admin-stop-lighthouse-asking-my-people` | When my organisation's policy forbids product analytics, I want one switch that stops the asking for everyone and guarantees nothing can be sent, so I can close the security review with a screenshot instead of a discussion. |

Full JTBD narrative (dimensions, four forces, opportunity scores) lives in `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Established by reading the code before writing requirements. Every decision below rests on these.

| # | Fact | Evidence |
|---|---|---|
| S1 | **"Telemetry" is already taken.** `TelemetryConfiguration.Enabled` wires OpenTelemetry to a Prometheus scrape endpoint — operator-facing observability, off by default, nothing leaves the cluster. The architecture brief and ADR-090 build on that meaning. | `Configuration/TelemetryConfiguration.cs`, `Startup/TelemetryConfigurator.cs`, `docs/product/architecture/brief.md:2915` |
| S2 | **Lighthouse already phones home, unconsented.** Every instance calls GitHub for the release check, so GitHub sees the customer's server IP and User-Agent. Nobody has ever documented this. | `Services/Implementation/GitHubService.cs:20`, `LighthouseReleaseService.cs:48` |
| S3 | The product ships the sentence **"Lighthouse never tracks how you use it, so your feedback is the only way we learn what to improve"** to every Community user, in a popup. | `components/SurveyNudge/SurveyNudge.tsx:114` |
| S4 | The CRA self-assessment claims **"Minimal data collection, no telemetry"** as a conformance statement against requirement 1.7. | `docs/compliance/cra-self-assessment.md:35` |
| S5 | Seven outcome KPIs sit at `status: deferred-pending-telemetry-feature`, each naming Epic #5015 as `blocked_by`. The KPI-contract preamble states as fact that no phone-home exists. | `docs/product/kpi-contracts.yaml:8,116,183,209,247,262` |
| S6 | **No per-browser machinery exists.** `SurveyNudge` state lives in `AppSettings` — one row for the whole instance. The first person to dismiss it dismisses it for everybody. | `AppSettingKeys.SurveyNudgeNextEligibleAt`, `AppSettingService.cs:113` |
| S7 | SurveyNudge cadence: Community-only, 14-day minimum install age, 6-month quiet cadence, 7-day remind-later capped at 2 repeats. | `nudgeEligibility.ts:26,36`, `AppSettingService.cs:20-24` |
| S8 | `OptionalFeature` already carries `IsPremium`. The gate enforces it by **silently returning the unchanged feature** — no error, no signal, the write is dropped and the caller cannot tell. | `Models/OptionalFeatures/OptionalFeature.cs`, `API/OptionalFeaturesController.cs:41` |
| S9 | `OptionalFeatureSeeder` refreshes Name / Description / IsPreview / IsPremium on every upgrade but **never** `Enabled` — "whether it is on is the operator's". A brand-new key seeds with its declared `Enabled`. | `Services/Implementation/Seeding/OptionalFeatureSeeder.cs` |
| S10 | Exactly one live optional feature (`DeltaSync`, preview, off). The toggle is `RbacGuard(SystemAdmin)`. | `OptionalFeatureKeys.cs`, `OptionalFeaturesController.cs:36` |
| S11 | **There is no user identity in a large part of the installed base.** Auth is optional, and standalone can never have it. With auth off, `DisabledAuthenticationHandler` hands *every* caller the same subject `lighthouse\|auth-disabled`. | `Auth/DisabledAuthenticationHandler.cs:15`, `Auth/AuthModeResolver.cs:59` |
| S12 | Everything a heartbeat needs already exists — version (`GetCurrentVersion()`), standalone/platform (`PlatformService`), licence tier (`CanUsePremiumFeatures()`), install timestamp. **No instance identifier exists.** | `LighthouseReleaseService.cs:22`, `PlatformService.cs:10,47`, `AppSettingKeys.InstallTimestamp` |
| S13 | The footer already renders an icon row with tooltips (`ExternalLinkButton`) beside `LighthouseVersion` — the exact affordance the indicator needs, already styled. | `components/App/Footer/Footer.tsx:53-82` |
| S14 | No configurable Terminology key covers anything this Epic names. `feature`, `workItem`, `team`, `portfolio`, `delivery` are configurable; nothing here is. | `Services/Implementation/Seeding/TerminologySeeder.cs` |

∴ **S11 is why "per user" cannot mean "per user account".** In standalone and every auth-off server
there is one shared subject, so account-scoped consent would collapse to one decision the first
person makes for everyone. The browser is the only unit of consent that behaves the same in all
deployment shapes.

∴ **S3 + S4 are a shipping blocker, not a follow-up.** The day this Epic's first slice ships, the
product tells users it never tracks them while an indicator in the footer says it does. Both surfaces
are corrected inside slice 01b — the slice that first makes them false — or slice 01b does not ship.

∴ **S6 means there is nothing to reuse.** The consent record is new machinery. The *cadence
arithmetic* in `AppSettingService` is a model to copy, not a component to share.

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — This is called Usage Data. "Telemetry" keeps its existing meaning

The word `Telemetry` in this codebase means OpenTelemetry metrics scraped by the customer's own
Prometheus (S1) — self-hosted, operator-facing, nothing leaving the cluster. This Epic builds the
opposite: data leaving the customer's instance for the vendor. Two opposite meanings under one word,
one of them in a settings tree an admin reads during a security review, is a defect waiting to be
filed.

User-facing name: **Usage Data**. Keys, endpoints and the OptionalFeature use `UsageData`. The Epic
title stays as written in ADO; the product does not.

### D2 — Consent is per browser, recorded by an opaque token, never by a fingerprint

The consent unit is the browser, because S11 leaves no alternative that works everywhere. The
mechanism is **not** a device fingerprint. On the click, the backend mints a random opaque token,
returns it, and the browser stores it. Nothing is read from or written to the browser before that
click.

This distinction is load-bearing, not pedantic. Fingerprinting (canvas, fonts, screen entropy) is
"gaining access to information stored in terminal equipment" under ePrivacy Article 5(3) as read by
EDPB Guidelines 2/2023 — it requires consent *before* it may happen, so a fingerprint used to record
consent needs consent to exist. Unfixable circularity. A token written only after the click, whose
sole content is the user's own choice, sits in the strictly-necessary exemption — the same reasoning
that lets a cookie banner remember "declined". A declined browser therefore also gets a token, for
the same reason and under the same exemption.

Legal sign-off on this reading is a DoR item (DoR-9), not something DISCUSS closes.

### D3 — Nothing is sent until a browser consents, including the heartbeat

The instance heartbeat has no user — no browser generates "which version am I". Rather than invent a
second, admin-level consent object beside the per-browser one, the heartbeat emits **only while at
least one browser on the instance holds live consent**, and carries the same instance identifier
every other event carries.

The consequence is stated in the docs rather than hidden: the installed-base census counts *instances
with at least one consenting user*, not instances. A biased denominator that is named is usable; an
unnamed one is not.

The instance identifier is minted lazily on first consent (S12 — none exists today), so an instance
that never consents never acquires one.

### D4 — The admin switch is an OptionalFeature, premium, and defaults to ON

`UsageData` becomes an `OptionalFeature` with `IsPremium = true` and `Enabled = true`. It governs one
thing: **may Lighthouse ask?** It is not itself the consent.

Default ON is deliberate. It does not violate the Epic's non-negotiable — asking is not sending — and
an opt-in channel nobody is ever invited to join produces no denominator, which is the whole point of
the Epic. S9 gives this for free: a new key seeds with its declared `Enabled` on upgrade, and is never
overwritten afterwards.

Premium is the product owner's commercial call, made with the trade named (see D5). It requires S8 to
be fixed first: a silently-dropped write is tolerable on a performance flag and is not tolerable on a
privacy control.

**S8 is no longer fixed here.** Story #5876 makes ordering ownership the first premium optional
feature and so reaches the broken branch before this Epic does; the fix moved there on 2026-08-31 and
is a **precondition** of this slice, not part of it. See
`docs/feature/story-5876-behaviour-settings/feature-delta.md` D6. If #5876 has not shipped when this
Epic unblocks, the fix comes back here — it may not simply be skipped.

### D5 — Community is re-asked every three months and cannot switch the asking off

Falls directly out of D4. A Community admin has no way to stop the prompt; a Premium admin does. A
Community user who declines is asked again in ~3 months; a Premium user who declines is never asked
again.

What makes this survivable is that the copy says so. The dialog tells a Community user "we will ask
again in a few months" and tells a Premium user "we will not ask again". A "we won't ask again" that
reappears in 90 days is a broken promise, and a broken promise on a privacy dialog costs more than
the nag it was trying to avoid.

### D6 — The admin switch suspends consent; it never revokes it

Master OFF stops all sending, including for browsers that already consented, and hides the prompt.
Master back ON resumes sending for those browsers silently — their consent was overridden by an
administrator, never withdrawn by them.

Enforced at the emit path, not by hiding the UI. A stale browser tab must not be able to keep sending
across a master-switch flip.

Documented in plain words on the settings page, because "my consent came back without me" is the
reading this earns if it is left implicit.

### D7 — The indicator is permanent chrome in the footer

A small icon beside the version, in the footer's existing icon row (S13): different appearance for
on and off, a tooltip stating which, and a click that reopens the decision. It is the answer to
"how do I change my mind", which is why the dialog can promise revocation without pointing at
Settings.

It is quiet by design — an icon, not a coloured badge. Permanent, because a control that only appears
when something is being sent tells you nothing in the state you most want reassurance about.

### D8 — Revocation takes effect immediately, server-side

One click on the indicator. The next emit does not happen — not the next day, not after a restart.
Inherited verbatim from #5015's non-negotiables.

### D9 — Coordinate with the survey nudge; never bundle with it

Two prompts, two purposes, two legal bases. One combined dialog fails GDPR Article 7(4) — consent
bundled with an unrelated ask is not freely given.

They must never fire in the same session, and both are Community-relevant (S7), so collision is
likely rather than theoretical. They share the install-age idea and the cadence arithmetic; they do
not share state (S6).

### D10 — The collector is not chosen here

The Epic asks "Upgrade to Plausible Growth Plan for this?". DISCUSS declines to answer. Plausible is a
web-analytics product keyed on domains and pageviews; its server-side Events API wants a URL and a
User-Agent and performs IP-based geolocation and de-duplication, which for a server-emitted phone-home
means the vendor's processor sees customer server IPs. Whether custom properties are available on
Growth rather than Business is a plan-tier question with a real architectural consequence.

SPIKE-00 probes Plausible Growth, PostHog Cloud EU, and a vendor-run minimal collector against the
slice-01 payload, and DESIGN decides. #5015's "self-hostable telemetry endpoint" requirement — an
air-gapped customer may point the channel at their own collector — is an input to that probe, not a
separate decision.

### D11 — The GitHub release check is documented, not changed

S2 is a pre-existing unconsented outbound call. It is out of scope to alter, and dishonest to leave
unmentioned on a page that enumerates everything the instance sends. The Usage Data docs page names
it, says what GitHub sees, and says it is not part of Usage Data and is not covered by this consent.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized, split already applied.** Against the oversized heuristics:

- User stories: 8 (threshold >10) — under.
- Bounded contexts: 3 — Usage Data (new), Optional Features / Licensing (existing), and the public
  documentation + compliance surface. **Signal fires.**
- Walking skeleton integration points: 1 new external system (the collector) — under 5.
- Effort: 1 SPIKE + 4 slices at ≤1 day each — under 2 weeks.
- Independent user outcomes that could ship separately: 3 — the consent control, the admin veto, and
  the maintainer's answers. **Signal fires.**

Two signals fire, which is the threshold. The answer is the slicing rather than a split of the Epic:
each of the three outcomes ships on its own day, and the two later ones are worthless without slice
01's skeleton, so they are not independently releasable in the sense the heuristic means. The
genuinely separable part — the OptionalFeatures rework and the manual-ordering migration the Epic
muses about — **is** split out, to the board, and is not in this Epic (see Out of Scope).

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy A — walking skeleton first.** Unlike most brownfield work here, this Epic has an unproven
end-to-end path: nothing in Lighthouse has ever sent a product event to a vendor collector, no consent
record exists (S6), no instance identifier exists (S12), and the collector itself is undecided (D10).

Slice 01 is that skeleton and nothing more: one browser consents, one heartbeat leaves, one maintainer
sees it arrive, one click stops it. Every subsequent slice thickens a path that already runs. SPIKE-00
precedes it because the skeleton cannot be built against an unchosen collector.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Kind | Slice |
|---|---|---|
| Footer → Usage Data indicator (icon + tooltip) | UI action | 01 |
| Usage Data dialog → "Yes, share usage data" / "No" | UI action | 01 |
| `GET /api/latest/usagedata/state` | HTTP (tokenless) | 01 |
| `POST /api/latest/usagedata/consent` | HTTP | 01 |
| `DELETE /api/latest/usagedata/consent` | HTTP (revoke) | 01 |
| `docs/settings/usagedata.md` — the complete event list | Documentation | 01 |
| Usage Data dialog, unprompted on install age | UI surface | 02 |
| Settings → Configuration → Optional Features → "Usage Data" | UI action (existing surface, new key) | 03 |
| `POST /api/latest/optionalfeatures/{id}` returning a refusal rather than a silent drop | HTTP (existing endpoint, corrected) | 03 |

**No CLI or MCP surface.** The Lighthouse-Clients CLI and MCP server expose Team and Portfolio
metrics. Nothing here changes a contract they consume, and neither is a browser, so neither can hold
consent or emit events.

**Marketing surface — yes.** The admin switch is a new premium capability (D4), so the pricing page
and feature comparison need a line. The website lives in its own repository; the copy change ships
there at feature finalization. It must also stop implying Lighthouse collects nothing, if it does.

**RBAC surface — no new permission.** The OptionalFeature toggle reuses `RbacGuard(SystemAdmin)`
(S10). Consent is deliberately *not* RBAC-gated: it belongs to the human at the browser, not to a
role, and on an auth-off instance there is no role to check (S11).

---

## Wave: DISCUSS / [REF] Pre-requisites

- **SPIKE-00 must complete before slice 01 starts.** The skeleton cannot be built against an unchosen
  collector (D10).
- A vendor collector account and, if the probe picks a hosted processor, a signed DPA naming it. This
  is a procurement dependency with a lead time, not an engineering task.
- Legal / DPO review of the consent copy and the D2 reading of ePrivacy Art 5(3) — DoR-9.
- One EF migration per supported database provider for the consent record, generated via the existing
  `CreateMigration` script, additive-only.
- Egress: the customer instance must be able to reach the collector host. Documented as a
  prerequisite, and its absence must degrade to "consent held, nothing sent, no error spam" rather
  than to a crash or a log flood.
- Premium licence fixture for slice 03's tests and any `@screenshot` run covering the admin switch.

---

## Wave: DISCUSS / [REF] Out of Scope

- **The OptionalFeatures rework the Epic muses about** — making the premium gate a first-class
  concept, migrating "manual ordering" out of `FeatureOrderingSettings` into an OptionalFeature. That
  board item became story **#5876**, and on 2026-08-31 it also took the **S8 silent-no-op fix**, which
  this Epic had pulled in. #5876 creates the first premium optional feature, so it reaches the broken
  branch first. S8 is now a precondition of slice 03 rather than work inside it.
- **Changing the GitHub release check** (S2). Documented (D11), not altered.
- **Per-user-account consent.** Provably impossible on the auth-off installed base (S11).
- **Customer-visible usage analytics.** This Epic sends data to the vendor. An in-app dashboard where
  the customer sees their own usage is a different product and a different job.
- **Error and crash reporting.** A different consent conversation with a different payload risk
  (stack traces carry customer data). Candidate Epic.
- **Backfilling anything.** Forward-only, inherited from #5015.
- **A self-hosted collector deployment** the customer runs. The *endpoint is configurable* so an
  air-gapped customer can point elsewhere; shipping and supporting a collector image is not in scope.
- **Any event carrying customer content** — no work item titles, queries, team names, connection URLs,
  email addresses, or free text. Enforced as an invariant, not a review habit.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — See at a glance whether Lighthouse is sending anything

`job_id: job-user-decide-once-whether-lighthouse-may-learn-from-me` · Slice 01

As someone using Lighthouse, I want a permanent, honest indicator of whether usage data is leaving
this browser, so that I never have to take the product's word for it or go looking through Settings.

#### Elevator Pitch
Before: there is no way to know what Lighthouse sends anywhere. The only statement on the subject is a
popup claiming it never tracks you, which is about to stop being true.
After: look at the footer, beside the version → sees a small icon whose appearance differs by state,
whose tooltip reads `Usage data: not being sent` or `Usage data: being sent from this browser`.
Decision enabled: whether to leave it alone or click it and change the answer.

#### Acceptance Criteria
- AC-01.1 The indicator renders in the footer's existing icon row, beside the version, in the same
  visual language as the links already there.
- AC-01.2 Its appearance differs between the sending and not-sending states in a way that survives
  greyscale and does not rely on colour alone.
- AC-01.3 Its tooltip states the current state in words, not as a symbol to interpret.
- AC-01.4 It renders identically on standalone, auth-off server, and RBAC server — no deployment
  shape hides it (S11).
- AC-01.5 It renders in the not-sending state when the backend cannot be reached, never in the
  sending state. An unknown state is reported as not sending.
- AC-01.6 Clicking it opens the Usage Data dialog (US-02) whether or not a decision has been made.

---

### US-02 — Decide, with the full list in front of me, whether Lighthouse may learn from me

`job_id: job-user-decide-once-whether-lighthouse-may-learn-from-me` · Slice 01

As someone using Lighthouse, I want to see exactly what would leave my machine before I agree to
anything, so that I am consenting to a list rather than to a category.

#### Elevator Pitch
Before: no consent surface exists, and nothing in the product tells a user what an instance sends.
After: click the footer indicator → sees a dialog that names every field that would be sent, says
where it goes and who runs it, says what is never sent, links the full docs page, and offers exactly
two buttons — `Yes, share usage data` and `No`.
Decision enabled: whether to help improve Lighthouse, made on evidence rather than on trust.

#### Acceptance Criteria
- AC-02.1 The dialog enumerates every field in the payload by name — for slice 01: instance
  identifier, Lighthouse version, deployment mode, licence tier, timestamp. No "and other metadata".
- AC-02.2 It states, in the dialog itself, that no work item titles, queries, names, URLs, email
  addresses or free text are ever sent.
- AC-02.3 It names the collector and who operates it.
- AC-02.4 It links `docs/settings/usagedata.md`, which carries the same list and adds the GitHub
  release check as a separate, pre-existing outbound call that this consent does not cover (D11).
- AC-02.5 It offers two decisions, not one plus a dismiss. Closing the dialog without choosing leaves
  the state unchanged and is not recorded as either answer.
- AC-02.6 Nothing is written to the browser and nothing is transmitted until a button is pressed.
  Verified by asserting the browser storage is untouched after opening and closing the dialog (D2).
- AC-02.7 `Yes` mints an opaque random token server-side, returns it, stores it in this browser, and
  the indicator flips within the same interaction — no reload.
- AC-02.8 `No` records the refusal against a token minted the same way, so this browser is not asked
  again on the applicable cadence (D5).
- AC-02.9 The dialog tells a Community user it will ask again in a few months and tells a Premium
  user it will not ask again — matching what actually happens (D5).
- AC-02.10 **No surface in the product or its compliance documentation claims Lighthouse does not
  track usage.** `SurveyNudge.tsx:114` and `cra-self-assessment.md:35` are corrected in this slice
  (S3, S4).

---

### US-03 — Change my mind in one click, and have it stop immediately

`job_id: job-user-decide-once-whether-lighthouse-may-learn-from-me` · Slice 01

As someone who consented, I want to withdraw with one click and have transmission stop at once, so
that consent stays a decision I hold rather than one I made once.

#### Elevator Pitch
Before: nothing to withdraw, because nothing can be granted.
After: click the footer indicator, choose `No` → sees the indicator flip to not-sending, and the next
scheduled emit does not happen.
Decision enabled: whether to keep helping, revisited at any moment without cost or hunting.

#### Acceptance Criteria
- AC-03.1 Revocation is reachable in one click from any page, via the indicator (D7).
- AC-03.2 The next emit after a revocation does not happen — not on the next cycle, not after a
  backend restart. Asserted at the emit path, not at the UI (D8).
- AC-03.3 Revoking the last consenting browser on an instance stops the heartbeat too (D3).
- AC-03.4 A revoked browser can consent again later; the cadence does not lock it out.
- AC-03.5 Clearing browser storage is equivalent to never having decided: the browser stops being
  counted as consenting, and is eligible to be asked again.

---

### US-04 — Know how many instances exist and which versions they run

`job_id: job-maintainer-see-the-installed-base` · Slice 01

As the Lighthouse maintainer, I want a daily signal from consenting instances carrying version and
deployment shape, so that I can pick deprecation dates against the installed base instead of against
a guess.

#### Elevator Pitch
Before: the installed base is unknown. Version adoption is inferred from GitHub download counts and
from whoever happens to post in Slack.
After: open the collector dashboard → sees a count of distinct instance identifiers reporting in the
last 24 hours, broken down by Lighthouse version, deployment mode and licence tier.
Decision enabled: when it is safe to drop support for a version, and whether an upgrade actually
propagated.

#### Acceptance Criteria
- AC-04.1 A heartbeat is emitted at most once per day per instance while at least one browser holds
  live consent, and never otherwise (D3).
- AC-04.2 Its payload is exactly: instance identifier, Lighthouse version, deployment mode, licence
  tier, timestamp. Nothing else. Asserted against the emitted payload, not against the intent.
- AC-04.3 The instance identifier is a random value minted on first consent and persisted. An
  instance that has never had a consenting browser has no identifier at all (D3).
- AC-04.4 The identifier is stable across restarts and derived from nothing — not from hostname,
  licence key, database name, or any other pre-existing value.
- AC-04.5 A collector that is unreachable, slow, or returning errors causes no user-visible failure,
  no retry storm, and at most one log line per emit attempt.
- AC-04.6 The dashboard is reachable by the maintainer and shows the vendor's own dogfood instance
  reporting on the day slice 01b ships.
- AC-04.7 The docs state that the instance count means "instances with at least one consenting user"
  (D3).

---

### US-05 — Be asked once, at a sensible moment, and told the truth about when I will be asked again

`job_id: job-user-decide-once-whether-lighthouse-may-learn-from-me` · Slice 02

As someone using Lighthouse, I want the question to arrive after I have actually used the tool and
not to become a recurring nag, so that I answer it properly instead of reflexively dismissing it.

#### Elevator Pitch
Before: the dialog exists but only opens if someone happens to click a small footer icon, so almost
nobody is ever asked.
After: on an instance that has been installed a few days, the Usage Data dialog appears once,
unprompted → sees the same list and the same two buttons as US-02, plus an honest line about whether
it will return.
Decision enabled: the same decision as US-02, actually put in front of enough people to produce a
denominator.

#### Acceptance Criteria
- AC-05.1 The dialog does not appear before the instance's install age reaches the configured
  threshold (2–3 days), reusing the install timestamp that already exists (S7, S12).
- AC-05.2 It appears once per browser, not once per instance. Two people on the same instance are
  each asked (S6, S11).
- AC-05.3 There is no "remind me later". Yes and No are the only outcomes, and closing the dialog
  leaves the browser undecided and eligible on the next session.
- AC-05.4 A Premium browser that answered No is never asked again (D5).
- AC-05.5 A Community browser that answered No becomes eligible again after ~3 months, and the
  dialog said so when it was declined (D5, AC-02.9).
- AC-05.6 The Usage Data dialog and the survey nudge never appear in the same session (D9).
- AC-05.7 A browser that answered Yes is never asked again on any tier.
- AC-05.8 With the admin switch off, the dialog never appears (D4) — asserted independently of
  US-06, since this is the behaviour a user experiences.

---

### US-06 — Stop Lighthouse asking my people, and guarantee nothing is sent

`job_id: job-admin-stop-lighthouse-asking-my-people` · Slice 03

As the system administrator, I want one switch that stops both the asking and the sending for the
whole instance, so that I can satisfy a security review without relying on every user answering No.

#### Elevator Pitch
Before: usage data is governed entirely by individual users, so an administrator with a policy to
enforce has no lever and nothing to screenshot.
After: Settings → Configuration → Optional Features → switch **Usage Data** off → sees the toggle
off, and every footer indicator in the instance reads not-sending on the next load regardless of what
those users chose.
Decision enabled: whether the organisation's policy is actually enforced, answerable in one screen.

#### Acceptance Criteria
- AC-06.1 `UsageData` appears as an OptionalFeature with `IsPremium = true`, seeded `Enabled = true`,
  toggled under `RbacGuard(SystemAdmin)` (D4, S9, S10).
- AC-06.2 On upgrade of an existing instance it arrives enabled; on every later upgrade the
  administrator's setting survives (S9).
- AC-06.3 Switching it off stops all emission immediately, including from browsers holding live
  consent, enforced at the emit path rather than by hiding UI (D6).
- AC-06.4 Switching it off suppresses the dialog everywhere, including for undecided browsers
  (AC-05.8).
- AC-06.5 Switching it back on resumes emission for browsers that had consented, without asking them
  again (D6).
- AC-06.6 The settings page states in plain words that turning it off suspends existing consent and
  turning it back on resumes it (D6).
- AC-06.7 With it off, the footer indicator reads not-sending and its tooltip says the administrator
  has disabled usage data — distinguishable from "you declined".

---

### US-07 — Be told the switch is unavailable rather than have my change silently dropped

> **TRANSFERRED to story #5876 on 2026-08-31 — not implemented by this Epic.** #5876 creates the
> first premium optional feature and so reaches this branch first; it carries the fix as its US-02.
> Kept here in full because slice 03 depends on it: a privacy control may not ship on a gate that
> drops writes. Verify it holds before slice 03 starts. AC-07.3 (`DeltaSync` stays ungated) remains
> this Epic's invariant to check, wherever the fix was written.

`job_id: job-admin-stop-lighthouse-asking-my-people` · Precondition of Slice 03 · owned by story #5876

As a Community system administrator, I want a refused toggle to say it was refused, so that I do not
believe I have disabled something that is still running.

#### Elevator Pitch
Before: `OptionalFeaturesController:41` returns the unchanged feature when the licence does not cover
it — the write is dropped, the response is a success, and the caller cannot tell (S8).
After: toggle a premium optional feature without a premium licence → sees the control disabled with
the existing premium tooltip, and if the request is made anyway, an explicit refusal instead of a
success carrying stale state.
Decision enabled: whether the setting on screen is the setting in force — which on a privacy control
is the only question that matters.

#### Acceptance Criteria
- AC-07.1 A premium OptionalFeature toggle attempted without a premium licence returns an explicit
  refusal, not a 200 carrying the unchanged entity (S8).
- AC-07.2 The UI renders premium optional features with the existing premium affordance and tooltip,
  consistent with how other premium controls already present.
- AC-07.3 The existing `DeltaSync` toggle behaviour is unchanged for licensed and unlicensed
  instances alike — it is not premium and must not become gated by this fix.
- AC-07.4 A Community administrator can see that Usage Data exists, that it is on, and that turning
  it off requires Premium. It is not hidden (D5 is a commercial line, not a secret).

---

### US-08 — Tell whether a shipped feature is actually being used

`job_id: job-maintainer-know-if-a-shipped-feature-landed` · Slice 04

As the Lighthouse maintainer, I want a small set of named product events beyond the heartbeat, so
that I can answer "did anyone turn this on" with data instead of with three support tickets.

#### Elevator Pitch
Before: seven outcome KPIs sit at `deferred-pending-telemetry-feature` and every post-release question
is answered from community chatter and the vendor's own instance (S5).
After: open the collector dashboard → sees counts for each named event over the last 30 days, split
by version and licence tier.
Decision enabled: whether to invest further in a shipped feature, fix it, or retire it.

#### Acceptance Criteria
- AC-08.1 A small named set of product events (2–4) is chosen with the product owner at slice start
  from the deferred-KPI list and from what is currently in flight. The set is written down before any
  is instrumented.
- AC-08.2 Every event is enumerated in `docs/settings/usagedata.md` and in the consent dialog before
  it is emitted. An event that ships ahead of its documentation is a defect (Epic non-negotiable).
- AC-08.3 No event carries work item titles, queries, team or portfolio names, URLs, email addresses,
  or any free text. Asserted as an invariant over the payload, not left to review.
- AC-08.4 Every event carries the instance identifier and is subject to the same consent and master
  switch as the heartbeat.
- AC-08.5 A browser that consented before these events existed keeps its consent, and the events it
  did not see enumerated are added to the docs and the dialog on the next open. **Answered 2026-09-12:
  widening the payload does not by itself require re-consent** — but only inside a boundary, because
  an unbounded "no" would let any future slice add anything at all under a consent given for five
  fields. Re-consent **is** required if any one of these changes:
  - the payload gains anything scoped to a person, or any free text (AC-08.3 already forbids both, so
    crossing this line means AC-08.3 was relaxed first, deliberately);
  - the purpose widens past "measure the installed base and how Lighthouse is used";
  - the set of parties who hold the data widens — turning the collector's AI features on attaches four
    more, which is why they stay off;
  - retention lengthens, which a paid-plan upgrade would do silently (ADR-175 point 7).

  Short of those, the dialog and `docs/settings/usagedata.md` are updated in the same change that adds
  the event, and the existing consent stands. The reasoning: the purpose is unchanged, the data stays
  instance-scoped with no person in it, and the indicator and one-click revoke are in front of the
  user the whole time.
- AC-08.6 The KPI contracts whose questions these events answer move off
  `status: deferred-pending-telemetry-feature` and name the events that now source them (S5).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone (user activities, left to right):**

`Find out what Lighthouse sends` → `Decide` → `Change my mind` → `Govern it for my organisation`
→ (vendor side) `Learn from what came back`

| Activity | Slice 01a (decide) | Slice 01b (emit) | Slice 02 | Slice 03 | Slice 04 |
|---|---|---|---|---|---|
| Find out what it sends | US-01 indicator, US-02 dialog + docs page | docs page names the separate GitHub call | — | — | US-08 docs widened |
| Decide | US-02 (on click) | — | US-05 (asked unprompted, honest cadence) | — | — |
| Change my mind | US-03 as a record, indicator follows | US-03 enforced against the emitter (AC-03.2) | — | — | — |
| Govern it | — | — | — | US-06 switch (US-07 honest refusal → story #5876) | — |
| Learn from it | — | US-04 heartbeat + census | — | — | US-08 product events |

**Walking skeleton = slice 01a followed by 01b.** One browser consents, one heartbeat leaves, the
maintainer sees it, one click stops it. Split on 2026-09-12 so each half is a day's work. 01a sends
nothing, which is why the two copy corrections travel with 01b rather than with it: they are still
true across the whole of 01a. Everything after thickens a path that already runs.

**Preceded by SPIKE-00** (collector probe), which is not a slice: it ships no user value and must not
be released as one.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | **Passes, after the 2026-09-12 split.** The combined slice 01 shipped indicator + dialog + consent record + emitter + docs = 5 and failed this test. It was accepted as a walking-skeleton cost, and DESIGN then made it worse. Split into **01a** (indicator, dialog, consent record with its endpoints) and **01b** (gate, heartbeat with its day key, publisher, docs and copy). |
| Does every slice depend on a new abstraction? | No. Slices 01b–04 depend on the consent record, which slice 01a ships first — the abstraction leads, as required. |
| Does any slice disprove a pre-commitment? | Yes, each. See per-slice hypotheses in the slice briefs. |
| Synthetic data only? | No. Every slice's acceptance runs against the vendor's own production instance emitting into the real collector on the day it ships. |
| Two slices identical except for scale? | Slice 01b (heartbeat) and slice 04 (product events) are both "emit an event". Not merged: 01b proves consent and transport with a payload that has no user in it, 04 proves the event *vocabulary* is the right one, and they fail for different reasons. |

---

## Wave: DISCUSS / [REF] Prioritization

| Order | Item | Rationale |
|---|---|---|
| 1 | SPIKE-00 collector probe | Highest uncertainty, blocks everything, and the wrong answer is expensive in both money (plan tier) and architecture (D10). Failing here costs a day. |
| 2 | Slice 01a consent without emitting | The half that can fail for structural reasons on the consent side: a browser-held decision the server can act on later, and a dialog that costs nothing to close undecided. Ships no emit path, so the product's existing "we collect nothing" claims stay true throughout. |
| 3 | Slice 01b the first heartbeat | The half that can fail on the emit side: revocation latency, zero leak, and one heartbeat per instance rather than per replica. Also the slice that makes the product stop contradicting itself (S3, S4), because it is the slice that first makes those claims false — so nothing after it may ship before it. |
| 4 | Slice 02 the ask | Highest learning leverage per hour: uptake is the number the whole Epic rests on, and until people are actually asked it is unmeasured. Deliberately before the admin switch, because a switch governing a prompt nobody sees proves nothing. |
| 5 | Slice 03 admin veto | Dependency-driven: needs something to veto. The S8 defect fix it used to carry moved to story #5876 on 2026-08-31 and is now a precondition — confirm it before starting, and bring it back here if #5876 has not shipped. |
| 6 | Slice 04 product events | Last on purpose. The event vocabulary should be chosen once there is a real consenting population to spend it on, and after slice 02 tells us how big that population is. |

Dogfood cadence: every slice is dogfooded on the vendor's own instance the day it ships. Slice 01b's
dogfood *is* its acceptance (AC-04.6).

---

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Measurement | Slice |
|---|---|---|---|
| OUT-usagedata-consent-uptake | The dogfood instance's own grants ÷ decisions is ≥ 20% within 60 days of slice 02 | A direct SQL read of the consent table on the vendor's own instance | 02 |
| OUT-usagedata-instances-reporting | ≥ 25 distinct instance identifiers report in a rolling 24h window within 90 days of slice 02 | Distinct instance identifiers at the collector | 01, 02 |
| OUT-usagedata-zero-leak-before-consent | **0 requests to the collector host** from an instance that has no consenting browser | Automated: an integration test asserting no outbound call on the collector host across a full emit cycle with zero consent, plus an ArchUnitNET rule forbidding ad-hoc HTTP client construction so the assertion cannot be bypassed | 01 |
| OUT-usagedata-revocation-latency | 100% of revocations stop the next emit; 0 emits after a revoke | Automated assertion at the emit path (AC-03.2) | 01 |
| OUT-usagedata-payload-purity | 0 events carrying customer content across the whole event set | Automated invariant over emitted payloads (AC-08.3), run in CI | 01, 04 |
| OUT-usagedata-kpis-unblocked | ≥ 3 of the 7 KPIs at `deferred-pending-telemetry-feature` move to a live measurement source within 30 days of slice 04 | Count in `docs/product/kpi-contracts.yaml` | 04 |
| OUT-usagedata-no-nag-complaints | 0 community reports of a repeated or unstoppable Usage Data prompt within 90 days of slice 02 | Community channels, GitHub issues | 02 |

`OUT-usagedata-zero-leak-before-consent`, `-revocation-latency` and `-payload-purity` are hard CI
gates. The rest are collector-sourced and become measurable for the first time *because* of this
Epic — which is the recursion the Epic exists to break.

**`OUT-usagedata-consent-uptake` has been rewritten twice, and the second rewrite is the one that
matters.**

On **2026-09-11**, in DEVOPS, because the original wording was unmeasurable rather than merely hard:
it asked for grants ÷ dialogs shown *both counted at the collector*, and a browser that declines sends
nothing — that is D3, enforced as a hard CI gate by `OUT-usagedata-zero-leak-before-consent`. The
denominator would have required an event from the browsers that refused. The ratio is dropped
centrally; the one place a true ratio exists is the vendor's own instance, where the consent table
records grants, declines and revocations and a SQL read costs nothing. n=1, unrepresentative, and a
real number. See the DEVOPS Monitoring Contracts section.

On **2026-09-12**, because what survived that rewrite still had a half that could not fail. "Grants
per reporting instance trends upward over the 60 days after slice 02" is satisfied by one additional
grant, so it was a target in form only. It is deleted rather than re-targeted, because
`OUT-usagedata-instances-reporting` already asks the same funnel question with a threshold that can
actually fail — 25 distinct instances in a rolling 24-hour window within 90 days. Two measures over
one funnel, one of which cannot fail, is worse than one that can. What is left is the dogfood ratio:
n=1, falsifiable, and cheap.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All acceptance criteria pass, per slice, in CI.
2. Backend `dotnet build` zero warnings, `dotnet test` green.
3. Frontend `pnpm test` green, `pnpm build` zero errors and zero warnings, Biome clean.
4. SonarQube Cloud gate green — no new issues of any severity.
5. Mutation testing ≥ 80% kill rate on the consent and emit paths, both stacks.
6. `docs/settings/usagedata.md` enumerates every field of every event actually emitted, and names the
   GitHub release check as a separate outbound call (D11).
7. `SurveyNudge.tsx` copy and `cra-self-assessment.md` row 1.7 no longer claim Lighthouse does not
   track usage (S3, S4).
8. Screenshots regenerated for the settings surface and the footer indicator; website copy updated
   for the new premium capability.
9. ADO children created and transitioned; KPI contracts updated (AC-08.6).

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Business value articulated | PASS | 7 KPIs blocked on this Epic (S5); installed base unknown; Epic states the goal as "base for future features". |
| 2 | User stories in LeanUX format with elevator pitches | PASS | US-01…US-08, each with Before / After / Decision enabled. |
| 3 | Every story traces to a job | PASS | 4 job IDs, all four used; no `infrastructure-only` story. |
| 4 | Acceptance criteria testable | PASS | Every AC names an observable. The three purity/latency ACs are automated invariants rather than review items. |
| 5 | Dependencies identified | PASS | Pre-requisites section. SPIKE-00 and the DPA procurement lead time are the two that can actually block. |
| 6 | Slices ≤ 1 day with learning hypotheses | PASS | The exception was slice 01, which failed the 4-component taste test. Split into 01a and 01b on 2026-09-12; both pass, and each keeps its own hypothesis. See Slice Taste Tests. |
| 7 | Out-of-scope explicit | PASS | Out of Scope section; the OptionalFeatures rework is explicitly deferred to the board. |
| 8 | Outcome KPIs with numeric targets and measurement method | PASS | 7 KPIs, each with a target and a named source. |
| 9 | Compliance assessment, **self-performed and risk accepted by the maintainer** — six questions, see below | **CLOSED 2026-09-12 — all six answered** | Re-scoped on 2026-09-11. The row previously said "legal / DPO sign-off" and named two questions; three waves then widened it to six without editing the row, so it understated its own gate. It also assumed a legal function that does not exist here. The maintainer's decision: read it ourselves, accept the risk in writing, do not buy a review. |

**DoR-9's six questions, and where each stands:**

| # | Question | Status |
|---|---|---|
| 1 | The ePrivacy Art 5(3) reading behind D2 — a post-click token is strictly-necessary | **Accepted.** The argument is written out in D2 and stands on EDPB Guidelines 2/2023. Risk accepted: it is a reading, not a ruling |
| 2 | Whether a widened payload needs re-consent (AC-08.5) | **Closed 2026-09-12. No** — inside a stated boundary. Re-consent is required only if the payload gains something person-scoped or free-text, the purpose widens, the set of parties holding the data widens, or retention lengthens. Otherwise the dialog and docs are updated alongside the new event and existing consent stands. Full wording at AC-08.5 |
| 3 | The PostHog DPA | **READ 2026-09-11.** SCCs plus EU-US Data Privacy Framework; return-or-delete on request at termination; sub-processors by general authorization against a **dynamic** page. See the residency finding below |
| 4 | Retention and erasure (H10) | **Closed 2026-09-12.** One year, which is **not a setting** — PostHog fixes retention by plan (1 year free, 7 years on every paid plan) and says a shorter period cannot be configured or requested. A year answers every question we ask. Carry the consequence: a paid upgrade silently widens it to seven and falsifies the dialog. The identifier is not deleted when consent lapses, because that would count a returning instance twice. Revocation stops future sending and does not erase; the dialog says so rather than letting a reader infer deletion. Written into ADR-175 point 7 |
| 5 | `POSTHOG_PERSONAL_API_KEY` blast radius (P11) | **Accepted** with the custody in P11 |
| 6 | Whether census data may appear in a public CI log (P14) | **Closed by P14** — the canary asserts on counts and property names only |

**The residency claim is narrower than it reads, and the dialog copy must not overstate it.** The DPA
says: *"Company acknowledges that the Processor will Process the Company Personal Data outside of the
Protected Area including in the US"*. That is compatible with everything the SPIKE established — the
live sub-processor page confirms AWS, PlanetScale and Modal in Germany and Wiz in Germany/France, with
Cloudflare as global edge — because it covers PostHog's own staff and internal tooling rather than
where the data rests. But "data rests in Frankfurt" is **not** "data never leaves the EU", and a reader
of the consent dialog who infers the second from the first has been misled by omission. That is the
same failure class A13 exists to prevent for the IP, and it earns the same treatment: the dialog says
where the data rests and does not imply it never goes further.

**DoR verdict: 9 of 9 PASS as of 2026-09-12, and all six of DoR-9's questions are now answered.**
Question 4 closed with the retention and erasure position in ADR-175 point 7; question 2 closed with
a bounded "no" on re-consent at AC-08.5. **Nothing in this Epic is gated on an external party or on
an unwritten decision.**

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions
- [D1] Named **Usage Data**; `Telemetry` keeps its Prometheus meaning (S1).
- [D2] Consent is per browser via an opaque post-click token — explicitly **not** a fingerprint.
- [D3] Nothing sent until a browser consents, heartbeat included; the census counts consenting
  instances and says so.
- [D4] Admin switch is a premium OptionalFeature, default ON; governs asking, not consent.
- [D5] Community is re-asked every ~3 months and cannot switch the asking off; the copy says so.
- [D6] Master OFF suspends consent, never revokes it; ON resumes silently.
- [D7] Permanent footer indicator: state-differentiated, tooltipped, clickable.
- [D8] Revocation is one click and immediate, enforced server-side.
- [D9] Coordinate with the survey nudge; never bundle (GDPR Art 7(4)).
- [D10] Collector undecided; SPIKE-00 then DESIGN.
- [D11] The GitHub release check is documented, not changed.

### Requirements Summary
- Primary jobs: the maintainer needs to know whether shipped features landed and what the installed
  base runs; the user needs to decide once, on evidence, and be able to undo it; the administrator
  needs one enforceable lever.
- Walking skeleton scope: slice 01, split into **01a and 01b** on 2026-09-12 — consent → heartbeat →
  collector → revoke, plus the docs page and the two copy corrections that stop the product
  contradicting itself.
- Feature type: cross-cutting (backend emitter, frontend consent surface, licensing, public docs,
  compliance).

### Constraints Established
- No user identity exists on standalone or auth-off instances (S11) — per-account consent is off the
  table, not merely inconvenient.
- Nothing may be read from or written to the browser before the consent click (D2).
- No event may carry customer content, enforced as a CI invariant.
- The premium gate must stop silently dropping writes before a privacy control rides on it (S8). Fixed by story #5876, not here — a precondition to verify, not an assumption to hold.
- Two shipped surfaces and one compliance document currently promise the opposite of this Epic and
  must change with slice 01b (S3, S4) — the half that first makes them false.

### Upstream Changes
- **`docs/product/kpi-contracts.yaml` preamble is now false in principle.** It states there is no
  phone-home mechanism. Amended at slice 04 (AC-08.6), when the first KPI actually moves. Not amended
  earlier — the statement stays true for every non-consenting instance, which is all of them until
  slice 02.
- **`docs/compliance/cra-self-assessment.md` row 1.7 is amended at slice 01b**, not slice 04. A
  conformance claim must be true the moment the capability exists, regardless of uptake.
- **#5015's non-negotiables are inherited into this Epic verbatim** (opt-in only, GDPR-compliant,
  transparent, configurable endpoint). #5015 is `Removed`; this delta is where they now live.

---

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | Appended 4 jobs with dimensions, four forces and opportunity scores. |
| `docs/product/journeys/epic-5733-opt-in-usage-data.yaml` | Created — the consent journey with emotional arc, shared artifacts and error paths. |
| `docs/product/personas/privacy-decider.yaml` | Created. |
| `docs/product/kpi-contracts.yaml` | Not yet touched — amended at slice 04 per Upstream Changes. |

---

## Wave: DISCUSS / [REF] Handoff

**To DESIGN (`nw-solution-architect`)** — full artifact set. The design questions this delta
deliberately leaves open:

1. The collector, after SPIKE-00 (D10), and how the endpoint is made configurable for an air-gapped
   customer.
2. Where the consent record lives and how the emit path consults it cheaply enough to run on every
   emit (D8 demands no staleness).
3. Whether events are emitted browser-side or server-side. Server-side is not ad-blocked and exposes
   the server IP; browser-side is the reverse and will be blocked for a meaningful share of this
   product's engineering audience. This materially changes what the collector can be, so it belongs
   with SPIKE-00 rather than after it.
4. How the master switch reaches the emitter without a per-emit database read.
5. The instance identifier's minting and persistence (S12 — nothing exists).

**To DEVOPS (`nw-platform-architect`)** — the Outcome KPIs section only, plus one note: three of the
seven KPIs are CI gates rather than dashboards, and `OUT-usagedata-zero-leak-before-consent` needs a
test that asserts the *absence* of network traffic, which is a harness capability this repository does
not have today.

---

## Wave: DESIGN / [REF] DDD Decision

**No DDD wave ran, and none is warranted.** Design scope was application/components only.

Usage Data is a **supporting subdomain**, not a core one: it carries no business rules worth
modelling, no invariants beyond "may we send", and no ubiquitous language a domain expert would
recognise. It is a consent record, a gate and an outbound adapter. Introducing aggregates, domain
events or a bounded-context map here would be ceremony over four types.

One boundary decision does carry weight and is recorded as a DDD-shaped fact: **Usage Data shares no
state with SurveyNudge.** They share an idea (install-age gating, a quiet cadence) and no data. The
survey nudge's state is one instance-wide row; consent is per browser. Merging them would force
browser scope onto a shipped instance-wide behaviour.

`Telemetry` keeps its existing meaning throughout — the OpenTelemetry/Prometheus scrape. Occupied
names verified: `TelemetryConfiguration`, `TelemetryLoggingConfiguration`, `TelemetryConfigurator`,
config section `"Telemetry"`. Everything new is `UsageData*`. No collision.

---

## Wave: DESIGN / [REF] Component Decomposition

Full table in `docs/product/architecture/brief.md` under
`## Application Architecture — epic-5733-opt-in-usage-data`. Summary: 12 new backend types, 4 new
frontend components, 6 extensions, 1 defect fix.

The four that carry the design:

| Component | Why it exists |
|---|---|
| `UsageDataConsent` + bespoke repository | One row per browser, keyed by token digest, with a `LastSeenAt` liveness stamp. `AnyLiveGrantAsync` is a SQL `EXISTS`, which is also how "revoking the last browser stops the heartbeat" happens with no rule of its own |
| `IUsageDataGate` | Reads the master switch and the consent set **fresh on every emit**. Fail-closed. No cache, so staleness is unrepresentable rather than mitigated |
| `UsageDataEmitPermit` | Sealed, internal constructor, minted by the gate. The publisher requires one, so an emit cannot happen **by omission** — there is no path to publish that skips the gate by accident. Not a compile-time guarantee (one assembly, `InternalsVisibleTo`); enforcement is `UsageDataEmitSeamArchUnitTest`. Corrected 2026-09-11 — this row still carried the claim C1 retracted |
| `PostHogUsageDataPublisher` | The **named** adapter. Not a port with a decision attached to it |

---

## Wave: DESIGN / [REF] Driving Ports

| Surface | Kind | Auth | Slice |
|---|---|---|---|
| `GET /api/latest/usagedata/state` | HTTP | None; token in a request header when present | 01 |
| `POST /api/latest/usagedata/consent` | HTTP | None | 01 |
| `DELETE /api/latest/usagedata/consent` | HTTP | None; token in header | 01 |
| Footer indicator opens the dialog | UI | — | 01 |
| Consent dialog, two decisions | UI | — | 01 |
| Consent dialog, unprompted on install age | UI | — | 02 |
| Optional Features, Usage Data toggle | UI | `RbacGuard(SystemAdmin)` | 03 |
| `POST /api/latest/optionalfeatures/{id}` returning an explicit refusal | HTTP (existing, corrected) | `RbacGuard(SystemAdmin)` | 03 |

**Reading of "tokenless".** The DISCUSS ports table marks the state endpoint tokenless. Designed as
"requires no **authentication**" — per-browser state is unanswerable unless the browser names itself,
and on an auth-off instance there is nothing to authenticate against. An absent token and an unknown
token get byte-identical responses so the endpoint is not an oracle.

---

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter | Notes |
|---|---|---|
| `IUsageDataPublisher` | `PostHogUsageDataPublisher` | `{CollectorBaseUrl}/i/v0/e/`, default `https://eu.i.posthog.com`, configurable for air-gap |
| Consent persistence | `UsageDataConsentRepository` | Bespoke interface, not `IRepository<T>` |
| Instance identifier | `AppSettingService` | Existing key/value store |
| Deployment-mode signal | `UsageDataDeploymentModeResolver` | Composes `IPlatformService`, does not modify it |

**External integration → `nw-platform-architect`.** PostHog Cloud EU. A consumer-driven contract test
on the capture transport is **not** recommended — emit is fire-and-forget and degrades silently by
specification. What *is* required is a **scheduled privacy-behaviour canary**: emit one event under a
reserved identifier, read it back through the PostHog query API, assert the stored record has no
`$ip`, no `$geoip_*`, and that AI features are off. Needs a PostHog personal API key in CI secrets, an
egress allowance and a schedule. This is a different thing from a contract test wearing similar
clothes: it asserts the processor's *handling of data*, not the API's schema.

---

## Wave: DESIGN / [REF] Technology Choices

No new library. PostHog is the only new dependency and it is a network endpoint, not a package.
CSPRNG is `RandomNumberGenerator` (already the house choice in four services). Architecture rules ride
the existing ArchUnitNET adoption in `Lighthouse.Backend.Tests/Architecture/`.

---

## Wave: DESIGN / [REF] Decisions

| ID | Decision | ADR |
|---|---|---|
| A1 | Consent is a server-side row keyed by the SHA-256 digest of a CSPRNG token, with a `LastSeenAt` liveness window (default 30 days) | [ADR-173](../../product/architecture/adr-173-consent-as-a-server-side-record-with-a-liveness-window.md) |
| A2 | The consent store is a bespoke repository, not `IRepository<T>` — that base evaluates predicates client-side over a materialised `DbSet`, and revoke/touch need affected-row counts | ADR-173 |
| A3 | The emit gate reads switch and consent fresh on every emit; **the "no per-emit database read" constraint is declined for slice 01** | [ADR-174](../../product/architecture/adr-174-the-emit-gate-is-uncached-fail-closed-and-mints-a-permit.md) |
| A4 | The gate is fail-closed; every uncertainty resolves to "do not send" | ADR-174 |
| A5 | Publishing requires a `UsageDataEmitPermit` that only the gate can construct | ADR-174 |
| A6 | The emitter is a plain `BackgroundService`, **not** an `UpdateServiceBase` | ADR-174 |
| A7 | The domain-event bus is **not** the invalidation channel — it swallows handler exceptions by design | ADR-174 |
| A8 | Instance identifier is one `AppSettings` scalar, 16 CSPRNG bytes, minted **only on a grant** | [ADR-175](../../product/architecture/adr-175-instance-identifier-as-an-appsettings-scalar-minted-on-first-grant.md) |
| A9 | ~~A unique index on `AppSetting.Key` ships in the same migration, as the get-or-create arbiter~~ **WITHDRAWN 2026-09-11** — `Key` is already the primary key (`LighthouseAppContext.cs:101`), so the arbiter ships today and no migration is needed. The dedup step, its two-provider test and the rollback contract built on it are withdrawn with it | ADR-175 |
| A10 | Collector is `PostHogUsageDataPublisher`, named, behind a port | [ADR-176](../../product/architecture/adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md) |
| A11 | The privacy guarantee moves into the payload (`$ip: null`, `$geoip_disable: true`) where CI can assert it — **verified against PostHog's server source; either property alone suppresses enrichment** | ADR-176 |
| A12 | A scheduled canary reads the vendor back and asserts no IP, no geo enrichment, AI features off | ADR-176 |
| A13 | The dialog is bound by a forbidden-phrase test to claim no more than the controls demonstrate | ADR-176 |
| A14 | Deployment mode is a closed usage-data-owned set including `Kubernetes`; `PlatformService` is composed, not modified | [ADR-177](../../product/architecture/adr-177-deployment-mode-is-a-usage-data-owned-closed-value-set.md) |

---

## Wave: DESIGN / [REF] Reuse Analysis

Every component with overlapping responsibility, with evidence. `UNCHANGED` means assessed and left
alone; where it means "assessed and rejected", the reason is stated.

| Existing | Verdict | Evidence |
|---|---|---|
| `IAppSettingService` / `AppSettingService` | **EXTEND** | `EnsureInstallTimestamp()` is already a lazy get-or-create over an `AppSettings` row — the exact shape the instance identifier needs. Private `UpsertSetting`, `TimeProvider` and the repository are all in place |
| `AppSettingKeys` | **EXTEND** | One constant, existing `Area:Name` convention |
| `AppSetting` entity | **UNCHANGED** | `Key` is already the primary key (`LighthouseAppContext.cs:101`), so concurrent get-or-create is arbitrated with no schema change. Note the separate hazard this uncovered: `Id` is **not** the key and carries no constraint, `UpsertSetting` mints every row with the default `Id = 0`, and `AppSettingSeeder.RemoveObsoleteSettings` deletes **by `Id`** — safe today (its list starts at 9), guarded by a test rather than a migration |
| `OptionalFeature` + `OptionalFeatureKeys` | **EXTEND** | One key. `IsPremium` already exists on the entity |
| `OptionalFeatureSeeder` | **EXTEND** | `AddOrUpdateCurrentFeatures` seeds a new key with its declared `Enabled` and never overwrites it afterwards — exactly the upgrade semantics required, for free |
| `OptionalFeaturesController` | **UNCHANGED HERE — defect fixed by story #5876** | Line 41 returns the unchanged feature on a premium miss: HTTP 200, write dropped, caller cannot tell. Story #5876 fixes it, because it creates the first premium optional feature and reaches the branch first. This Epic verifies the refusal holds and that `DeltaSync` stayed ungated. If #5876 has not shipped, the fix returns here and the **shared contract** caution applies — grep callers and extend the test factory first |
| `UpdateServiceBase<TEntity>` + `TeamUpdater`/`PortfolioUpdater`/`ForecastUpdater` | **UNCHANGED — rejected as base class** | `where TEntity : class, IEntity`; iterates `repository.GetAll()`, asks `ShouldUpdateEntity` per row, enqueues per-entity work under an `UpdateType` feeding the SignalR status hub. The heartbeat is instance-scoped with no entity to iterate. Riding it needs a fake entity and two `NotSupportedException` overrides. House precedent for instance-scoped background work is plain `AddHostedService` (`GracefulShutdownService`, `KeyRingFileWatcher`) |
| `UpdateQueueService` / `IUpdateExecutionLock` | **ASSESSED — verdict reopened 2026-09-11** | The original reason ("cluster-wide single execution is not needed; the identifier is per instance, not per replica") is a true clause that does not imply its conclusion, as C2 found: every replica runs the emitter. ADR-174 point 8 replaced it with a `UsageData:LastHeartbeatDay` compare-and-swap — but `AppSettingService.UpsertSetting` is read-then-write (`GetByPredicate` → `Add`/`Update`), so that store cannot perform a CAS and no component change was specified. **Open: either give the day key a bespoke conditional-update accessor, or re-adopt the execution lock with a restart guard** |
| `IDomainEventDispatcher` | **UNCHANGED — rejected as invalidation channel** | Swallows handler exceptions by design (`CA1031` suppressed: *"one failing handler must not abort the others"*). Correct for metrics; for a consent gate a dropped invalidation means sending after a revoke, silently |
| `PlatformService` / `IPlatformService` | **COMPOSE, not modify** | `IsDocker()` is true in a Kubernetes pod; `KUBERNETES_SERVICE_HOST` appears nowhere. Adding a `SupportedPlatform.Kubernetes` member would touch the release/update path where `Docker` carries operational meaning about in-place updates |
| `GitHubService` | **UNCHANGED** | Pre-existing unconsented outbound call. Documented in the docs page, not altered. Its bypass of `IHttpClientFactory` is why no outbound chokepoint exists — raised as a board item, not fixed here |
| `IRandomNumberService` | **UNCHANGED — rejected as unsuitable** | It is `new Random().Next(maxValue)`. Statistical PRNG, sole consumer is the Monte Carlo forecaster. Using it for token or identifier minting would be a security defect |
| `EmbedSessionTokenService` | **UNCHANGED — technique copied** | CSPRNG + base64url + digest-at-rest + conditional-update-as-verdict. Machinery not shared: single-use 60-second redemption vs long-lived revocable consent |
| `IEmbedSessionTokenRepository` | **UNCHANGED — cited as precedent** | Its own comment is the justification for A2: *"Deliberately not `IRepository<T>`: single use is a conditional update returning an affected-row count"* |
| `ApiKeyService` | **UNCHANGED** | `FindMatchingKey` re-derives PBKDF2 across every row per call — a lookup shape this design must not copy |
| `IRepository<T>` / `RepositoryBase<T>` | **UNCHANGED — rejected for the consent store** | `GetByPredicate` takes `Func<T,bool>` and evaluates client-side over a materialised `DbSet`. Fine for a dozen `AppSettings` rows; would load every consent row per emit |
| `ISystemInfoService` / `SystemInfo` | **UNCHANGED — deliberately** | Readable by any signed-in viewer including inside an embed frame. Its own doctrine (*"named here rather than at the call site so that a fourth one added later is withheld by this sentence"*) is honoured by keeping the identifier out |
| `TelemetryConfiguration` / `TelemetryConfigurator` | **UNCHANGED** | Namespace-disjoint. Those names stay OpenTelemetry/Prometheus |
| `SurveyNudge.tsx` | **EXTEND (copy fix)** | Line 114 ships *"Lighthouse never tracks how you use it"* to every Community user. Corrected in slice 01b — the slice that first makes it false — or slice 01b does not ship |
| `nudgeEligibility.ts` | **UNCHANGED — pattern copied** | A pure decision function, not a hook. The model for `usageDataEligibility.ts`. Arithmetic copied, storage not: nudge state is one instance-wide row, consent is per browser |
| `AppSettingService` survey cadence | **UNCHANGED — arithmetic copied** | Same reason. Sharing would force browser scope onto a shipped instance-wide behaviour |
| `Footer.tsx` | **EXTEND** | Mount the indicator in the right-hand box beside `LighthouseVersion` |
| `ExternalLinkButton` | **UNCHANGED — rejected as host** | It is `component="a" href target="_blank"`. The indicator opens a dialog. Hosting it would mean making `link` optional and branching anchor-vs-button. Copied for visual language only |
| `LighthouseVersion.tsx` | **UNCHANGED** | Already 405 lines and already renders two `Tooltip > IconButton onClick` clusters. Sibling component rather than growing it further |
| `useRbac` | **UNCHANGED — anti-pattern for this case** | Fails **open** via `PERMISSIVE_SUMMARY` on purpose. The indicator must fail **closed**. Named so the adjacent, obvious thing to copy is not copied |
| `ApiServiceContext` / `BaseApiService` | **EXTEND** | Register `usageDataService`; subclass `BaseApiService`. Established pattern, plus `MockApiServiceProvider` |
| `useArchiveConfirmationPreference` | **UNCHANGED — pattern copied** | Closest existing browser-scoped preference. Namespaced key convention (`lighthouse:usagedata:consent`), read in `useEffect` not during render |
| `ArchiveConfirmationDialog.test.tsx` | **UNCHANGED — technique copied** | Its `it.each([...])("never promises the Delivery is %s")` forbidden-word assertion over rendered copy is the enforcement mechanism for A13 |
| `SystemSettingsTab.tsx` | **UNCHANGED** | Already disables premium toggles without a licence and shows `LicenseTooltip` — the UI half of the honest-refusal story already ships. Only the API refusal is missing |
| `Lighthouse.Backend.Tests/Architecture/` | **EXTEND** | ArchUnitNET already adopted; add `UsageDataEmitSeamArchUnitTest` |

---

## Wave: DESIGN / [REF] Contradictions with the DISCUSS Delta

Flagged loudly rather than smoothed over. Four items; three are corrections, one is a declined premise.

1. **S13 points at the wrong affordance.** The delta says the footer *"already renders an icon row
   with tooltips (`ExternalLinkButton`) beside `LighthouseVersion` — the exact affordance the
   indicator needs, already styled."* That row is the **"Contact us:"** group — Email, LinkedIn,
   Slack, Product Board, Ko-fi — and every entry is an `<a href target="_blank">`. A privacy
   indicator there reads as a sixth way to contact the vendor, and `ExternalLinkButton` cannot host a
   dialog trigger without becoming two components in a trench coat. The indicator goes in the
   right-hand box beside `LighthouseVersion`, which already renders exactly this affordance twice.

2. **The "no per-emit database read" constraint is declined for slice 01.** It appears in the DISCUSS
   handoff (question 4) and in the slice-03 reference class. The slice-01 emit is once per day; a
   per-emit read is two queries per day. Accepting the constraint would buy a cache whose only
   possible failure is the one this feature cannot afford. The constraint becomes real at slice 04's
   per-action events, and the gate interface is shaped to absorb that behind one method.

3. **SPIKE design implication and F4 place the emitter on `UpdateServiceBase`.** *"The daily emit
   hangs naturally off the `BackgroundServices/Update/UpdateServiceBase` pattern."* It does not — that
   base is per-entity, queue-backed and wired to the operator's refresh-status surface. Plain
   `BackgroundService`. The `Program.cs` CI cost that F4 predicts still applies and is still accepted.

4. **"Exactly five fields" risks implying "and nothing else reaches the processor".** AC-02.1 and
   AC-04.2 are correct about the payload. But the TLS peer address reaches the collector's edge on any
   HTTP request, and this audience is engineers. The dialog must say so plainly and say what is done
   about it. **This adds a copy requirement AC-02.1/AC-02.2 do not currently carry**, and A13's
   forbidden-phrase test enforces the negative half: the dialog may never state the IP is not
   transmitted, not seen, or never leaves the machine.

---

## Wave: DESIGN / [REF] Assumptions Made Without Being Able to Ask

Running headless in PROPOSE mode. Each is a real decision taken on the architect's judgement; each is
cheap to reverse and none is load-bearing for the shape of the design.

| # | Assumption | Reverse if |
|---|---|---|
| 1 | Consent liveness window = **30 days** | **RESOLVED 2026-09-12 — kept at 30 days, and the dialog now says so.** Shortening it decays the consent of anyone who does not open Lighthouse weekly, and re-asking someone who already agreed is the repeated prompt the no-nag outcome exists to prevent. The month-long tail is disclosed rather than engineered away. See ADR-173 |
| 2 | "Tokenless" state endpoint means "no authentication", with the consent token in a request header | Intended literally, in which case per-browser state is not answerable and US-01 needs rework |
| 3 | Instance identifier is minted on a **grant only**, not on a refusal | Read differently — but AC-04.3's "no consenting browser means no identifier at all" seems decisive |
| 4 | Consent token stored in `localStorage`, not `sessionStorage` | Consent should not survive a browser restart, which would contradict "decide once" |
| 5 | Deployment mode gains `Kubernetes` now rather than shipping the conflated set | The product owner would rather not spend a value-set decision before the census exists |
| 6 | PostHog honours `$ip: null` and `$geoip_disable` per-event | **RESOLVED — verified from PostHog's own server source**, not from the unreachable website. See "Assumption 6 — RESOLVED" below. No longer blocking |

---

## Wave: DESIGN / [REF] Open Questions Deferred

**To DISTILL (`nw-acceptance-designer`)**

- The liveness-window gap in AC-03.5 needs its own scenario: cleared storage makes a browser
  immediately undecided and immediately re-askable, but stops it counting toward the heartbeat only
  after the window. Both halves are testable; the delta currently implies one behaviour.
- AC-07.1's refusal is a contract change on an endpoint with existing callers. Extend the test factory
  before touching it.
- The zero-leak assertion needs the `DelegatingHandler` harness built as part of slice 01b, not after.
  It cannot be written in 01a, where no collector client exists for it to observe.

**To DELIVER**

- **Verify the PostHog per-event privacy properties before slice 01b ships** (assumption 6). Blocking.
- Set the canary schedule interval — it is the exposure window for vendor drift.
- `docs/settings/usagedata.md` is the single source for the field list with three consumers that must
  agree (dialog, docs, CI assertion). Build the comparison check with the first event, not the fifth.
- `ARCHITECTURE.md` §16 says "The full set (001–159)" and is stale; correct it to 177 in this change.

**Still open upstream, unchanged by DESIGN**

- ~~DoR-9 remains open and blocking for slice 01 shipping.~~ **Closed 2026-09-12** — self-performed
  and risk-accepted rather than bought as a legal review, which is what the row had assumed. See the
  DoR Validation section.
- ~~AC-08.5 (whether a widened payload requires re-consent) is a legal question.~~ **Answered
  2026-09-12: no, within a boundary** — see AC-08.5. A14 still fixes the deployment-mode value set at
  slice 01, because widening a value set the dialog enumerates is cheaper to avoid than to justify.

---

## Wave: DESIGN / [REF] Peer Review and Revisions

Adversarial review run 2026-08-22. **Verdict: REJECT** — 5 critical, 11 high. The review was right
about the things that mattered, including two claims in the first draft that were simply false. What
follows is what changed, not a defence.

### Corrected — claims that were untrue as written

| # | Finding | Correction |
|---|---|---|
| C1 | *"It does not compile"* — the permit was presented as a compile-time guarantee, and the brief rested Testability on it. `Lighthouse.Backend` is **one assembly with `InternalsVisibleTo`**, so an internal constructor is visible to every backend type. The claim was false and "assembly-visible seam" was hand-waving | ADR-174 §2 rewritten. The permit prevents emission *by omission* and gives ArchUnit one greppable type. Enforcement is a test and is now described as one. Separate assembly noted as the only way to make the original claim true; not proposed for slice 01 |
| C3 | The zero-leak enforcement was a tautology — the `DelegatingHandler` watches the client that is already permit-guarded, and cannot see `GitHubService`'s self-built `GitHubClient`. ADR-174 stated this problem in Context and then claimed the outcome met | New "honesty note" section in ADR-174. Added an ArchUnitNET rule forbidding ad-hoc client construction. **Recommends rescoping `OUT-usagedata-zero-leak-before-consent` from "0 bytes" to "0 requests to the collector host"** — a delta change, flagged below |

### Corrected — defects

| # | Finding | Correction |
|---|---|---|
| C2 | **Multi-replica emits N heartbeats/day.** A plain `BackgroundService` runs in every replica; all share one database and one identifier. AC-04.1 violated; every per-day count multiplied by replica count, invisibly. The reuse verdict dismissing `IUpdateExecutionLock` rested on a true clause that did not imply its conclusion | ADR-174 point 8: a `UsageData:LastHeartbeatDay` compare-and-swap. Preferred over the execution lock because it also survives a mid-day restart. Reuse verdict rewritten |
| H1 | **`Revoked` collapsed into `Declined`**, so a Premium user who granted then revoked would never be asked again — contradicting AC-03.4 | Third enum state. Cadence keys on `Decision` + `AskedAt`; `Revoked` is re-askable |
| H3 | `POST /consent` was unauthenticated, unrate-limited, and wrote a durable row per call. A single caller could plant one `Granted` row and keep an instance emitting for a full liveness window | `UsageDataConsentPolicy`, matching `AuthLoginPolicy` / `ApiKeysPolicy` / `EmbedSessionPolicy` |
| Q1 | Write-on-read `LastSeenAt`: unthrottled writes on a hot path with SQLite's process-wide writer lock; and **a caching proxy would suppress the touch and silently decay consent under an active user** | Throttled to a `WHERE LastSeenAt < @stale` conditional update (~7h at a 30-day window), plus a mandatory `Cache-Control: no-store`. Also corrected the claim that the window measures "activity" — it measures a browser still presenting its token |
| H5 | ADR-175 said the identifier was written *"in the same save"* as the consent row **and** insert-then-catch-unique-violation. Both cannot hold: a violation aborts the save and **loses the user's grant** | Writes separated and ordered: identifier first in its own transaction, then the consent row. New enforcement row: a losing race still records the grant |
| H4 | ~~The `AppSetting.Key` unique index could fail on upgraded customer databases, bricking an upgrade for a feature they never enabled. And the concurrency test was **vacuous on EF InMemory**, which does not enforce unique indexes~~ | **THE FINDING ITSELF WAS WRONG, retracted 2026-09-11.** `Key` is already the primary key (`LighthouseAppContext.cs:101`): duplicates are unstorable, no index is added, no upgrade can break, and EF InMemory *does* enforce primary keys. The remediation it prescribed — a row-deleting dedup migration with a two-provider test — was then inherited by DEVOPS and hardened into a database-restore rollback contract. An adversarial review sharpening an unchecked premise is how a phantom risk gains authority; the premise needed one `grep` |
| C4 | The canary could pass vacuously — "property X is absent" is green forever if the API stops projecting it | **Positive control added**: a second event omitting `$geoip_disable`, asserting geo properties *do* appear. Plus a bounded poll for ingestion latency, a stated daily interval, and "AI features off" demoted to layer 3 as probably unanswerable via the query API |
| C5 | The `/state` response contract was unspecified while five ACs depended on its fields — and AC-02.9's Community/Premium copy needs licence tier, which is otherwise behind `[Authorize]` | DTO specified in the brief. The anonymous-disclosure decision taken explicitly: return the **derived** `willAskAgain` boolean, not the tier |

### Accepted, not yet fixed — carried into DISTILL

- **H6 — slice 01 is oversized and DESIGN made it worse. ACCEPTED AND DONE 2026-09-12.** DISCUSS
  accepted a 5-component exception; DESIGN's own summary was 12 backend types + 4 frontend + 6
  extensions + 2 migrations + a schema change + an ArchUnit fixture + a test harness + a docs CI
  check. The reviewer's split is now the plan: **01a** consent record, endpoints, indicator and
  dialog, sending nothing; **01b** gate, publisher, heartbeat, docs and copy. The objection that
  splitting would leave the product contradicting itself mid-slice runs the wrong way — 01a sends
  nothing, so the two surfaces promising Lighthouse collects nothing stay true across the whole of it,
  and they are corrected in 01b, where they first become false. One consequence to carry: the
  zero-leak assertion is vacuously green in 01a, because there is no collector client there to make a
  request, so it must be written in 01b or it proves nothing.
- **H2 — AC-05.6 (never in the same session as the survey nudge) has no owner.** A GDPR Art 7(4)
  requirement with no named mechanism. Proposed: a session-scoped flag written by whichever dialog
  opens first, owned by the shared caller of the two eligibility functions. Needs a decision in DISTILL.
- **H10 — no retention or erasure position. CLOSED 2026-09-12.** Answered in ADR-175 point 7: 13
  months at the collector, the identifier kept when consent lapses so that a returning instance is not
  counted as a new one, and revocation as a stop rather than an erasure — with the dialog saying so
  plainly instead of leaving a reader to infer deletion.
- **AC-01.2 / AC-01.4 / AC-02.7 remain untraceable**: greyscale-safe indicator states, un-gated
  rendering across all three deployment shapes, and how the indicator flips in the same interaction as
  the dialog (two components, two directories, no shared state designed). DISTILL must close these.
- **Clock skew unaddressed** — no ADR states UTC. Given Bug #5567 was exactly a backend UTC-anchor
  error, all usage-data timestamps must be `timeProvider.GetUtcNow()`, stored UTC, with an enforcement row.
- **Missing index** — `AnyLiveGrantAsync` filters `Decision` + `LastSeenAt`; only `TokenHash` is
  indexed, so the "one indexed existence question" is a table scan. Composite index needed.
- **The slice-04 cache invariant has no test until slice 04.** Recommended: write it now, skipped,
  with the un-skip condition named, per this repo's own green-before-push convention.

### Accepted as fair criticism of the framing

- **Q2 — I over-promoted a design question to a constraint.** "No per-emit database read" is not an
  AC, KPI or locked decision; it appears only in the handoff's open questions and a slice brief — and
  the *same handoff's question 2* asks the opposite ("cheaply enough to run on every emit; D8 demands
  no staleness"). The two conflict. The honest framing is that question 2 reflects D8 and wins, not
  that a hard constraint was courageously declined. **The decision stands; the rhetoric was inflated.**
- **Four alternatives across the set are strawmen** (fingerprinting, already forbidden by D2;
  `IRandomNumberService`, listed twice; `IOptionsMonitor`; the deliberately-worst hybrid cache). Real
  reuse rejections belong in the Reuse verdict, where they already appear.
- **"Four layers" was arithmetic, not depth.** Layer 4 controls what we *claim*, not what happens to
  data. At design time there are **zero verified preventive controls**. The correct description is:
  one conditional preventive control, one detective control, one procedural control, and a binding
  constraint on what the dialog may say.

### Upstream change requested (back-propagation to DISCUSS) — APPROVED and applied

**`OUT-usagedata-zero-leak-before-consent` has been rescoped** from *"exactly 0 bytes leave an
instance"* to *"0 requests to the collector host"*. Approved by the product owner on 2026-08-22 and
applied to the Outcome KPIs table above. The original wording cannot be met by any CI gate in a
codebase with a second, known, unconsented outbound call — and a hard gate that cannot measure its own
claim is the failure mode this Epic exists to remove. The ArchUnitNET rule forbidding ad-hoc HTTP
client construction is what stops the narrower claim from being quietly bypassed.

**Slice 01 is NOT re-sliced.** DESIGN flagged it as oversized and observed that its own design made it
worse. The product owner reviewed and let the walking-skeleton exception stand, on the DISCUSS
reasoning that removing any one of the five components leaves a path that does not run end to end.

### Assumption 6 — RESOLVED, and not by a spike

**PostHog honours `$ip` and `$geoip_disable` per event. Verified from PostHog's own server source**,
which removes the need for the pre-slice-01 probe this section originally demanded.
`PostHog/posthog:nodejs/src/cdp/templates/_transformations/geoip/geoip.template.ts` opens its GeoIP
transformation with:

```
if (event.properties?.$geoip_disable or empty(event.properties?.$ip)) {
    print('geoip disabled or no ip.')
    return event
}
```

Both switches are carried in the event payload, and **either one alone** is sufficient — the event
returns unenriched. `$ip: null` satisfies the `empty()` branch. So the layer-1 control in ADR-176 is
a property of the vendor's published code rather than a hoped-for behaviour, and asserting our own
outbound payload in CI is a real invariant.

A second, independent layer holds by default: PostHog's data-collection documentation states that
**"EU organizations: Automatically default to IP data capture disabled for GDPR compliance."** The
safe state is the default rather than something an operator must remember to set.

Two limits worth stating rather than glossing. This is the `master` branch of the transformation
template — it establishes the mechanism and the branch logic, not that PostHog Cloud EU runs that
exact build. And it says nothing about whether the raw IP is observed upstream of the transformation
pipeline, only that it is not used for enrichment and, with IP capture off, not stored. The canary in
ADR-176 — including its positive control — remains worth running once the project exists, as
behavioural confirmation rather than as the blocking gate it was written to be.

---

## Wave: DEVOPS / [REF] Environment Matrix

Usage Data ships into every shape Lighthouse runs in, and the shapes differ in ways this feature
cares about — whether a user identity exists at all, whether an operator can set a configuration
value, and whether more than one process emits.

| Environment | Platform | Preconditions | What it changes for Usage Data |
|---|---|---|---|
| `standalone` | Windows, macOS, Linux desktop binaries | Single process, SQLite, auth impossible | One replica by definition. Consent is per browser and there is exactly one browser. Collector URL settable via `appsettings.json` |
| `docker-single` | Docker / Docker Compose | One container, SQLite or Postgres | Collector URL settable via environment variable. The reference shape for the slice-01 walking skeleton |
| `kubernetes-single` | Helm chart, Postgres-only | `replicaCount: 1` | Collector URL reachable **only after chart 0.1.16** (P4). Before that, an air-gapped tenant cannot override it |
| `kubernetes-multi` | Helm chart, Postgres-only | `replicaCount > 1`, `redis.connectionString` set | Every replica runs the emitter. The `UsageData:LastHeartbeatDay` compare-and-swap (ADR-174 point 8) is what keeps AC-04.1 true |
| `auth-off` | Any of the above | `DisabledAuthenticationHandler` active | Every caller is `lighthouse\|auth-disabled` (S11). The consent endpoints are unauthenticated **by necessity**, not by oversight |
| `auth-on` | Any of the above | OIDC or local auth configured | Consent is still per browser, not per account. A second account on the same browser inherits the decision — DISTILL needs the scenario |
| `community-licence` | Any of the above | No premium licence | The admin switch cannot be turned off (D5). Re-ask cadence applies |
| `premium-licence` | Any of the above | Valid premium licence | The switch is operable. A decline is final |
| `ci-clean` | GitHub Actions `ubuntu-latest` | No network to the collector host | Where the three hard gates run. Must pass with zero egress |

Machine-readable form: `docs/feature/epic-5733-opt-in-usage-data/environments.yaml`.

---

## Wave: DEVOPS / [REF] CI/CD Pipeline Outline

Extends the existing workflows. No new CI platform, no new runner class.

| Stage | Workflow | Trigger | Usage Data content |
|---|---|---|---|
| Backend build + test | `ci_backend.yml` (existing, called by `ci.yml`) | push to `main`, PR, `features/**` | The three hard gates: zero-leak `DelegatingHandler` assertion, payload-purity invariant, revocation-latency assertion, plus `UsageDataEmitSeamArchUnitTest`. **All run with no egress** |
| Frontend build + test | `ci_frontend.yml` (existing) | same | Dialog forbidden-phrase test (A13), indicator rendering, `usageDataEligibility.ts` |
| Docs field-list check | `ci_backend.yml`, new step | same | Compares the emitted field set against `docs/settings/usagedata.md`. Built with the first event, not the fifth |
| E2E build | `ci_e2e.yml` (existing) | same | Type-check and compile only — this workflow does **not** run Playwright |
| E2E execution | `ci_verifysqlite.yml`, `ci_verifypostgres.yml`, `ci_verifyauth.yml` (existing) | same | Where the walking-skeleton spec actually runs. **Six app-start blocks** need the collector override (see below) |
| Sonar gate | `ci_sonar_gates.yml` (existing) | PR | Unchanged |
| **Privacy canary** | **`ci_usagedata_canary.yml` (new)** | `schedule: daily` + `workflow_dispatch` | The only job that talks to PostHog. Never called from `ci.yml` |

**The canary is a separate workflow on purpose.** This repository already has the failure mode: live
connector tests talk to real Jira / Linear / ADO / ServiceNow instances, do not skip when a credential
is missing, and the shared Linear key rate-limits the next CI run — one of six resulting failures ever
names the 429. A vendor-talking assertion wired into the per-commit path buys the same class of
problem. It gets its own workflow, guarded by `if: github.repository == 'LetPeopleWork/Lighthouse'`
(the precedent is `ci_generate-update-feed.yml`).

**The canary tests carry two categories, not one:** `[Category("Integration")]` **and**
`[Category("UsageDataCanary")]`. This is the convention every network-talking test in the repository
already follows — `JiraScopedTokenIntegrationTest.cs:13-14` is the model. `Integration` is what both
`ci_backend.yml`'s computed filter (`Category!=Integration`, line 118) and the documented local filter
in `CLAUDE.md` exclude; `UsageDataCanary` is what the canary workflow selects on.

A single `UsageDataCanary` category would have excluded the canary from **nothing**: it does not match
`Integration`, so `ci_backend.yml` would have run it on every push and every PR, and `ci.yml` passes
`secrets: inherit` to that job — so once the PostHog secrets exist, the per-commit backend job would
have emitted to the vendor with live credentials. Two categories, and no `CLAUDE.md` edit is needed at
all.

**The two categories are still not enough on their own, and this is the path that matters.**
`ci_backend.yml:114` seeds `parts=("Category!=Integration")`; line 129, under `force_full`, appends
`Category=Integration`; line 138 joins them with `|`. The filter becomes
`Category!=Integration|Category=Integration` — every test, which is what "force full" means and is
correct for the connector suites. It is not correct for a test that talks to a vendor with live
credentials. And the two commits **this feature itself lands** are exactly the ones that set it: a
`Program.cs` edit hits the shared-connector whitelist, and adding `ci_usagedata_canary.yml` matches
`ci_changes.yml:157`'s `^.github/workflows/ci.*\.yml` rule, which force-sets every connector output.

So the exclusion has to survive `force_full`. Change that branch to append
`(Category=Integration&Category!=UsageDataCanary)` rather than `Category=Integration`, and **land that
change before the commit that registers the emitter**, not with it. Verify with
`dotnet test --list-tests` under the force_full filter before the canary tests are written.

### The E2E override, and why a missing one is silent

ADR-176 defaults `CollectorBaseUrl` to `https://eu.i.posthog.com`. The slice-01 walking skeleton is
consent → heartbeat → collector → revoke. Playwright does not run in `ci_e2e.yml`, which stops at
`pnpm run build`. **There are seven app starts in CI**, not the six this section first claimed:

| Where | Note |
|---|---|
| `ci_verifysqlite.yml:106` | Has an `env:` block |
| `ci_verifypostgres.yml:119` | Has an `env:` block |
| `ci_verifyauth.yml:109` (`test:auth`) | Has an `env:` block |
| `ci_verifyauth.yml:177` (`test:rbac`) | Has an `env:` block |
| `ci_verifyauth.yml:245` (`test:proxyauth`, behind a TLS-terminating Traefik proxy) | Has an `env:` block — and is the one a reader skips |
| `ci_verifywindows.yml:86` | Smoke check |
| **`ci_verifymacos.yml:145`** | `open -a /Applications/Lighthouse.app`, **no `env:` block at all** — it launches an installed bundle, so it cannot take a per-step override the way the other six can. Runs on every `ci.yml` execution |

**The compensating invariant this section originally proposed does not work, and is withdrawn.** It
said an emit to the production default would be refused "when the process is running under a test
environment". There is no such signal: `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` appear in
**no** workflow in this repository, and every app start launches the published Release binary, which
defaults to Production. The invariant would never have fired in any environment it was written for.

**Re-specified on a signal that exists, and it is configuration-shaped rather than environment-shaped:
refuse an emit to the compiled-in production default unless `CollectorBaseUrl` was explicitly
supplied.** A real deployment always supplies it — the chart renders it (P4), Docker and standalone set
it — and no CI app start does, including the macOS bundle that cannot. One assertion, no YAML, and it
covers the seventh start that no override can reach.

The per-block overrides are still worth setting where a block exists, as defence in depth: point
`UsageData__CollectorBaseUrl` at a blackhole host. Not empty — empty means "fall back to the PostHog
default", which is the failure. The precedent for the shape is `Lighthouse__OAuth__UseStubProvider:
"true"` in the same blocks. But they are no longer the guarantee: an override missing from any one of
them sends real heartbeats from CI into the production census with **no error and no red build**,
because the emit is fire-and-forget and degrades silently by specification.

Open, and owned by DISTILL: the heartbeat is daily, so an E2E run may never trigger an emit at all.
The spec needs a forced-emit seam or it asserts nothing.

### The canary's two jobs

The choice of a **separate PostHog CI project** keeps synthetic events out of the census, and costs
the control its strongest claim: a canary passing against a CI project proves nothing about the
project customers emit into. Two jobs recover it, and neither writes to production:

| Job | Project | Action | Proves |
|---|---|---|---|
| `assertion-can-fail` | CI project | Emit one event with `$geoip_disable`, one **without**, read both back | The assertion is capable of failing. Without the second event "property absent" is green forever, including on the day the guarantee breaks |
| `production-sweep` | Production project | **Read-only** query over a rolling window: zero events carry `$ip` or `$geoip_*` | The real project, on real traffic, with no synthetic rows written into the census |
| `settings-parity` | Both projects | **Read-only** project-settings query: IP capture and the GeoIP transformation are in the same state in both | That `assertion-can-fail`'s result transfers to production at all |

**With one project, two of those three jobs cannot run — stated plainly rather than quietly dropped.**
The free plan allows one project (see Pre-requisites), so until a second exists:

| Job | Status on one project |
|---|---|
| `production-sweep` | **Runs, unchanged.** Read-only against the real project on real traffic. This is the job carrying most of the value |
| `assertion-can-fail` | **Blocked.** It deliberately emits one event *without* `$geoip_disable` to prove the assertion can fail. Running that against the census would write a geo-enriched event into the dataset this Epic promises carries no location data |
| `settings-parity` | **Moot.** Nothing to compare one project against |

The cost is exactly what C4 identified and is not cosmetic: with only `production-sweep`, *"zero events
carry `$geoip_*`"* stays green forever if the vendor stops projecting the property, including on the
day the guarantee breaks. Layer 1 - the payload properties asserted against the serialised body in CI -
is unaffected and remains the layer that actually meets the standard.

One idea to reject before someone has it: emitting the positive control into production under a
synthetic `distinct_id` and deleting that person afterwards. It writes location data into the census,
even briefly, and makes a promise depend on a cleanup step that can fail silently.

Three mechanical requirements, each of which closes a way for this to pass while broken:

1. **`production-sweep` must assert a non-zero event count before it asserts cleanliness**, and report
   "no traffic" as an outcome distinct from "clean traffic". Otherwise it is green on every quiet day —
   the green-wired light C4 already caught once, arriving by a different route.
2. **The sweep window must be longer than the schedule interval.** A 24h window on a daily cron leaves
   an uncovered gap the moment one run is delayed or skipped, and GitHub's scheduler is best-effort.
   Use 30h and accept the overlap; a gap is invisible, a double-count is not.
3. **`assertion-can-fail` must poll for the read-back, bounded.** Capture-to-query at PostHog is
   asynchronous on the order of minutes, so an immediate read flakes — and ADR-176 says in its own
   words that *"a flaky scheduled job gets muted, which silently deletes this layer"*.

**Why `settings-parity` exists, and what the first draft of this section got wrong.** It claimed
`production-sweep` checks that the two projects are configured identically. It does not and cannot:
it asserts something about *events*, and a clean result cannot distinguish "production is configured
correctly" from "production's query API stopped projecting `$ip`". The positive control that
distinguishes those lives in `assertion-can-fail`, which by design runs against the *other* project.
Left there, the combined control assumed exactly the parity it claimed to be checking — circular, in
the one place this Epic can least afford it. A direct settings read is not circular: it compares the
two configurations to each other rather than inferring one from the other's traffic. It also recovers
part of "AI features are off", which ADR-176 demoted to layer 3 on the guess that the *query* API
cannot answer it; the project-settings endpoint is a different endpoint.

**Considered and not adopted: a reserved identifier in production.** ADR-176's original design used a
reserved `distinct_id` so the detecting event is synthetic, and
`OUT-usagedata-instances-reporting` counts distinct identifiers — so a reserved one is trivially
excluded from the census. That would give production a real positive control. It is not adopted
because it writes synthetic rows into the production project, which is the thing the product owner
chose against on 2026-09-11. `settings-parity` buys most of the same assurance with no write. If the
settings endpoint turns out not to expose these values, this is the fallback and the choice should be
revisited rather than the claim weakened.

### What happens when the canary goes red

A detective control has to detect *to somebody*. A scheduled workflow that fails into the Actions tab
is how ADR-176's own warning — *"a flaky scheduled job gets muted, which silently deletes this
layer"* — actually comes true.

| Outcome | Meaning | Response |
|---|---|---|
| `production-sweep` **clean**, non-zero events | Working as promised | Nothing |
| `production-sweep` **no traffic** | Nobody emitted in the window. Says nothing about the guarantee | Not a failure. Reported distinctly, and escalated only if it persists past the point where `OUT-usagedata-instances-reporting` expects traffic |
| `production-sweep` **dirty** | Events at the vendor carry an IP or geo properties | **Stop-the-line.** Turn the `UsageData` OptionalFeature off instance-wide on the vendor's own instance, check the two project settings against their recorded expected state in `docs/settings/usagedata.md` (layer 3 — this is what makes layer 2 actionable rather than merely red), and establish the window: which events, from when. Users were told this would not happen, so the disclosure question is live and belongs to the maintainer, not to CI |
| `settings-parity` **mismatch** | The two projects drifted. `assertion-can-fail`'s result no longer transfers | Re-align, then re-run both jobs before trusting either |
| `assertion-can-fail` **cannot produce an enriched control event** | The assertion has stopped being able to fail | Treat as red. This is the vacuous-pass alarm and it is the one nobody will think to look for |

**Notification: GitHub's built-in scheduled-workflow failure email to the repository owner.** Decided
2026-09-11. No workflow in this repository has any Slack or webhook integration today, so every other
option meant introducing one; this is the mechanism that already exists and costs nothing.

The residual is stated rather than glossed: this is precisely the channel ADR-176 warns about when it
says a flaky scheduled job gets muted and the layer silently disappears. Nothing here prevents that —
it depends on the maintainer continuing to read those emails. **If the canary ever starts flapping,
this is the first decision to revisit**, because a filtered email and a deleted control are the same
thing. A failure-opened GitHub issue is the fallback: durable, visible, and it needs no new secret.

---

## Wave: DEVOPS / [REF] Monitoring Contracts

| KPI | Instrument | Where | Gate? |
|---|---|---|---|
| `OUT-usagedata-zero-leak-before-consent` | `DelegatingHandler` asserting zero requests to the collector host across a full emit cycle with zero consent + ArchUnitNET rule forbidding ad-hoc `HttpClient` construction | `ci_backend.yml`, every commit | **Hard** |
| `OUT-usagedata-revocation-latency` | Assertion at the emit path: revoke, then run the emit cycle, assert no publish | `ci_backend.yml`, every commit | **Hard** |
| `OUT-usagedata-payload-purity` | Invariant over the serialised payload: closed field set, no free text, compared against `docs/settings/usagedata.md` | `ci_backend.yml`, every commit | **Hard** |
| `OUT-usagedata-instances-reporting` | Distinct `distinct_id` count, rolling 24h | PostHog production project | No |
| `OUT-usagedata-kpis-unblocked` | Count of `status: deferred-pending-telemetry-feature` rows in `docs/product/kpi-contracts.yaml` | Repository, checked at slice 04 | No |
| `OUT-usagedata-no-nag-complaints` | Community channels, GitHub issues. Manual, and correctly so — a complaint is not an event | — | No |
| `OUT-usagedata-consent-uptake` | Distinct granting browsers per reporting instance, plus grants ÷ decisions read straight out of the consent table on the dogfood instance | PostHog production project + one SQL read | No |

### `OUT-usagedata-consent-uptake` could not be measured at the collector — RESOLVED 2026-09-11

The KPI reads *"Consent grants ÷ dialogs shown, both counted at the collector."* The denominator
cannot exist. A browser that is shown the dialog and declines sends nothing — that is D3, the Epic's
own non-negotiable, and it is what `OUT-usagedata-zero-leak-before-consent` enforces as a hard CI
gate. Counting dialogs shown at the collector would require an event from browsers that refused, i.e.
the exact behaviour the feature is built to prevent. The two cannot both hold.

This is flagged rather than fixed, because the fix is a product decision:

1. **Drop the denominator.** Measure grants per reporting instance over time. Honest, no new data, no
   ratio.
2. **Count dialogs shown locally, emit the count from consenting instances only.** Gives a ratio that
   is still biased (it omits every instance where nobody consented), and adds a sixth field to a
   payload AC-02.1 enumerates to the user — which under AC-08.5 is a re-consent question and therefore
   a DoR-9 question.
3. **Leave the count instance-local and unemitted**, visible to the admin on the settings page only.
   Answers "is my instance nagging people" without sending anything.

**Decision, taken by the product owner on 2026-09-11: option (1), plus the dogfood read.** Applied to
the DISCUSS Outcome KPIs table above rather than left as a flagged contradiction — the same
back-propagation the zero-leak rescope got. Option (2) is rejected on its own terms: it spends a legal
review on a ratio that would still be biased.

**The dogfood number is free and available on day one.** The consent table records *every* decision
locally — H1 added `Revoked` as a third state, so grants, declines and revocations all have rows. On
the vendor's own instance, where slice 01's acceptance happens anyway (AC-04.6), grants ÷ decisions is
a direct SQL read: no payload change, no new field, no legal exposure. n=1 and unrepresentative, and
still strictly better than "unmeasurable" — enough to sanity-check a 20% target before slice 02's
60-day window closes. It carries the ≥ 20% half of the target; the collector carries the trend.

Option (3) — surfacing the local counts to the admin on the settings page — was not adopted. It is an
addition rather than an answer, and slice 01 is already oversized (H6). Revisit if an admin ever asks
"is my instance nagging people".

---

## Wave: DEVOPS / [REF] Deployment Strategy

Lighthouse is shipped software, not a hosted service: the deployment unit is a release artifact
(container image, Helm chart, signed standalone binaries) that a customer installs. There is no
traffic shifting to design.

- **Kubernetes**: rolling, unchanged. The chart already carries `stakater/reloader` wiring and a
  bounded drain window.
- **Rollback contract, consent table**: additive. Rolling the image back leaves a table the old code
  ignores. This is the expand-only rule working as intended.
- **Rollback contract, `AppSetting`: nothing to contract for. WITHDRAWN 2026-09-11.** This section
  previously said image rollback was *not* the recovery path, because the migration deleted rows to
  de-duplicate `AppSetting.Key` before indexing it. **`Key` is already the primary key**
  (`LighthouseAppContext.cs:101`), so duplicates have never been storable, no index is added, nothing
  is deleted, and the `DELETE` that drove all of this could never have executed. The whole paragraph
  — database-restore instruction, seeder audit, mid-flight-failure procedure — defended a state the
  schema forbids. See ADR-175 point 4's correction.

  **The migration is additive: it adds the consent table and nothing else.** Image rollback is a
  supported recovery path again, exactly as the expand-only rule intends, and the rolled-back image
  ignores a table it does not know about.

  Worth recording how this survived four waves: DESIGN invented the risk without opening the
  `DbContext`, its own adversarial review sharpened it into a bricked-upgrade hazard (H4), this band
  inherited it and made the consequence more severe, the DEVOPS review verified that rewrite as
  holding, and DISTILL committed a Testcontainers dependency partly on its basis. Five checks, one
  unchecked premise. The fix is one `grep` nobody ran.
- **The real hazard is smaller and needs a test, not a migration.** `AppSetting.Id` is not the key and
  carries no constraint; `UpsertSetting` mints every new row with the default `Id = 0`; and
  `AppSettingSeeder.RemoveObsoleteSettings` deletes **by `Id`**. Its `obsoleteIds` list starts at 9, so
  the instance identifier is safe today — but a low id added to that list later would delete it
  silently, and the instance would mint a new one and re-enter the census as a second instance.
- **Chart**: 0.1.16 adds one value (P4). Additive, no breaking change, no operator action required to
  keep an existing install working.

---

## Wave: DEVOPS / [REF] Mutation Testing Strategy

**per-feature**, unchanged — already the project standard and already in `CLAUDE.md`. Kill rate ≥ 80%
on the consent and emit paths, both stacks, per DoD item 5.

One scoping note: the Stryker configuration traps in this repository are known — the .NET runner
ignores line-span and whole-file `mutate` globs, and StrykerJS has left `@ts-nocheck` in hundreds of
files. Scope the run to the Usage Data paths explicitly and check the mutant count before trusting the
score.

---

## Wave: DEVOPS / [REF] Observability Stack

Unchanged: OpenTelemetry → Prometheus scrape plus structured JSON logs, enabled by
`telemetry.enabled` in the chart, off by default. **This is the surface D1 renamed around and Usage
Data is deliberately not part of it.**

Inside the customer's instance, Usage Data contributes:

- **Structured log on suppression.** When the gate refuses, one debug-level line naming which
  condition refused (master switch off, no live grant, no identifier). This is the admin's evidence
  in a security review and costs nothing when nothing is being sent.
- **No log line ever carries the instance identifier or the consent token.** `ISystemInfoService`'s
  own doctrine — that a fourth field added later is withheld by the sentence that withheld the first
  three — applies to logs as well as to the API.
- **Recommended, not required: one counter** `lighthouse_usagedata_emits_total{result=sent|suppressed}`
  on the existing Prometheus surface. No identifier, no payload, no per-browser dimension. It lets an
  admin answer "has this instance sent anything" from their own monitoring rather than from our word.
  Nothing in the delta asks for it; drop it if slice 01 is tight, since H6 already found the slice
  oversized.

---

## Wave: DEVOPS / [REF] Branching Strategy

Trunk-based on `main`, unchanged. Commits push directly; `ci.yml` runs on every push to `main` and on
PRs. No feature branch is created for this Epic.

The relevant consequence for Usage Data is the project's own green-before-push convention: the
slice-04 cache invariant has no test until slice 04, and DESIGN's recommendation is to write it now,
skipped, with the un-skip condition named. A skipped Vitest `describe` still evaluates its body, so
the scaffold call must not be hoisted into it or the suite reports BROKEN rather than pending.

---

## Wave: DEVOPS / [REF] Coexistence Matrix

| Thing | Must keep working | Why it is at risk here |
|---|---|---|
| Live connector tests (Jira / Linear / ADO / ServiceNow) | Yes | Registering the emitter edits `Program.cs`, which `ci_changes.yml` treats as a shared-connector change and expands the filter to `Category=Integration` — the full live-connector run, with its flake and rate-limit exposure. Accepted, and worth spending in **one** commit rather than five |
| `Telemetry:` configuration section | Yes | Namespace-disjoint by decision (D1). Nothing new may be named `Telemetry*` |
| `DeltaSync` optional feature | Yes | It is not premium. Slice 03 must verify it stayed ungated when the premium refusal became real |
| Existing `OptionalFeaturesController` callers | Yes | AC-07.1 changes the response contract. Extend the test factory before touching it |
| `GitHubService` release check | Yes | Unchanged; the docs page names it as a separate outbound call (D11) |
| Focused-commit convention | Yes | `ci_changes.yml:157` force-sets **every** connector output to true when any `.github/workflows/ci*.yml` file changes — and `ci_usagedata_canary.yml` matches that pattern. So adding the canary workflow triggers the full live-connector run too, a second time, unless it lands in the same commit as the `Program.cs` registration. One commit or two full runs; pick deliberately |
| Helm installs on chart ≤ 0.1.15 | Yes | 0.1.16's new value is additive with a default; an install that never sets it behaves as today |
| Local `dotnet test` with no secrets | Yes | Carried by the `Integration` category the canary tests also wear (P6) — already in the documented filter, so nothing new to remember |
| The migration on an upgraded customer database | Yes | Additive only — it adds the consent table. The `AppSetting.Key` dedup that this row previously warned about was withdrawn on 2026-09-11: `Key` is already the primary key |
| The obsolete-settings sweep | Yes | `AppSettingSeeder.RemoveObsoleteSettings` deletes by `Id`, and every row this feature mints carries `Id = 0`. Safe today; guard it with a test |

---

## Wave: DEVOPS / [REF] Pre-requisites

1. ~~**A PostHog Cloud EU organisation with two projects**~~ — **not available, found 2026-09-12.**
   The free plan allows **one project per organisation**; a second requires card details, which moves
   the organisation onto a paid plan.

   **Decided: stay on one project and stay free.** Paying for the second project would move event
   retention from one year to seven, and PostHog does not let a retention period be shortened
   afterwards - so it is a one-way door on the exact number the consent dialog states, bought to
   obtain a drift detector for configuration that cannot drift until something is emitting. The
   canary's value begins when slice 01b ships, not now.

   Worth five minutes before accepting the degradation: PostHog says Cloud users may *"create,
   manage, and join organizations without limits"*, so a **second free organisation** may carry its
   own one-project allowance. If it does, the canary gets its project at no cost and none of the
   below applies. Unverified.
2. **Credentials, which are not four peers.** They have very different blast radii and must be
   handled differently:

   | Credential | What it is | Worst case |
   |---|---|---|
   | `POSTHOG_CI_PROJECT_API_KEY` | A PostHog **project** write key for the CI project. Public by design — these ship in browser bundles | Junk events in a project nobody reads |
   | `UsageData:ProjectApiKey` (production) | The same kind of key, for the census project — but this one **ships inside every Lighthouse install**, because an instance cannot emit without it | **Anyone who reads it out of a config file can write to the census.** Nothing can be read with it, so this is not a confidentiality loss; it is a measurement one. See the integrity note below |
   | `POSTHOG_PERSONAL_API_KEY` | A **user-scoped** read credential | **The whole census.** Every instance identifier, version, licence tier and deployment mode, for every consenting instance |
   | `POSTHOG_PROD_PROJECT_ID` / `POSTHOG_CI_PROJECT_ID` | Identifiers | Not secrets. Storing them as secrets buys nothing and makes the rotation story four items long instead of one |

3. **The personal key's blast radius is a DoR-9 question, not a CI detail.** It reads the dataset this
   Epic spends ADR-175 making unguessable, and two properties of this repository make that sharper:
   `ci.yml` passes `secrets: inherit` to thirteen called workflows, so a repo-level secret is in scope
   for jobs with no business holding it; and the project is trunk-based with direct pushes to `main`,
   so a workflow that reads a secret can land without a PR in the path. Required before the canary is
   wired:
   - **Store it as a GitHub Environment secret**, not a repository secret, and give the canary workflow
     an explicit `secrets:` mapping — never `secrets: inherit`.
   - **Custody, decided 2026-09-11: the maintainer holds it, and it rotates on suspicion rather than
     on a calendar.** Chosen deliberately over a quarterly cadence, because a documented interval
     nobody keeps is worse than an honest one — it reads as a control while providing none. The
     residual: a leaked key stays valid until somebody notices, and nothing prompts a look. What makes
     that survivable is the narrowing below, not the rotation policy.
   - **Scope it as narrowly as PostHog allows.** If a saved insight can answer "how many events, and do
     any carry `$ip`" under a narrower scope than event-level read, use that instead.
   - **Route it into DoR-9** beside the DPA read. "Who else can read the census, and how" is the same
     class of question as retention and erasure, which ADR-175 point 7 already sends there.
4. **Two things about the production project key that had not been written down**, both found on
   2026-09-12 while the projects were being created.

   **How it reaches an install is undecided.** The key has to be present in every deployment or
   nothing emits, and the chart carries only `collectorBaseUrl` (P4) — not the key. So it arrives
   either as a committed default in `appsettings.json` or injected at build or release time, and
   nobody has chosen. Decide it in slice 01b, and note that the committed-default route meets this
   repository's own secret-scanning pre-push hook, which will have an opinion about a vendor key in a
   tracked file.

   **The census can be written to by its own audience, and that is accepted rather than fixed.**
   A shared key that ships to customers authenticates nothing: anyone running Lighthouse can read it
   out of their own config and post events with any `distinct_id` they like. There is no
   confidentiality loss — the key writes, it does not read — but every number this Epic produces is a
   count of events written with a credential the audience holds.

   It is accepted because the alternatives are disproportionate to what is being protected: a
   per-instance credential means an enrolment service and a registry of installs, which is a larger
   privacy surface than the census it would defend, and signed payloads need a key that also ships.
   What is required instead is that the number be read knowing this. `OUT-usagedata-instances-reporting`
   asks for ≥ 25 distinct identifiers in a rolling 24 hours — a threshold one bored person could
   manufacture in an afternoon. Treat a sharp jump, or a population of identifiers that never report a
   second time on a plausible cadence, as a reason to look rather than a result. Same posture as the
   unbound embed nonce: named, sized, accepted, and detectable.
5. **Network access from the runner to `eu.i.posthog.com`.** Worth stating plainly: GitHub-hosted
   `ubuntu-latest` runners have unrestricted egress, so there is no per-workflow allowance to grant and
   nothing enforces "the canary workflow only". What actually keeps every other job off the vendor is
   the `Integration` category exclusion. If real egress control is wanted, it needs a self-hosted
   runner with an egress policy — and that is a decision nobody has taken.
6. **Chart 0.1.16 released** before a Kubernetes tenant can be told the collector host is
   configurable (P4). This is a **separate release train**: `chart/**` is not in `ci.yml`'s
   `push.paths`, and `ci_chart.yml` publishes behind a `Release` environment gate. It does not ship
   with the feature commit.
7. ~~**DoR-9 closed** before slice 01 ships.~~ **Closed 2026-09-12** — all six questions answered; see
   the DoR Validation section.

---

## Wave: DEVOPS / [REF] Decisions

| ID | Decision | Rationale |
|---|---|---|
| P1 | The canary runs against a **separate PostHog CI project**, and is split into `assertion-can-fail` (CI project, writes) and `production-sweep` (production project, read-only) | Product owner's call on 2026-09-11. Keeps synthetic rows out of the census without giving up the claim that the *production* project honours the arrangement |
| P2 | `production-sweep` must assert a non-zero event count before asserting cleanliness, and report "no traffic" distinctly from "clean traffic" | Otherwise it is green on every day nobody emits — the vacuous-pass failure C4 already caught once in this design |
| P3 | The canary lives in a new `ci_usagedata_canary.yml`, daily schedule plus `workflow_dispatch`, repository-guarded, never called from `ci.yml` | The live-connector precedent in this repository: vendor-talking tests in the per-commit path rate-limit the next run and produce failures that read as regressions |
| P4 | Chart **0.1.16** adds `app.usageData.collectorBaseUrl` → `UsageData__CollectorBaseUrl`, emitted **conditionally** with `{{- with }}` | The chart has no generic `extraEnv` passthrough. The precedent is `app.timeZone`, not `app.embed.enabled`: `Embed__Enabled` is emitted unconditionally because `false` is a real value, whereas an unconditionally emitted **empty** collector URL would override the appsettings default and break every default install — silently, because the emit is fire-and-forget. Without the value at all, the air-gap claim is false on Kubernetes, the one shape an air-gapped customer is most likely to be running |
| P5 | The three hard gates run in the normal backend suite with **zero egress**; the canary is never a merge gate | A gate that needs a vendor to be up is not a gate. The canary is a detective control and is allowed to be late |
| P6 | Canary tests carry **both** `[Category("Integration")]` and `[Category("UsageDataCanary")]`, **and `ci_backend.yml`'s `force_full` branch must append `(Category=Integration&Category!=UsageDataCanary)`** | The two-category convention (`JiraScopedTokenIntegrationTest.cs:13-14`) covers the default filter and needs no `CLAUDE.md` edit. It does **not** cover `force_full`, where the filter becomes `Category!=Integration\|Category=Integration` — every test — and the commits this feature lands (a `Program.cs` edit; adding any `ci*.yml`) are exactly what sets it. Corrected 2026-09-11: the first version of this decision closed only half the paths, on the one that mattered least |
| P7 | The Prometheus counter is **recommended, not required** | Useful to an admin, but nothing in the delta asks for it and slice 01 is already oversized (H6) |
| P8 | `docs/product/kpi-contracts.yaml` is **not** updated in this wave | The seven deferred KPIs move to a live source at slice 04 per AC-08.6. Writing instrumentation now would name a measurement source that does not yet exist — the kind of claim this Epic exists to stop making |
| P9 | Project parity is checked by a **third job reading both projects' settings**, not inferred from production's traffic | `production-sweep` asserts a property of events. A clean result cannot tell "production is configured correctly" from "production's query API stopped projecting `$ip`". Inferring parity from it assumed the thing it claimed to check |
| P10 | The canary's failure states are enumerated with a response each; notification is **GitHub's built-in scheduled-workflow failure email** to the repository owner | ADR-176 warns that a flaky scheduled job gets muted, which deletes the layer. No workflow here has Slack or webhook integration, so every alternative meant adding one. Chosen 2026-09-11 with the residual named: this is the channel that gets filtered, and a failure-opened issue is the fallback if the canary flaps |
| P11 | The `POSTHOG_PERSONAL_API_KEY` is an Environment-scoped secret with an explicit `secrets:` mapping — never `secrets: inherit` — held by the maintainer and **rotated on suspicion, not on a calendar**; its blast radius goes to DoR-9 | It reads the entire census: every instance identifier ADR-175 spends four alternatives making unguessable. `ci.yml` inherits secrets into thirteen workflows and this project pushes straight to `main`, so "who else can read this" is not hypothetical. A quarterly cadence was offered and declined on 2026-09-11 — an interval nobody keeps reads as a control while providing none |
| P12 | **The backend refuses an emit to the compiled-in production collector host unless `CollectorBaseUrl` was explicitly supplied.** The per-block overrides remain as defence in depth | Corrected 2026-09-11. The original invariant keyed on "running under a test environment", and there is no such signal — `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` appear in no workflow and every app start runs the published Release binary as Production, so it would never have fired. There are also **seven** app starts, not six, and `ci_verifymacos.yml:145` has no `env:` block at all. A configuration-shaped guard covers the one no override can reach |
| P13 | `OUT-usagedata-consent-uptake` drops its denominator; the one true ratio is read out of the consent table on the dogfood instance | The original asked for grants ÷ dialogs shown at the collector, and a declining browser sends nothing. Decided 2026-09-11 and applied to the DISCUSS table, rather than left flagged |
| P14 | **The canary asserts on counts and property names only — never on event bodies or `distinct_id`s.** Failure messages carry a count and a time window; event-level forensics happens in the PostHog UI, not in CI output | This is a **public repository**: Actions logs need no credentials to read and are retained by default. `production-sweep` queries the census with a read key, and P10's dirty-response instruction ("establish the window: which events, from when") invites printing exactly the instance identifiers ADR-175 spends four alternatives making unguessable — a privacy incident inside the control built to prevent one. Whether census data may appear in a public CI log at all is now a DoR-9 question |

---

## Wave: DEVOPS / [REF] Changed Assumptions

**ADR-176, layer 2, original wording:** *"A CI job, on a schedule rather than per-commit, emits one
event with a reserved `distinct_id` and then reads that same event back through PostHog's query API"*
— a single job against a single project.

**New:** two jobs against two projects (P1). The split is forced by the choice of a separate CI
project: a write-and-read-back cycle in a project no customer emits into demonstrates that project's
configuration and nothing else. `production-sweep` is what carries the claim ADR-176 actually makes.

**ADR-176 says nothing about the Helm chart.** It states the collector base URL is configurable and
treats that as sufficient for the air-gap requirement inherited from #5015. On Kubernetes it was not:
the chart cannot reach the setting. P4 closes it. This is an addition to ADR-176's scope, not a
contradiction of it.

Both are recorded as an amendment in
`docs/product/architecture/adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md`.

**Back-propagation to DISCUSS — APPROVED and applied 2026-09-11.**
`OUT-usagedata-consent-uptake` was unmeasurable as written (see Monitoring Contracts): grants ÷
dialogs shown *both at the collector*, where a declining browser sends nothing by design. The product
owner took option (1) — drop the ratio centrally, measure grants per reporting instance — plus the
dogfood SQL read for the one place a true ratio exists. The DISCUSS Outcome KPIs table is updated, the
same way the zero-leak rescope was.

---

## Wave: DEVOPS / [REF] Open Questions Deferred

**To DISTILL (`nw-acceptance-designer`)**

- `environments.yaml` names twelve environments; five axes change behaviour rather than packaging:
  `auth-off` vs `auth-on`, `community` vs `premium`, `kubernetes-multi`, **SQLite vs Postgres**, and
  **fresh install vs `upgrade-from-pre-5733`**. Parametrise over those five, not over all twelve.
  Provider is an axis and not packaging for one reason: EF InMemory does not enforce unique indexes,
  so the get-or-create concurrency test passes vacuously on it.
- `air-gapped-self-collector` is the environment the chart change exists for, and it had no scenario
  anywhere until now. `ci-clean` is not a substitute — it tests "no collector", not "a different one".
- The `auth-on`, two-accounts-one-browser case has no scenario anywhere yet: consent is per browser,
  so the second account inherits the first's decision. Correct per D2, and it needs to be written
  down as intended before someone files it.
- `production-sweep`'s "no traffic" state needs its own assertion, not a skip.

**To DELIVER**

- Create the two PostHog projects and record their settings in `docs/settings/usagedata.md` before the
  canary workflow is written, or the first run asserts against nothing.
- Chart 0.1.16 also needs the tenant values wiring in the private platform repository — a separate
  repository, and therefore a separate change that is easy to forget until a tenant asks for it.
- The `Program.cs` registration edit expands CI to the full live-connector run, and so does adding
  `ci_usagedata_canary.yml`. Land them together or spend two full runs.
- **Generate the migration with the existing `CreateMigration` PowerShell script**, not
  `dotnet ef migrations add`. Both `Lighthouse.Migrations.Sqlite` and `Lighthouse.Migrations.Postgres`
  need it, and the migration DLLs are HintPath references — build them before running anything that
  loads them.
- Confirm the repository owner actually receives GitHub's scheduled-workflow failure emails before
  relying on them — send one deliberate failure and check it arrives (P10). An alerting path nobody has
  ever seen fire is an assumption, not a control.

**Unchanged upstream**

- DoR-9 is still open and still blocks slice 01 shipping. It now also covers a second PostHog project,
  the personal-key blast radius (P11), and, if the product owner picks option (2) above, a sixth
  payload field.

---

## Wave: DEVOPS / [REF] Peer Review and Revisions

Reviewed 2026-09-11. **Verdict: NEEDS_REVISION** — 8 blocking. Every blocking finding was verified
against the repository before being accepted; all 8 were correct, and three of them were claims in the
first draft that were simply false. Fixed in place. What follows is what changed.

### Corrected — claims that were untrue as written

| # | Finding | Correction |
|---|---|---|
| 1 | *"Tests carry `[Category("UsageDataCanary")]` so a bare `dotnet test` never reaches the network."* `ci_backend.yml:118` computes its filter as `Category!=Integration` — a canary-only category matches nothing and would have run on **every push and PR**, with `ci.yml` passing `secrets: inherit` to that job. A category alone stops nothing locally either | Two categories, `Integration` + `UsageDataCanary`, which is what every network-talking test here already does. P6 rewritten; the `CLAUDE.md` edit it proposed is no longer needed at all |
| 2 | The CI/CD table put E2E in `ci_e2e.yml`. That workflow **builds** the suite; Playwright runs in `ci_verifysqlite.yml`, `ci_verifypostgres.yml` and `ci_verifyauth.yml`, with a fourth app start in `ci_verifywindows.yml`. "Collector host pointed at a stub" was one line standing in for **six app-start blocks** — and a missing one emits real heartbeats into the production census with no error and no red build | Table corrected, the six blocks named, and the override backed by a backend invariant (P12) rather than six YAML lines somebody must remember |
| 3 | *"The parity is what `production-sweep` exists to check rather than assume."* It cannot. The sweep asserts a property of events and cannot distinguish "production is configured correctly" from "production's query API stopped projecting `$ip`" — the control that would distinguish them runs against the other project by design. The claim assumed the parity it said it was checking | Third job, `settings-parity`, reading both projects' settings directly (P9). Corrected in the ADR-176 amendment too |

### Corrected — defects

| # | Finding | Correction |
|---|---|---|
| 4 | **The rollback contract called a row-deleting migration "additive".** ADR-175 point 4 has the dedup keep the lowest `Id` per key and delete the rest; a `Down` cannot restore them | **Fixed, then the whole thing was retracted 2026-09-11.** The original wording was right by accident — the migration *is* additive, because there is no dedup: `Key` is already the primary key. My "fix" replaced a correct statement with an alarming and false one, and the next review verified the false version as holding. Both are now withdrawn |
| 5 | **The canary had no alerting, owner or runbook.** ADR-176 warns that a flaky scheduled job gets muted and the layer disappears; a red build routed to nobody is that outcome arriving quietly | Failure states enumerated with a response each, `dirty` as stop-the-line with the disclosure question named, and notification made a prerequisite (P10) |
| 6 | **The `POSTHOG_PERSONAL_API_KEY` blast radius was never assessed** — it reads the entire census, `ci.yml` inherits secrets into thirteen workflows, and this project pushes straight to `main` | Credentials split by blast radius, Environment-scoped secret with explicit mapping, holder and rotation required, routed into DoR-9 (P11) |
| 7 | **`environments.yaml` omitted air-gapped, upgrade and rollback**, and collapsed the provider axis that H4 and ADR-175 both require — contradicting the band's own instruction to DISTILL | Three environments added, `database_providers` made an explicit axis, DISTILL handoff corrected from three axes to five |
| 8 | **P4 cited the wrong chart precedent.** `Embed__Enabled` is emitted unconditionally because `false` is meaningful; an unconditionally emitted **empty** collector URL overrides the appsettings default and breaks every default install, silently | `{{- with }}`, following `app.timeZone`. Corrected in P4 and in the ADR amendment |

### Accepted and applied without argument

- The sweep window is 30h, not 24h — a window equal to the interval leaves a gap the moment one run
  is delayed, and GitHub's scheduler is best-effort.
- `assertion-can-fail` polls for its read-back, bounded. ADR-176 required this and the first draft
  dropped it on the way into the implementable artifact.
- Adding `ci_usagedata_canary.yml` *also* trips `ci_changes.yml:157`'s workflow-file rule and forces
  the full live-connector run. Land it with the `Program.cs` commit or spend two.
- The `CreateMigration` script and the two migration projects belong in the DELIVER handoff.
- `OUT-usagedata-consent-uptake` has one free measurement the analysis missed: grants ÷ decisions is a
  local SQL read on the dogfood instance, where slice 01's acceptance already happens. n=1, and better
  than "unmeasurable". The decision also now has a gate — slice 02's start.
- "Undocumented until now" about the GitHub release check was wrong: D11 documented it in DISCUSS, two
  waves ago.
- Pre-requisite 4 was written as an infrastructure control. GitHub-hosted runners have unrestricted
  egress; what actually keeps other jobs off the vendor is the category filter. Reworded to say so.
- The four "secrets" are not peers — two of them are identifiers and not secrets at all.

### Noted, not changed

- **The three hard gates are detective, not preventive, on a direct-push trunk.** CI runs after the
  commit is on `main`, so a zero-leak regression is caught once it has landed. True, and a property of
  the branching model rather than of this feature. Worth branch protection on `main` for these three
  specifically; that is a repository decision, not this Epic's to take.
- **The Prometheus counter stays recommended-not-required**, with the reviewer's observation recorded:
  it is the only proposed control that gives the *customer* independent evidence rather than the vendor
  checking itself. Revisit at slice 03 rather than dropping.

---

## Wave: DISTILL / [REF] Distill decisions

Wave: DISTILL. Date: 2026-09-11. Density: lean, Tier-1 only — DISTILL declares no `ask-intelligent`
triggers, so no expansion menu was offered. DISCUSS D1-D11, DESIGN A1-A14 and DEVOPS P1-P13 are
constraints here, not options. Numbering starts at DT-1 so it collides with none of them.

| ID | Decision | Implements |
|---|---|---|
| DT-1 | **Slice 01 only.** Product owner's call, 2026-09-11. Slices 02-04 are separate ADO Stories and re-enter DISTILL when they start. Slice 01 is already oversized (H6), and DoR-9 could still change slice 04's payload — authoring its tests now would be writing against a spec legal has not seen. | — |
| DT-2 | **No Gherkin, no `.feature` file, no `__SCAFFOLD__` markers.** The project's ATDD policy says in its own preamble that the Python-pilot artifacts do not apply here. Skip markers are NUnit `[Ignore]` and Vitest `describe.skip`/`it.skip`. A scenario's identifier is its test name. | — |
| DT-3 | **No production scaffold types on the backend.** C# is compiled: a test naming `UsageDataConsent`, `IUsageDataGate` or `PostHogUsageDataPublisher` breaks the **whole** test assembly's build — BROKEN, the exact classification the scaffold rule exists to prevent, and a zero-warning-gate failure besides. Backend ATs are black box over HTTP and name only types that exist today. Precedent: `epic-5146-jira-forge-app`, same reasoning. | — |
| DT-4 | **Frontend scaffolds are real modules that throw, and the throw interpolates its arguments.** `noUnusedParameters` is on, so a stub ignoring its props does not compile, and underscore-prefixing would force a rename in DELIVER. Interpolating satisfies the compiler and makes the failure name the missing contract and what it was asked. Precedent: `story-5914`. | — |
| DT-5 | **Every scaffold call sits inside a test body, never at describe scope.** `describe.skip` still *evaluates its describe body*; a hoisted call throws during collection and the file reports as a failed **suite** — BROKEN, not pending. Verified by running: 5 files, 2 skipped, zero failed suites. | — |
| DT-6 | **The store-level guarantees are specified here and authored in DELIVER.** Conditional revoke and the throttled liveness touch need the entity, which DT-3 forbids scaffolding. **Corrected 2026-09-11:** this row also named the `AppSetting.Key` dedup (withdrawn — `Key` is already the primary key) and the multi-replica heartbeat CAS, which is **not specified at all**: ADR-174 point 8 puts it on `AppSettingService`, whose `UpsertSetting` is read-then-write and cannot perform a compare-and-swap. DISTILL was deferring to a specification that does not exist. It is now an open DESIGN question, not a DELIVER task. | A1, A2 |
| DT-7 | **The consent store runs on real Postgres, not SQLite or EF InMemory.** Product owner's call, 2026-09-11. Revoke and the touch are conditional updates read through an affected-row count, which EF InMemory cannot express. This reason survives the `AppSetting.Key` withdrawal untouched — it never depended on it. Cost recorded rather than buried: `requires-docker` carries no `Integration` category, so **nothing filters these** — they run on every push and need Docker locally, which contradicts this band's own coexistence row and is flagged for reconciliation. | A2 |
| DT-8 | **The indicator's two states differ by accessible name, not by colour.** Asserted directly (`aria-label` of one state ≠ the other), which is how AC-01.2's greyscale-safety becomes testable at all. A colour-only signal would be assertable only by computed style — brittle, and inaccessible in the way the AC exists to prevent. | AC-01.2 |
| DT-9 | **The indicator fails closed: `unknown` renders as not-sending.** Named explicitly because the adjacent, obvious thing to copy is `useRbac`, which fails **open** via `PERMISSIVE_SUMMARY` on purpose. A privacy indicator guessing "sending" when it cannot tell is alarming and wrong; guessing "not sending" is only wrong. | AC-01.4 |
| DT-10 | **The dialog takes `willAskAgain` as a boolean, never a licence tier.** It is the shape C5 settled for the `/state` response, and the dialog's props mirror it so the component never learns what a licence is. | C5, D5 |
| DT-11 | **The forbidden-phrase test is six patterns, not one.** A13 forbids claiming the IP is not transmitted; the same sentence can be written six ways, and "completely anonymous" and "we cannot identify you" are the two that would slip past a single-pattern check while being the most damaging to get wrong. | A13 |
| DT-12 | **The SurveyNudge correction is a skipped test on the shipped component, not a copy edit.** It asserts the popup no longer promises Lighthouse never tracks you. It fails today — deliberately. Making the copy change now would be DELIVER's work landing in DISTILL, and shipping the change before the feature exists would make the product *understate* itself instead of overstating it. | S3 |
| DT-13 | **No Playwright work in this wave.** The E2E walking skeleton is specified below and written in DELIVER. Three standing repo rules collide otherwise: never commit an unrun spec or page-object locator, never push red, and `pnpm build` runs `tsc -b`, so a spec calling a page-object method nobody has written fails the build for everyone. | — |

---

## Wave: DISTILL / [REF] Scenario list

**51 scenarios: 40 frontend (`describe.skip` / `it.skip`), 11 backend (`[Ignore]`).** No `.feature`
file exists (DT-2), so a scenario's identifier is its test name. Tags are notional — this repo has no
tag runner; they are here for the traceability the wave contract asks for.

The count grew from 39 after the consolidated wave review: AC-02.2 (the never-sent categories, stated
positively), AC-02.4 (the docs link), AC-02.5 and AC-02.6 (nothing written to the browser before the
click) were all authorable here and were missing, and several matchers were split so a positive case
and its negation are asserted separately rather than by one pattern that matched both.

### `src/components/UsageData/UsageDataIndicator.test.tsx` — 7 tests, NEW

| Scenario | Tags |
|---|---|
| says it is sending, in words rather than in colour | `@US-01` `@AC-01.1` |
| says it is not sending, in words rather than in colour | `@US-01` `@AC-01.1` |
| tells the two states apart by their accessible name, so the difference survives greyscale | `@US-01` `@AC-01.2` `@a11y` |
| says nothing is being sent when it could not find out, because a privacy control fails closed | `@US-01` `@AC-01.4` `@error` |
| is there whether or not anything is being sent, because absence answers nothing | `@US-01` `@D7` |
| reopens the decision when it is clicked | `@US-03` `@AC-03.1` |
| reopens the decision from the not-sending state too, so a refusal can be changed | `@US-03` `@AC-03.1` `@edge` |

### `src/components/UsageData/UsageDataDialog.test.tsx` — 20 tests, NEW

| Scenario | Tags |
|---|---|
| names each of the five fields as something that would be sent (5 cases) | `@US-02` `@AC-02.1` |
| names who would hold the data, not just that it is sent | `@US-02` `@AC-02.3` |
| says where the data would rest, in a place a reader can check | `@US-02` `@AC-02.3` |
| never claims the IP is not transmitted / not seen / never leaves the machine / no data leaves / completely anonymous / we cannot identify you (6 cases) | `@US-02` `@AC-02.2` `@A13` |
| tells a reader who will be asked again that they will be asked again | `@US-02` `@AC-02.9` `@D5` |
| tells a reader who will not be asked again that this is the last time | `@US-02` `@AC-02.9` `@D5` |
| does not say it will ask again to someone it will never ask again | `@US-02` `@AC-02.9` `@error` |
| reports a grant when the reader agrees | `@US-02` `@AC-02.4` |
| reports a refusal when the reader declines | `@US-02` `@AC-02.4` |
| offers a way out that is not a decision, because a dialog nobody can leave is a dark pattern | `@US-02` `@edge` |
| renders nothing at all when it is closed | `@US-02` `@edge` |

### `src/components/SurveyNudge/SurveyNudge.test.tsx` — 1 test appended

| Scenario | Tags |
|---|---|
| does not promise that Lighthouse never tracks how you use it | `@S3` `@blocking` |

### `Lighthouse.Backend.Tests/Integration/UsageData/UsageDataConsentEndpointsTests.cs` — 11 tests, NEW

| Scenario | Tags |
|---|---|
| GetState, with no token at all, answers rather than refusing | `@US-01` `@driving_port` |
| GetState, with an unknown token, answers exactly as it does with no token | `@US-01` `@security` `@oracle` |
| GetState tells the browser whether it will be asked again, without naming the licence | `@US-02` `@C5` `@security` |
| GetState forbids caching | `@US-01` `@Q1` `@error` |
| PostConsent, when the browser agrees, mints a token for it | `@US-02` `@AC-02.4` |
| PostConsent, when the browser declines, also mints a token | `@US-02` `@D2` `@edge` |
| PostConsent mints a different token every time | `@US-02` `@security` |
| PostConsent is rate-limited | `@US-02` `@H3` `@security` |
| DeleteConsent, with the browser's own token, stops the instance sending | `@US-03` `@AC-03.2` `@D8` |
| DeleteConsent, with a token this instance never minted, answers exactly as a real revoke does | `@US-03` `@security` `@oracle` |
| The consent endpoints require no authentication, because most instances have none | `@US-01` `@S11` |

---

## Wave: DISTILL / [REF] WS strategy

**Walking skeleton: specified, authored in DELIVER (DT-13).** DISCUSS chose strategy A — skeleton
first — because nothing in Lighthouse has ever sent a product event to a vendor collector. That
remains right, and the skeleton is still the first thing DELIVER builds; what this wave cannot do is
write its Playwright spec, because a spec whose page-object methods do not exist fails `tsc -b` and
therefore everyone's build.

The skeleton, for DELIVER: a lead opens the app against seeded demo data, sees the indicator saying
nothing is being sent, clicks it, reads the five fields in the dialog, agrees, sees the indicator
change, and clicks again to revoke. One spec, one new page object, driven through the production
composition root — and the collector host pointed at a blackhole (P12), because the default is
production and a leaked emit lands in the census.

---

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| `IUsageDataPublisher` → `PostHogUsageDataPublisher` | **DELIVER** | Needs the type (DT-3). Capturing fake per the ATDD policy; the real publisher may never resolve in a test |
| Consent persistence → `UsageDataConsentRepository` | **DELIVER** | Needs the entity (DT-3, DT-6). `Testcontainers.PostgreSql`, `[Category("requires-docker")]` |
| Instance identifier → `AppSettingService` | **DELIVER** | The get-or-create race (arbitrated by the existing primary key, no migration), and the guard that `AppSettingSeeder`'s by-`Id` sweep leaves the identifier alone |
| Deployment-mode signal → `UsageDataDeploymentModeResolver` | **DELIVER** | Needs the type. The Kubernetes case is the whole point: `IsDocker()` is true in a pod |
| The three HTTP endpoints (driving) | **YES — this wave** | `UsageDataConsentEndpointsTests`, real ASP.NET host via `IntegrationTestBase` |
| Footer indicator, consent dialog (driving) | **YES — this wave** | Real components rendered through RTL, no shallow rendering, no component mocking |

Four of six adapters land in DELIVER. That is a consequence of DT-3, not an omission, and it is the
single largest thing a reader should take from this band: **DISTILL could author the driving side and
not the driven side.** Each row above names what is owed and the mechanism it is owed under.

---

## Wave: DISTILL / [REF] Scaffolds

Two frontend modules, both new files, neither touching shipped code:

| Scaffold | Contract it declares |
|---|---|
| `src/components/UsageData/UsageDataIndicator.tsx` | `UsageDataSendingState` (`sending` / `not-sending` / `unknown`) + `UsageDataIndicatorProps` |
| `src/components/UsageData/UsageDataDialog.tsx` | `UsageDataDecision` (`granted` / `declined`) + `UsageDataDialogProps` including `fields`, `collectorName`, `dataResidency`, `willAskAgain` |

Both throw a message naming the function and interpolating its arguments (DT-4). No backend scaffolds
(DT-3). No `__SCAFFOLD__` marker — the policy retires it for this repo; `grep -rn "is not implemented"
Lighthouse.Frontend/src/components/UsageData` is the equivalent progress check, and it must return
nothing when slice 01 is done.

---

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Frontend/src/components/UsageData/` — colocated `*.test.tsx` beside the component, the
  convention the ATDD policy records for React component ATs.
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Integration/UsageData/` — a new folder beside
  `Integration/Containers/`, matching where black-box `WebApplicationFactory` ATs live. The
  Postgres-backed store tests DELIVER writes belong in `Integration/Containers/` instead, with the
  rest of the `requires-docker` set.
- `SurveyNudge.test.tsx` — appended in place. The assertion is about that component's copy, and it
  belongs where a reader changing the copy will see it.

---

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving port from DESIGN | Exercised by |
|---|---|
| `GET /api/latest/usagedata/state` | `UsageDataConsentEndpointsTests` — real HTTP through the real host, including the two oracle assertions and `Cache-Control: no-store` |
| `POST /api/latest/usagedata/consent` | Same — grant, decline, token uniqueness, rate limit |
| `DELETE /api/latest/usagedata/consent` | Same — revoke with own token, and with a token never minted |
| Footer indicator opens the dialog | `UsageDataIndicator.test.tsx` clicks the real button. The indicator and dialog meeting inside `Footer` is DELIVER's wiring test |
| Consent dialog, two decisions | `UsageDataDialog.test.tsx` clicks the real buttons |
| Team/Portfolio page as a whole, end to end | **DELIVER**, one Playwright walking skeleton (DT-13) |
| `docs/settings/usagedata.md` | **DELIVER** — the page does not exist yet, and the CI check comparing it against the emitted field set needs the field set |

---

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN's driving ports and the `/state` DTO — consumed as written. The token travels in
  `X-Lighthouse-UsageData-Token`; DESIGN said "a request header" without naming one, so this wave
  names it.
- DEVOPS's `environments.yaml` — `auth-off` is asserted directly (the endpoints answer without
  authentication). `upgrade-from-pre-5733`, `rollback-after-upgrade` and the SQLite/Postgres axis are
  DELIVER's, because all three need the migration.
- The ATDD infrastructure policy — two rows appended this wave (`IUsageDataConsentRepository`,
  `IUsageDataPublisher`).
- SPIKE-00 promoted nothing (DISCARD), so there is no walking skeleton to inherit.

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

**Reconciliation passed — 0 contradictions.** DISCUSS D1-D11, DESIGN A1-A14 and DEVOPS P1-P13 were
read in full, along with DESIGN's four flagged contradictions and DEVOPS's two back-propagations.

Every contradiction in the chain was already resolved in-band rather than left for this wave:

- DESIGN's four (S13's wrong affordance, the declined no-per-emit-read constraint, `UpdateServiceBase`,
  "exactly five fields") are recorded with their resolutions in the DESIGN band.
- DEVOPS's two back-propagations are both **applied**, not pending: `OUT-usagedata-zero-leak-before-consent`
  was rescoped during DESIGN review, and `OUT-usagedata-consent-uptake` on 2026-09-11.

The one place a reader might expect a contradiction and find none: DEVOPS P12 requires the backend to
refuse an emit to the production collector host under a test environment, which reads like a new
constraint on DESIGN's publisher. It is an addition, not a contradiction — ADR-176 fixes the default
host and says nothing about test contexts.

---

## Wave: DISTILL / [REF] AT completeness audit

Slice 01's criteria only (DT-1). Where a criterion is *partly* covered, the remainder is named.

| AC | Covered by | Remainder in DELIVER |
|---|---|---|
| AC-01.1 | — | **DELIVER**: the indicator mounted in `Footer`, beside the version. DESIGN moved it out of the "Contact us" link row, and this criterion still describes the old placement — flagged upstream |
| AC-01.2 | Indicator — accessible names differ between states, asserted as names not attributes (DT-8) | — |
| AC-01.3 | Indicator says which state it is in, in the journey's exact words | The indicator reflecting real `/state`, which needs the service |
| AC-01.4 | — | **DELIVER**: renders identically on standalone, auth-off and RBAC. Needs the deployment shapes, not a component test |
| AC-01.5 | Indicator — `unknown` renders as not-sending (DT-9) | — |
| AC-01.6 | Indicator click reopens the decision, from either state | — |
| AC-02.1 | Dialog names all five fields, and the list is now the payload's own (identifier, version, deployment mode, licence tier, **timestamp**) | The field list agreeing with `docs/settings/usagedata.md` (CI check) |
| AC-02.2 | Dialog states each of the six never-sent categories **positively** | — |
| AC-02.3 | Dialog names PostHog and Frankfurt | The docs page carrying residency and sub-processors |
| AC-02.4 | Dialog links `docs/settings/usagedata.md` (`docsUrl`, asserted on the rendered link) | The page existing, and carrying the GitHub-release-check carve-out (D11) |
| AC-02.5 | Dialog — Escape closes without deciding, and `onDecision` is never called | — |
| AC-02.6 | Dialog — browser storage untouched across open and close, and no decision reported | The network half (no request before the click), which belongs with the zero-leak harness |
| AC-02.7 | `POST /consent` mints a token on a grant; dialog reports `granted` | — |
| AC-02.8 | `POST /consent` mints a token on a decline too; dialog reports `declined` | — |
| AC-02.9 | Dialog — `willAskAgain` both ways, each anchored against the other's copy; `/state` derives it without naming the licence | — |
| AC-02.10 | SurveyNudge forbidden-phrase test (skipped, fails today by design) | **DELIVER**: the copy change itself, plus CRA rows 1.7 **and 1.9** |
| AC-03.2 | `DELETE /consent` stops the instance sending | **DELIVER**: the emit path actually not firing, which needs the gate |
| AC-03.3 | — | **DELIVER**: revocation survives a restart, which needs the store |
| AC-03.4 | — | **DELIVER**: `Revoked` as a third state, re-askable (H1) |
| AC-03.5 | — | **DELIVER**: the liveness-window gap — cleared storage makes a browser immediately re-askable but stops it counting only after the window |
| AC-04.1 | — | **DELIVER**: one heartbeat per instance per day, `requires-docker`, N hosts one container |
| AC-04.2 | — | **DELIVER**: payload purity — the serialised property set equals the declared set exactly |
| AC-04.3 | — | **DELIVER**: no consenting browser means no identifier at all |
| AC-04.4 | — | **DELIVER**: identifier derived from nothing, 16 CSPRNG bytes |
| AC-04.5 | — | **DELIVER**: fire-and-forget, degrades silently |
| AC-04.6 | — | **DELIVER**: slice 01's acceptance is the dogfood instance emitting and being seen |
| AC-04.7 | — | **DELIVER**: the emit carries no browser-supplied value |
| S3 / S4 | Covered as AC-02.10 above | **DELIVER**: the SurveyNudge copy, CRA row 1.7 **and row 1.9** ("No outbound connections except to configured work tracking systems" — already false via the GitHub release check, and more so once the collector exists; found by the DISCUSS reviewer, unmentioned in any band before that) |
| `OUT-usagedata-zero-leak-before-consent` | The browser half — AC-02.6's storage assertion | **DELIVER**: `DelegatingHandler` + the ArchUnit rule forbidding ad-hoc client construction |

Error and edge coverage: **13 of 51** tests carry `@error`, `@edge`, `@oracle` or `@security` — 25%,
below the 40% guideline and reported rather than met.

Two corrections to how this was stated before. The earlier figure ("11 of 39, 28%") was arithmetically
wrong at the granularity the total was counted at. And the stated reason was only half true: DT-3
genuinely puts the richest error paths out of reach — the gate refusing, the emit degrading silently, a
losing identifier race that still records the grant — but AC-02.5 and AC-02.6 were driving-side edge
cases DT-3 never reached, and they were simply missing rather than deferred. They are now written.

The ratio went **down** while coverage went up, because most of what was added is positive assertion.
The 13 excludes the 7 forbidden-phrase and 6 never-sent tests, which are copy constraints and could be
argued into the numerator — that would put it at 39%, which is close enough to the guideline to be
exactly the kind of reclassification that makes a metric meaningless. Counted conservatively and left
below the line. Re-measure after DELIVER, where the driven-side error paths land.

---

## Wave: DISTILL / [REF] RED classification

Verified by running, not asserted.

1. **Backend builds clean.** `dotnet build` on the test project: **0 warnings, 0 errors** — after one
   fix. The first build failed with `CS0121: The call is ambiguous between SomeItemsConstraint.Using(IComparer)
   and Using<T>(IComparer<T>)`. Exactly the class of thing DT-3 exists to prevent reaching the whole
   assembly, caught by the gate rather than by a reviewer.
2. **Frontend: 5 files, 2 skipped, 3 passed, 0 failed suites.** No collection error — the DT-5 trap
   (`describe.skip` still evaluating its body) did not fire, because every scaffold call sits inside a
   test body.
3. **RED confirmed by probe.** `UsageDataIndicator`'s block was temporarily unskipped and the file
   run: **7 failed tests**, each `Error: UsageDataIndicator(state=…, onOpenDecision=function) is not
   implemented`. The assertion is never reached because the contract is missing — the correct RED
   shape. No `ImportError`, no fixture failure, no collection failure. The block was restored.

4. **The backend was then probed too, and the first claim here was false.** This section originally
   read *"Classification for all 39: MISSING_FUNCTIONALITY"* on the strength of the frontend probe
   alone. Running the backend fixture with its `[Ignore]` attributes stripped gave **8 failed, 3
   passed**. Three tests passed against a product with no endpoints at all:
   `GetState_WithAnUnknownToken_…` and `DeleteConsent_WithATokenThisInstanceNeverMinted_…` compared two
   responses to *each other* with no anchor that either had succeeded (two empty 404 bodies are equal;
   two 405s on an unrouted DELETE are equal), and `TheConsentEndpoints_RequireNoAuthentication_…`
   asserted only `Is.Not.EqualTo(Unauthorized)`, which a 404 satisfies.

   Falsely green is worse than BROKEN: the two oracle tests would have stayed green if the leak ever
   arrived through the status code or a header rather than the body, which is the channel an oracle
   usually leaks through. All three are now anchored — assert the baseline succeeded and is the state
   document, *then* compare — and re-probed: **11 failed, 0 passed.**

**Classification for all 51: `MISSING_FUNCTIONALITY`.** Zero BROKEN, zero falsely green. This time it
was verified on both stacks rather than generalised from one file.

---

## Wave: DISTILL / [REF] Verification

Run at hand-off, on the working tree:

| Gate | Result |
|---|---|
| `dotnet build` (test project) | **0 warnings, 0 errors** |
| `npx tsc -b` | **Clean**, exit 0 — the scaffolds compile under `strict`, `noUnusedLocals`, `noUnusedParameters` (DT-4 is why) |
| `npx biome check` on the new and touched files | **Clean** after `--write`, scoped to the two directories (never repo-wide — the `*/docs` symlink hazard) |
| `npx vitest run` on the touched paths | **41 passed, 40 skipped, 0 failed**, zero collection errors |
| `dotnet test` on the backend fixture, `[Ignore]` stripped | **11 failed, 0 passed** — every one RED for the right reason (re-run after the review; the first attempt had 3 passing falsely) |

The suite is **green by construction** at hand-off: every new test is skipped or `[Ignore]`d, and the
only shipped file touched is `SurveyNudge.test.tsx`, where the addition is skipped.

---

## Wave: DISTILL / [REF] Outcomes registry

**Not registered this wave. N/A, because** every typed contract slice 01 introduces — the consent
record, the gate, the permit, the publisher port — is a type DT-3 forbids creating here. Registering
an OUT-N row for a contract with no artifact would put a promise in the registry that nothing
implements, which is the failure the registry exists to catch rather than commit.

DELIVER registers them as each lands. The candidates, named now so the decision is not re-taken:
the emit gate (`specification` — may this instance send, fail-closed), the consent record
(`invariant` — one row per browser, revocable, decaying on a liveness window), and the payload
contract (`invariant` — the closed field set). `nwave-ai outcomes check-delta` returned exit 0 on this
delta during DESIGN.

---

## Wave: DISTILL / [REF] KPI contracts

`docs/product/kpi-contracts.yaml` is **not extended. N/A, because** P8 already settled it: the seven
deferred KPIs move to a live measurement source at slice 04 per AC-08.6, and naming a source that does
not exist is the habit this Epic was commissioned to end. The three hard-gate KPIs are asserted in the
suite rather than measured in the field, so there is no measurement window or `@kpi` tag to link.

Stated rather than skipped.

---

## Wave: DISTILL / [REF] Handoff to DELIVER

**Held for review — DELIVER is not entered.** Nothing is finalized and no mutation testing has run.

The four-reviewer Final Wave Review Gate (Eclipse / Architect / Forge / Sentinel) has **not** been
dispatched. It is the documented entry condition for DELIVER and must clear — or be explicitly waived
— before slice 01 starts. Recorded here rather than silently skipped. Note that Forge already reviewed
the DEVOPS band on 2026-09-11 (NEEDS_REVISION, 8 blocking, all fixed); that was a per-wave review and
does not substitute for the consolidated gate.

**DoR-9 no longer blocks slice 01.** It closed on 2026-09-12; see the DoR Validation section. Its one
remaining question governs slice 04.

What DELIVER picks up, in order. **Slice 01 was split into 01a and 01b on 2026-09-12**, and the
ordering below now spans the two: steps 1 and 3 are 01a, steps 2, 4 and 5 are 01b.

1. **The driven side of consent first** (01a), because most of the error paths are owed there: the
   consent entity and repository (Postgres, `requires-docker`), and the identifier with its migration
   (additive, one table — no `AppSetting` schema change).
2. **Then the gate, the permit and the publisher** (01b), and the invariants that need them: payload
   purity, revocation latency, the zero-leak `DelegatingHandler` plus the ArchUnit rule, and the day
   key that keeps three replicas to one heartbeat. The day key is a conditional update read by its
   affected-row count, not a read-then-write upsert, and it needs a seeded row and a database
   container — see ADR-174 point 8, which also retracts a precedent an earlier draft cited wrongly.
3. **Then the consent wiring** (01a): the service, the indicator in `Footer`, the dialog,
   `docs/settings/usagedata.md`, and the two wiring tests the component tests deliberately do not
   cover.
4. **Then the corrections that gate the ship** (01b, because 01b is what makes them false):
   `SurveyNudge` copy (un-skip DT-12's test), `cra-self-assessment.md` row 1.7, and the CI field-list
   check over the docs page.
5. **Last, the Playwright walking skeleton** (DT-13), with the collector host pointed at a blackhole in
   all six app-start blocks (P12).
6. Never push red; un-skip only as each block goes green.

---

## Wave: DELIVER / [REF] Five decisions taken 2026-09-12, and one premise withdrawn

Slice 01 had been held on five open questions. None was external and none needed another wave to
answer, so they were answered together. Each is recorded in the section it belongs to as well; this
is the one place that lists them.

| # | Question | Decision |
|---|---|---|
| 1 | How long the collector keeps events | **One year, and not by choice** — PostHog fixes retention by plan (1 free / 7 paid) and will not shorten it on request. A year happens to answer every question we ask. A paid upgrade would silently make it seven and falsify the dialog. ADR-175 point 7 |
| 2 | What the heartbeat day key is written with | A **narrow conditional-update accessor of its own**, not the `AppSettings` upsert, using the `ExecuteUpdateAsync`-with-the-old-value-in-the-`Where` shape this codebase already uses for compare-and-swap. ADR-174 point 8 |
| 3 | Whether slice 01 splits | **Yes — 01a and 01b.** The reviewer's H6 split, accepted |
| 4 | The 30-day consent decay | **Kept at 30 days, and disclosed in the dialog.** ADR-173 |
| 5 | Whether the uptake measure needs a target that can fail | **Yes**, and the half that could not fail is deleted rather than re-targeted |
| 6 | Whether a widened payload needs re-consent (AC-08.5) | **No, within a stated boundary.** Re-consent only if the payload gains something person-scoped or free-text, the purpose widens, the set of parties holding the data widens, or retention lengthens. An unbounded "no" would let a later slice add anything under a consent given for five fields |

**A premise withdrawn, found while writing decision 2.** ADR-174 point 8 said the day key used "the
same compare-and-swap shape that `DeliveryMetricSnapshot`'s `RecordedDay` already uses". It does not.
`DeliveryMetricSnapshotRepository.GetOrCreateForDay` reads with `FirstOrDefault` and then adds, and
its one-row-per-day guarantee comes from a unique index over `(DeliveryId, RecordedDay)` in
`LighthouseAppContext` rather than from an affected-row count. An implementer following the pointer
would have written the defect the point exists to prevent.

This is the **second** invented precedent in this Epic, after the `AppSetting.Key` deduplication that
five review passes sharpened instead of checking. Both were citations to code, both were wrong, and
both took one `grep` to settle. The working conclusion is not "review harder" — the reviews were
thorough. It is that **a claim about what existing code does is not review-able prose, and has to be
opened rather than reasoned about.** The real precedent is named now, with its file, so the next
reader can check it in one step.
