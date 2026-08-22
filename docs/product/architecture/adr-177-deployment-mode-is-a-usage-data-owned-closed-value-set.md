# ADR-177: Deployment mode is a closed value set owned by the usage-data payload and resolved beside `PlatformService`, not inside it

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5733-opt-in-usage-data (ADO Epic #5733, slice 01)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Deployment mode is one of the five heartbeat fields, and the consent dialog enumerates the payload by
name. So the value set is not an implementation detail - it is part of what the user is agreeing to,
and widening it later is a re-consent question the legal review has to answer.

`PlatformService` cannot produce the value the census wants. It yields
`SupportedPlatform { Windows, Linux, MacOS, Docker }` plus a separate `IsStandalone` flag, and its
`IsDocker()` keys on `DOTNET_RUNNING_IN_CONTAINER`, `/.dockerenv` and `LIGHTHOUSE_DOCKER` - all of
which are true inside a Kubernetes pod. `KUBERNETES_SERVICE_HOST` appears nowhere in the repository.

So today every Kubernetes instance would report as Docker. Given that Kubernetes readiness and
productization have been the subject of their own Epics, a census that cannot see Kubernetes adoption
is missing the thing it would most be used to answer.

Fixing it inside `PlatformService` is not free. It is a singleton consumed by the release and update
paths, where `Platform` decides whether an in-place update is even supported. Changing what `Docker`
means there would alter behaviour on a path that has nothing to do with usage data, on instances that
never consent.

## Decision

**A `UsageDataDeploymentMode` enum owned by the usage-data payload, resolved by a small collaborator
that composes `IPlatformService` with a Kubernetes probe. `PlatformService` is not modified.**

1. **The value set is closed and frozen at slice 01:**
   `Standalone`, `Windows`, `Linux`, `MacOS`, `Docker`, `Kubernetes`.

2. **Resolution order, and it matters:**
   - `IsStandalone` wins first - it is a distinct distribution shape, not an operating system, and a
     standalone install is the case where "which OS" is least interesting and "is this the desktop
     build" is most.
   - Then `Kubernetes`, if `KUBERNETES_SERVICE_HOST` is present. It is checked *before* Docker
     precisely because the Docker markers are also true in a pod, so the more specific answer has to
     be asked for first.
   - Then whatever `IPlatformService.Platform` reports.

3. **The resolver is a new, tiny component** - `IUsageDataDeploymentModeResolver` - taking
   `IPlatformService` as a collaborator and reading the one environment variable itself. It lives with
   the usage-data code, because the value set belongs to the payload contract rather than to the
   platform abstraction.

4. **Widening the set later is a re-consent decision, not a code change.** Adding a value means the
   dialog's enumerated list changes, which means browsers that consented to the old list consented to
   something narrower. That is the same question the delta already flags for slice 04's widened
   payload, and it is answered by the legal review, not by whoever adds the enum member.

## Alternatives considered

- **Emit only what `PlatformService` can already produce** - the four platforms plus the standalone
  flag - and accept that Kubernetes reports as Docker. **Rejected.** It is the cheapest option and it
  poisons the field permanently: once the dialog has enumerated a set that conflates the two, fixing
  it is a re-consent event rather than a bug fix. Better to pay for the distinction once, now, while
  the value set is being defined for the first time and nobody has consented to anything yet.

- **Teach `PlatformService` about Kubernetes** by adding a `Kubernetes` member to `SupportedPlatform`
  and checking the variable in `IsDocker()`. **Rejected on blast radius.** `SupportedPlatform` is
  consumed by the release/update path where `Docker` currently carries an operational meaning about
  whether in-place updates apply. Adding a member means every consumer's switch has to be re-examined,
  on behalf of a feature most instances will never turn on. This is the option that would be right if
  Kubernetes detection were needed *generally* - and if a second consumer ever appears, that is the
  moment to promote it, with the update path re-examined deliberately rather than incidentally.

- **Send the raw signals - the platform enum, the standalone flag and the Kubernetes marker - and
  derive the mode at the collector.** **Rejected.** It widens the payload from one field to three to
  say the same thing, and the dialog would have to enumerate all three. The user consents to a
  fact about their deployment, not to the evidence we used to work it out.

- **Detect Kubernetes more thoroughly** - service-account token file, cgroup inspection, downward-API
  fields. **Rejected as disproportionate.** `KUBERNETES_SERVICE_HOST` is injected by kubelet into
  every pod in the default configuration and is the conventional check. A false negative degrades to
  `Docker`, which is exactly today's behaviour, so the downside of the simple probe is bounded by the
  status quo.

## Consequences

**Positive**

- The census can answer Kubernetes adoption from the day it starts collecting, which is the question
  the platform Epics most invite.
- `PlatformService` and the release path are untouched, so no instance that never consents changes
  behaviour.
- The value set is a named, closed, reviewable thing that the dialog, the docs and the payload
  assertion can all point at - three consumers that must agree, which is the property the docs page
  is meant to guarantee.

**Negative / accepted**

- Two places now answer "what am I running on", with slightly different vocabularies. The resolver
  depends on `IPlatformService` rather than duplicating it, so they cannot disagree about the OS - but
  a reader has to know why the second one exists, which is what this ADR is for.
- A Kubernetes deployment that somehow lacks `KUBERNETES_SERVICE_HOST` reports as `Docker`. That is
  the current behaviour for every such instance, so it is not a regression.

**Reuse verdict**: `IPlatformService` / `PlatformService` -> **UNCHANGED**, composed rather than
modified, on the blast-radius grounds above. `SupportedPlatform` -> **UNCHANGED**; deliberately not
extended, and this is the contested verdict in this ADR. `ISystemInfoService` -> **UNCHANGED**; it
reports `Os`/`Runtime`/`Architecture` for an operator reading a diagnostics panel, which is a
different question with a different audience and a different privacy posture.

**Enforcement**

| Rule | Mechanism |
|---|---|
| A pod reports Kubernetes, not Docker | NUnit: `KUBERNETES_SERVICE_HOST` set together with the Docker markers resolves to `Kubernetes` |
| Standalone wins over the OS | NUnit: standalone plus Windows resolves to `Standalone` |
| The set is closed | NUnit: the emitted value is always a declared enum member; the payload assertion rejects anything else |
| The dialog lists exactly this set | Vitest: the rendered dialog's enumerated deployment values equal the enum's members |
| The docs list exactly this set | CI: `docs/settings/usagedata.md` compared against the declared set |
| `PlatformService` behaviour is unchanged | Existing `PlatformService` tests, re-run unmodified |

Cross-refs [ADR-176](./adr-176-posthog-cloud-eu-as-a-named-adapter-with-payload-carried-privacy-controls.md)
(the payload this field belongs to, and the purity assertion that closes it).
