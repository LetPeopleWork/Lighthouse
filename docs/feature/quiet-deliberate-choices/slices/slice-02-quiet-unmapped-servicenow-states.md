# Slice 02 — Drop unmapped ServiceNow states in silence

**Story**: US-02 | **Job**: `job-config-admin-quiet-deliberate-state-omission` | **Estimate**: ~1h

## Goal

Delete the WARNING that names the states a Team never mapped, leaving the drop behaviour untouched.

## IN scope

- `ServiceNowWorkTrackingConnector.cs` — remove `ReportStatesTheTeamNeverMapped` (`:306-327`), its call
  (`:290`) and its preamble comment (`:302-305`).
- `ServiceNowTeamSyncTest.cs` — delete `WorkInAStateTheTeamNeverMapped_IsNamedInTheLogRatherThanDroppedInSilence`
  (`:190-200`) and `WorkCarryingNoStateAtAll_IsNamedInTheLogAsHavingNone` (`:729-741`); replace them with
  a single test asserting **silence** on a sync holding unmapped and stateless records. The
  `AWarningContaining` helper (`:753`) loses both callers in this file — delete it there; the identical
  helper in `ServiceNowRecordClassTest.cs:530` is a separate copy and stays.
- `MappedRecord.Label` (`:1169`) is read **only** at `:320`, inside the removed method — verified; the
  `span.Label` at `:439` is a different type. Drop the positional parameter along with the method. The
  mapper still reads the raw state label to classify it, it just no longer needs to carry it forward.

## OUT of scope

- The other four ServiceNow warnings (D6). `ATeamThatMappedEveryStateItsWorkIsIn_IsNotWarnedAbout`
  (`:719`) stays and keeps guarding them.
- Which records get dropped (D7) — `WorkInAStateTheTeamNeverMapped_IsLeftOut` (`:170`) must not be
  edited, and must stay green.
- Any replacement surface: no UI hint, no validation message, no API field (AC-2.6).
- The Linear connector, which drops unmapped states without logging already.

## Learning hypothesis

Confirms, if it succeeds: the warning was load-bearing for nothing — the drop is documented behaviour
and no other surface reads it.
Disproves, if it fails: something else depends on the message — a validation path, a support runbook, or
a test outside `ServiceNowTeamSyncTest`. Checked and clean at DISCUSS time (no ADR, no docs page, no
other test references it), so a failure here means the search missed a caller.

## Acceptance criteria

AC-2.1 … AC-2.6 in `feature-delta.md`. The load-bearing one:

- **AC-2.2** — silence at *every* level. A Debug line is not an acceptable compromise (D5): the moment
  someone raises the log level to chase a real problem is exactly when the noise costs most.

## Dependencies

None.

## Reference class

`docs/concepts/worktrackingsystems/servicenow.md:206` — the shipped guidance this restores consistency
with: "Items in unmapped states are not tracked by Lighthouse and will not affect your metrics."

## Pre-slice SPIKE

None.
