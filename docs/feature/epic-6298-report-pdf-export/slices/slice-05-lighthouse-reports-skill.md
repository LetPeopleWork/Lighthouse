# Slice 05 — Lighthouse reports skill: show a Then & Now as a dashboard or document

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-05 (ADO: to be created) · **Estimate**: ~½d ·
**Tier**: Community · **Ships alone**: yes (builds on finished slice 04; with Q1 option c, on nothing) · **Repo**:
`lighthouse-clients/skills/` · `job_id: job-share-numbers-outside-lighthouse`

## Goal

With the skill installed, an assistant asked for a dashboard or a document of a report produces one that carries the
report's own numbers, dates and windows, in the instance's terms, attributed to Lighthouse, with any comment of its
own kept apart — the same way every time. Then & Now is the first (and today only) report type it covers.

## IN

- New skill folder, name `lighthouse-reports` (D24, Q6), `SKILL.md` with frontmatter `name` = folder name and a
  `description` that says when to use it (and points to the `lighthouse` skill for connecting).
- The Then & Now layout: dated header (owner, report name, Then window, Now window, the day the report was read);
  the summary groups (Improved, Got worse, Mixed, Unchanged); one block per shown metric with Then, Now, change and
  % change; the direction of good per metric; "—" with its reason.
- Guardrails (D25, Q3): quote values exactly, never rebuild Then from metric tools, dashes stay dashes, instance
  terms, attribution line "Data from Lighthouse by LetPeopleWork", own comments under their own labelled heading.
- Fallback (Q1 c): works from a report PDF or pasted values the user supplies, saying where the numbers came from.
- Written so a later report type adds a section, not a new skill.
- Eval cases (`evals/cases.json` + fixtures), run by hand before the release, 3 runs each; results recorded in
  `docs/feature/story-6217-lighthouse-skills/evals/<date>-lighthouse-reports.md`.
- Its own zip `lighthouse-reports-skill.zip` through the existing packing; listed in `docs/aiintegration.md` at
  finalize.

## OUT

Report types that do not exist yet · building the dashboard file format (the assistant chooses: document, HTML,
slide) · the PDF itself (01) · any client code (04).

## Learning hypothesis

**Disproves "a skill makes assistant dashboards faithful"** if in 3 eval runs a dashboard misquotes a value, drops
the dates or windows, turns "—" into 0, or mixes the assistant's comment into the numbers.

## Shared-artifact risk

The direction of good per metric lives in the frontend panels; the skill writes a second copy. MEDIUM risk: an eval
case checks it, and a later metric added to Then & Now must update the skill (note in the DELIVER checklist).

## Usage data

N/A, because a skill is text an assistant reads; nothing of ours runs to emit. Reads go through slice 04's tools.

## Acceptance criteria

AC-5.1 … AC-5.5 in `feature-delta.md` (US-05).

## Dependencies

Slice 04 (finished) under Q1 option (a). No dependency on slices 01–03, except that option (c) can read their PDF.
