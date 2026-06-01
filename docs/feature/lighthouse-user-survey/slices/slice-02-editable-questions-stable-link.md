# Slice 02: Editable questions without a link change

**Feature**: lighthouse-user-survey
**Repo**: website (`/storage/repos/website`)
**Stories shipped**: US-03
**Estimate**: ~0.5 day / ≤ 4h

## Goal

Harden the first-class capability that the question set is **data/config**, editable without
changing the `/survey` route or breaking previously-shared links — and without breaking the
dashboard's ability to read both old and new responses. This is the epic's explicit
"questions easily editable without a link change" requirement, made executable.

## IN scope

- Questions defined as data/config (the single source of the rendered questions).
- Editing the question config changes what `/survey` shows, with NO route/URL change.
- Old responses (prior question set) remain readable alongside new ones in the dashboard (no
  schema break for the survey `kind`).
- A verification path for the boolean capability KPI (edit questions → confirm `/survey` URL
  unchanged + old links still resolve).

## OUT scope

- A question-authoring UI (editing config by hand / file is acceptable this round).
- Versioned question history / analytics joins across question versions.
- Trial opt-in (slice 03), in-app nudge (slices 04-05).

## Learning hypothesis

**Confirms if it succeeds**: questions can evolve over time with zero churn to shared links and no
loss of past responses — so the placeholder questions can be replaced by the real set later
without rework.
**Disproves if it fails**: changing questions forces a route change or strands old responses →
the link-stability requirement isn't met and the data model needs rethinking.

## Acceptance criteria

See US-03 in `../feature-delta.md`. Slice specifics:

- Edit the question config → `/survey` serves updated questions at the same URL.
- A link shared before the edit still resolves to `/survey` and shows current questions.
- Dashboard reads both pre-edit and post-edit responses without error.

## Dependencies

**Hard**: slice 01 (the page + store + dashboard read exist).

## Production data requirement

**Preferred.** Verify against the real responses already collected in slice 01 (old question set)
plus a fresh edit.

## Dogfood moment

Edit a placeholder question, redeploy, confirm the Slack-shared link from slice 01 still works and
shows the new question; confirm old responses still render in the dashboard.

## Pre-slice spike candidates

- 15 min: confirm the response payload shape tolerates added/removed questions (no rigid column
  per question) so old + new responses coexist.
