# Slice 03 — Teaser + email gate unlocks the full breakdown

**Goal**: After computing the score, show a TEASER (number + band, no email), gate the FULL band-specific breakdown behind an email-capture form, and on valid email persist the volunteered email with the response and unlock the breakdown in place.

## IN scope

- Results restructured into TEASER (score/100 + band + anchor, always visible) and GATED breakdown.
- Email-gate form (react-hook-form + zod email validation), inline validation, no submit on invalid.
- On valid submit: write/extend the Supabase row with `email` (the only PII), then unlock the breakdown.
- Breakdown content per band (both-pillars explanation + named next rung + band-specific CTA set), keyed by band.
- Degrade-open: if the email write fails, still unlock the breakdown, retry once, non-blocking notice.

## OUT scope

- Admin dashboard (Slice 04).
- Scroll-reveal / Navigation restyle (Slice 05).
- Per-pillar sub-scores.

## Learning hypothesis

"Showing the number first and gating only the why/next-step earns enough trust that a meaningful share of teaser-viewers trade an email."
Disproved if: email-capture rate among teaser-viewers is near zero in dogfooding/early traffic, indicating the teaser gives away too much or the gate feels unfair.

## Acceptance criteria

- [ ] On completion the teaser shows score/100 + band with NO email required.
- [ ] The full breakdown is hidden until a valid email is submitted.
- [ ] Invalid email (e.g. "maria@", "not-an-email") → inline error, no write, breakdown stays gated.
- [ ] Valid email (e.g. maria.santos@acme.example) → row carries the email, breakdown unlocks in place.
- [ ] Breakdown content exists and renders for ALL four bands; Community CTA present in all four.
- [ ] Email write failure still unlocks the breakdown (degrade open) with a non-blocking notice.

## Dependencies

- Slice 01 (teaser/breakdown content + bands), Slice 02 (the row to extend with email).

## Effort estimate

~6h. **Reference class**: a gated form with zod validation + a Supabase update/insert — comparable to existing form-driven flows in the website repo.
