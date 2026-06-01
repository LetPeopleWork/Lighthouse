# Slice 03: Trial opt-in (email → manual signal)

**Feature**: lighthouse-user-survey
**Repo**: website (`/storage/repos/website`)
**Stories shipped**: US-04, US-05 (trial-requests view)
**Estimate**: ~1 day / ≤ 6h

## Goal

Let a respondent optionally tick "I'd like a free premium trial" and volunteer an email, recording
a **trial-request signal** (`wantsTrial`) + the email in 5123's lead-capture — with NO auto-issued
license (D4) — and surface those requests in the dashboard's trial-requests view for a human to
action. The volunteered email is the ONLY PII stored, and only on explicit opt-in (D3).

## IN scope

- Optional trial opt-in control + email field on `/survey`, clearly labeled as optional and as the
  only place personal data is asked.
- On opt-in + valid email + submit → a lead-capture row with `wantsTrial` + email, tagged to the
  survey source/kind (extends 5123's lead-capture, D6).
- No opt-in → no email collected or stored; response stays anonymous.
- No license auto-issuance (signal only, D4).
- Inline validation for an invalid email without losing the rest of the answers.
- Partial-write handling: if the response saved but the lead row failed, tell the user the trial
  request specifically didn't go through so they can retry the opt-in.
- Dashboard trial-requests view listing each request with its email.

## OUT scope

- Auto-issuing or provisioning any license (explicitly out, D4).
- Emailing the user / any outbound automation (a human follows up out-of-band).
- In-app nudge (slices 04-05).

## Learning hypothesis

**Confirms if it succeeds**: community users will self-select into a premium trial by volunteering
an email, and the only-PII-on-opt-in discipline holds end-to-end.
**Disproves if it fails**: nobody opts in (no trial demand via this channel), or PII leaks into
non-opt-in responses (discipline broken → fix before any further rollout).

## Acceptance criteria

See US-04 and US-05 (trial-requests view) in `../feature-delta.md`. Slice specifics:

- Opt in + valid email + submit → lead row with `wantsTrial` + email; thank-you notes human
  follow-up; NO license created.
- No opt-in + submit → no email stored anywhere; response anonymous.
- Invalid email on opt-in → inline validation; other answers preserved.
- Maintainer opens trial-requests view → request listed with email.

## Dependencies

**Hard**: slice 01 (page + store); 5123's lead-capture + email-capture handling available (D6).

## Production data requirement

**Preferred.** Real opt-ins from the Slack-shared link populate the trial-requests view.

## Dogfood moment

Submit a real opt-in from the project's own browser → confirm a `wantsTrial` lead row appears in
the dashboard trial-requests view with the email, and that NO license was created.

## Pre-slice spike candidates

- 15 min: confirm 5123's lead-capture row shape accepts `wantsTrial` + email tagged to a survey
  source/kind without a migration.
