# Slice 04 — Edit, delete, and validate recurring rules in settings

**Story:** US-04 · **Surface:** recurring-rules section (`PUT`/`DELETE /api/.../recurring-blackout-rules/{id}`) + form validation · **Direction:** rule lifecycle management

## Goal
One sentence: let a config-admin edit and delete existing recurring rules and be stopped at the form with clear messages when a rule is invalid (no weekday, interval < 1, end before start), so the recurring-rules screen is a complete, trustworthy management surface like the one-off periods table.

## IN scope
- `PUT /recurring-blackout-rules/{id}` and `DELETE /recurring-blackout-rules/{id}`, both carrying `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(SystemAdmin)]`; NotFound on unknown id (mirrors one-off `BlackoutPeriodsController`).
- FE edit dialog (pre-filled), delete confirmation dialog (mirrors the one-off period UX), and inline form validation.
- Validation rules (backend authoritative, surfaced in the form): at least one weekday selected; interval ≥ 1 week; if an end date is given, end ≥ start.

## OUT scope
- New recurrence semantics (done in 01/02).
- Downstream fan-out (done in 03).

## Learning hypothesis
**Disproves** "the recurring-rule lifecycle reuses the one-off period's edit/delete/validation UX 1:1 (same dialogs, same guard pattern, analogous ValidateDateRange)" **if** the recurrence-specific validations (weekday-required, interval≥1) do not fit the existing form/validation pattern and need a bespoke flow.
**Confirms** the management surface is complete and the screen is dogfood-ready.

## Acceptance criteria
- US-04 AC1: editing a rule (change Sat+Sun → Fri only) updates it; the forecast immediately reflects the new matching days.
- US-04 AC2: deleting a rule removes its days from the blackout set; a previously-stepped-over forecast date reverts (and a viewer can no longer see delete controls).
- US-04 AC3: saving a rule with zero weekdays is rejected with "Select at least one weekday for the rule to repeat on."
- US-04 AC4: saving a rule with interval < 1 is rejected with "Repeat interval must be at least 1 week."
- US-04 AC5: saving a rule whose end date precedes its start date is rejected with "End date must be on or after the start date."
- US-04 AC6: PUT/DELETE by a non-premium or non-SystemAdmin caller is rejected (403); an unknown id returns 404.
- Production-data: an admin correcting a mis-entered off-site rule (wrong weekday) and deleting a one-off rule that recurrence now supersedes.

## Dependencies
Slices 01–03 (entity, endpoint, expansion, downstream coherence). No new endpoint family — adds PUT/DELETE to the Slice-01 controller.

## Effort / reference class
~0.5–1 day. Reference class: the shipped one-off period edit/delete/validation (`Update`/`Delete`/`ValidateDateRange`) plus two recurrence-specific validations.

## Slice value
Observable: admin edits a wrong rule and it self-corrects in the forecast; an invalid rule is blocked at the form with a readable message. Completes the screen as a trustworthy, self-service management surface.
