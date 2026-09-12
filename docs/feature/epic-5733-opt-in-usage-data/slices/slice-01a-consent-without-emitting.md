# Slice 01a — A person can see the offer, decide on it, and change their mind, and nothing is sent

**First half of the walking skeleton.** The consent record and the surfaces that read and write it.
Nothing in this slice can send anything anywhere, because nothing that sends exists yet.

## Goal

A person opening Lighthouse can see that usage data is not being sent, read the full list of what
*would* be sent if they agreed, agree or refuse, and reverse the decision afterwards — and the state
they chose is what the server holds.

## IN scope

- Footer indicator (US-01): icon beside the version, state-differentiated, tooltipped, clickable.
- Usage Data dialog (US-02): enumerates the five payload fields, names the collector and its
  operator, states what is never sent, says where the data rests without implying it never leaves the
  EU, says that sending stops within 30 days of the last visit, says that revoking stops future
  sending and does not erase what was already sent, links the docs page, two buttons.
- Consent record: opaque token minted server-side on the click, stored in the browser. Grant,
  refusal and revocation each recorded as distinct states.
- The anonymous state endpoint, and the throttled liveness touch behind it.
- Revocation as a **record** (US-03, the storage half): one click flips the state, the indicator
  follows in the same interaction, and the server holds the new state.
- Instance identifier: random, minted lazily on first grant, persisted. Nothing reads it yet.
- `docs/settings/usagedata.md`: the complete field list, what is never sent, where the data rests,
  the retention period, the 30-day decay, and what revoking does and does not do. The dialog links
  it, so it has to exist here rather than in 01b.
- The composite index over the columns the live-grant question filters on.

## OUT of scope

- **Anything that emits.** No gate, no permit, no publisher, no heartbeat, no collector client. That
  is slice 01b.
- Revocation *enforcement* against an emitter — there is no emitter to enforce against. AC-03.2 is
  slice 01b.
- The copy corrections in `SurveyNudge.tsx` and `cra-self-assessment.md`. Both currently say
  Lighthouse collects nothing, and after this slice that is still true. They change in 01b, in the
  same slice that makes them false.
- The unprompted dialog and any cadence — slice 02. Here the dialog opens only on a click.
- The admin master switch and premium gating — slice 03.
- Any product event beyond the heartbeat — slice 04.

## Learning hypothesis

**Disproves "a browser can hold a consent decision that the server can act on later" if** the state
endpoint cannot answer per-browser state without authentication, or if the token cannot survive the
round trip the dialog needs.

**Disproves "deciding nothing costs nothing" if** opening and closing the dialog without choosing
leaves anything behind in browser storage.

**Confirms** the consent half of the architecture if the maintainer can grant, see the indicator
change, revoke, and see it change back, with the server's record matching at each step.

## Acceptance criteria

Per US-01 (AC-01.1…1.6), US-02 (AC-02.1…2.10) and the storage half of US-03 (AC-03.1, AC-03.3,
AC-03.4, AC-03.5) in `feature-delta.md`. The two that make or break the slice:

- **AC-02.6** — browser storage untouched after opening and closing the dialog without deciding.
- **AC-03.4** — a browser that granted and then revoked is re-askable, and is not confused with one
  that refused in the first place.

## Dogfood moment

Same day. The maintainer grants on the vendor's own instance, watches the indicator change, revokes
from the footer, and watches it change back. No network traffic is involved and none should appear.

## Dependencies

- SPIKE-00 complete (collector and direction chosen), because the dialog names the collector and its
  operator even though this slice never contacts it.
- **DoR-9 closed for slice 01** as of 2026-09-12. The retention position the dialog states is in
  ADR-175 point 7; the residency wording it must not overstate is in the DoR-9 section of
  `feature-delta.md`. The one question still open (whether a widened payload needs re-consent)
  governs slice 04 and does not gate this slice.
- One additive EF migration per provider for the consent record, via `CreateMigration`.

## Effort

One day. This is the half of the original slice 01 that carries no emit path, and it passes the
four-component taste test that the combined slice failed: indicator, dialog, consent record with its
endpoints.

## Pre-slice risk

The dialog's copy is the part of this slice that is hardest to take back, because a sentence that
overstates where the data rests or what revoking does is a promise a reader will rely on. The
retention, residency and decay wording in ADR-173 and ADR-175 point 7 is the source; the dialog
quotes it rather than paraphrasing it.
