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
are corrected inside slice 01 or slice 01 does not ship.

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
  concept, migrating "manual ordering" out of `FeatureOrderingSettings` into an OptionalFeature.
  Separate board item. Only the S8 silent-no-op defect is pulled in, because a privacy control cannot
  ship on a broken gate.
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
  reporting on the day slice 01 ships.
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

`job_id: job-admin-stop-lighthouse-asking-my-people` · Slice 03

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
  did not see enumerated are added to the docs and the dialog on the next open. Whether re-consent is
  required for a widened payload is a legal question flagged at DoR-9 and answered before this slice
  ships.
- AC-08.6 The KPI contracts whose questions these events answer move off
  `status: deferred-pending-telemetry-feature` and name the events that now source them (S5).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone (user activities, left to right):**

`Find out what Lighthouse sends` → `Decide` → `Change my mind` → `Govern it for my organisation`
→ (vendor side) `Learn from what came back`

| Activity | Slice 01 (skeleton) | Slice 02 | Slice 03 | Slice 04 |
|---|---|---|---|---|
| Find out what it sends | US-01 indicator, US-02 dialog + docs page | — | — | US-08 docs widened |
| Decide | US-02 (on click) | US-05 (asked unprompted, honest cadence) | — | — |
| Change my mind | US-03 | — | — | — |
| Govern it | — | — | US-06 switch, US-07 honest refusal | — |
| Learn from it | US-04 heartbeat + census | — | — | US-08 product events |

**Walking skeleton = slice 01.** One browser consents, one heartbeat leaves, the maintainer sees it,
one click stops it. Everything after thickens a path that already runs.

**Preceded by SPIKE-00** (collector probe), which is not a slice: it ships no user value and must not
be released as one.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | Slice 01 ships indicator + dialog + consent record + emitter + docs = 5. **Fails.** Accepted with reason: this is a walking skeleton (Strategy A) and removing any one of the five leaves a path that does not run end to end. Deliberately compensated by shrinking its scope to one event, one payload, no cadence, no admin switch, no premium. |
| Does every slice depend on a new abstraction? | No. Slices 02–04 depend on slice 01's consent record, which slice 01 ships first — the abstraction leads, as required. |
| Does any slice disprove a pre-commitment? | Yes, each. See per-slice hypotheses in the slice briefs. |
| Synthetic data only? | No. Every slice's acceptance runs against the vendor's own production instance emitting into the real collector on the day it ships. |
| Two slices identical except for scale? | Slice 01 (heartbeat) and slice 04 (product events) are both "emit an event". Not merged: 01 proves consent and transport with a payload that has no user in it, 04 proves the event *vocabulary* is the right one, and they fail for different reasons. |

---

## Wave: DISCUSS / [REF] Prioritization

| Order | Item | Rationale |
|---|---|---|
| 1 | SPIKE-00 collector probe | Highest uncertainty, blocks everything, and the wrong answer is expensive in both money (plan tier) and architecture (D10). Failing here costs a day. |
| 2 | Slice 01 walking skeleton | The only slice that can fail for structural reasons. Also the slice that makes the product stop contradicting itself (S3, S4), so nothing else may ship before it. |
| 3 | Slice 02 the ask | Highest learning leverage per hour: uptake is the number the whole Epic rests on, and until people are actually asked it is unmeasured. Deliberately before the admin switch, because a switch governing a prompt nobody sees proves nothing. |
| 4 | Slice 03 admin veto | Dependency-driven: needs something to veto. Carries the S8 defect fix, so it is also the slice that must not be skipped. |
| 5 | Slice 04 product events | Last on purpose. The event vocabulary should be chosen once there is a real consenting population to spend it on, and after slice 02 tells us how big that population is. |

Dogfood cadence: every slice is dogfooded on the vendor's own instance the day it ships. Slice 01's
dogfood *is* its acceptance (AC-04.6).

---

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Measurement | Slice |
|---|---|---|---|
| OUT-usagedata-consent-uptake | ≥ 20% of browsers shown the dialog answer Yes, within 60 days of slice 02 | Consent grants ÷ dialogs shown, both counted at the collector | 02 |
| OUT-usagedata-instances-reporting | ≥ 25 distinct instance identifiers report in a rolling 24h window within 90 days of slice 02 | Distinct instance identifiers at the collector | 01, 02 |
| OUT-usagedata-zero-leak-before-consent | Exactly 0 bytes leave an instance that has no consenting browser | Automated: an integration test asserting no outbound call on the collector host across a full emit cycle with zero consent, plus a packet-level check on a clean instance at slice 01 acceptance | 01 |
| OUT-usagedata-revocation-latency | 100% of revocations stop the next emit; 0 emits after a revoke | Automated assertion at the emit path (AC-03.2) | 01 |
| OUT-usagedata-payload-purity | 0 events carrying customer content across the whole event set | Automated invariant over emitted payloads (AC-08.3), run in CI | 01, 04 |
| OUT-usagedata-kpis-unblocked | ≥ 3 of the 7 KPIs at `deferred-pending-telemetry-feature` move to a live measurement source within 30 days of slice 04 | Count in `docs/product/kpi-contracts.yaml` | 04 |
| OUT-usagedata-no-nag-complaints | 0 community reports of a repeated or unstoppable Usage Data prompt within 90 days of slice 02 | Community channels, GitHub issues | 02 |

`OUT-usagedata-zero-leak-before-consent`, `-revocation-latency` and `-payload-purity` are hard CI
gates. The rest are collector-sourced and become measurable for the first time *because* of this
Epic — which is the recursion the Epic exists to break.

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
| 6 | Slices ≤ 1 day with learning hypotheses | PASS with one documented exception | Slice 01 fails the 4-component taste test as an accepted walking-skeleton cost; see Slice Taste Tests. |
| 7 | Out-of-scope explicit | PASS | Out of Scope section; the OptionalFeatures rework is explicitly deferred to the board. |
| 8 | Outcome KPIs with numeric targets and measurement method | PASS | 7 KPIs, each with a target and a named source. |
| 9 | Legal / compliance review of consent copy and the Art 5(3) reading (D2), and of whether a widened payload requires re-consent (AC-08.5) | **OPEN — blocking slice 01** | Not resolvable inside DISCUSS. Named here so it is scheduled rather than discovered. |

**DoR verdict: 8 of 9 PASS, DoR-9 open and blocking.** DESIGN and SPIKE-00 may start; slice 01 may not
ship until DoR-9 closes.

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
- Walking skeleton scope: slice 01 — consent → heartbeat → collector → revoke, plus the docs page and
  the two copy corrections that stop the product contradicting itself.
- Feature type: cross-cutting (backend emitter, frontend consent surface, licensing, public docs,
  compliance).

### Constraints Established
- No user identity exists on standalone or auth-off instances (S11) — per-account consent is off the
  table, not merely inconvenient.
- Nothing may be read from or written to the browser before the consent click (D2).
- No event may carry customer content, enforced as a CI invariant.
- The premium gate must stop silently dropping writes before a privacy control rides on it (S8).
- Two shipped surfaces and one compliance document currently promise the opposite of this Epic and
  must change with slice 01 (S3, S4).

### Upstream Changes
- **`docs/product/kpi-contracts.yaml` preamble is now false in principle.** It states there is no
  phone-home mechanism. Amended at slice 04 (AC-08.6), when the first KPI actually moves. Not amended
  earlier — the statement stays true for every non-consenting instance, which is all of them until
  slice 02.
- **`docs/compliance/cra-self-assessment.md` row 1.7 is amended at slice 01**, not slice 04. A
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
