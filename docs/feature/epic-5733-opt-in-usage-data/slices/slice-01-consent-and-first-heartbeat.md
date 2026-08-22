# Slice 01 — One browser consents, one heartbeat arrives, one click stops it

**Walking skeleton.** The first end-to-end path: consent → emit → collector → revoke.

## Goal

A person can see whether Lighthouse is sending anything, decide on the full list of what would be
sent, and stop it again — and the maintainer sees the vendor's own instance arrive in the collector
the same day.

## IN scope

- Footer indicator (US-01): icon beside the version, state-differentiated, tooltipped, clickable.
- Usage Data dialog (US-02): enumerates the five payload fields, names the collector and its
  operator, states what is never sent, links the docs page, two buttons.
- Consent record: opaque token minted server-side on the click, stored in the browser. Grant and
  refusal both recorded.
- Revocation (US-03): one click, effective on the next emit, enforced server-side.
- Instance identifier: random, minted lazily on first consent, persisted.
- Daily heartbeat (US-04): instance identifier, version, deployment mode, licence tier, timestamp.
  Emitted only while at least one browser holds live consent.
- `docs/settings/usagedata.md`: the complete field list, plus the GitHub release check named as a
  separate pre-existing outbound call this consent does not cover.
- **Copy corrections**: `SurveyNudge.tsx:114` and `cra-self-assessment.md` row 1.7.

## OUT of scope

- The unprompted dialog and any cadence — slice 02. In this slice the dialog opens only on a click.
- The admin master switch and premium gating — slice 03.
- Any product event beyond the heartbeat — slice 04.
- Making the collector endpoint customer-configurable. Hard-wired to the SPIKE-00 choice; the
  air-gap requirement lands with DESIGN's endpoint work.

## Learning hypothesis

**Disproves "a browser-scoped consent record can gate a server-side emitter with no staleness" if**
a revoked browser's revocation does not stop the very next emit, or if the emit path cannot consult
consent cheaply enough to do so on every cycle.

**Disproves "nothing leaks before consent" if** the zero-consent packet check finds any traffic to
the collector host.

**Confirms** the whole architecture if the vendor's own instance appears in the dashboard and
disappears from it within one cycle of a revoke.

## Acceptance criteria

Per US-01 (AC-01.1…1.6), US-02 (AC-02.1…2.10), US-03 (AC-03.1…3.5), US-04 (AC-04.1…4.7) in
`feature-delta.md`. The three that make or break the slice:

- **AC-02.6** — browser storage untouched after opening and closing the dialog without deciding.
- **AC-03.2** — the next emit after a revoke does not happen, asserted at the emit path.
- **AC-04.2** — the emitted payload contains exactly five fields, asserted against the payload.

## Production-data acceptance

**AC-04.6** — the maintainer opens the collector dashboard and sees the vendor's own production
instance reporting, on the day this ships. No synthetic instance counts.

## Dogfood moment

Same day. The vendor's instance consents, the heartbeat lands, the maintainer revokes from the
footer, and the next cycle produces nothing. That round trip is the demo.

## Dependencies

- SPIKE-00 complete (collector + direction chosen).
- **DoR-9 closed** — legal sign-off on the consent copy and the D2 Art 5(3) reading. This slice may
  be built before DoR-9 closes; it may not ship.
- One additive EF migration per provider for the consent record, via `CreateMigration`.

## Effort

Above one day, and knowingly so. This slice fails the four-component taste test (indicator, dialog,
consent record, emitter, docs = 5) and the failure is accepted because removing any one leaves a path
that does not run end to end. Compensated by holding it to one event, one payload, no cadence, no
switch, no premium.

## Reference class

`epic-5775` secret encryption — the comparable "new durable record + new gate + new settings surface"
slice. It ran long for the same reason: the first slice of a capability that has no existing
scaffolding pays for the scaffolding.

## Pre-slice risk

The zero-leak proof (`OUT-usagedata-zero-leak-before-consent`) needs a test asserting the *absence*
of network traffic. This repository has no harness for that today. Building it is part of this slice,
not an afterthought — and if it cannot be built, the invariant is unverified and the slice is not
done.
