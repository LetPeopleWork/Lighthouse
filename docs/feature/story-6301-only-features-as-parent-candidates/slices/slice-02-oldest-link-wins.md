# Slice 02 — A Work Item linked to several Features counts toward the oldest link's Feature, every refresh

**Feature**: story-6301-only-features-as-parent-candidates · **ADO**: #6301 · **Story**: US-02 · **Estimate**: ~6h (+ ≤ 2h spike)
**Reference class**: git's `--first-parent` (DIVERGE, Prior art): one stable line through a graph whose nodes
have several parents, decided by an order recorded when the edge was made.

## Goal

At Team grain, when narrowing leaves two or more stored Features, the one whose current link was added first
is the parent, on every refresh, whatever order Jira lists the links in.

## IN scope

- `IssueFactory` reads the changelog already fetched and dates each candidate's current link; a link removed
  and added again counts from its re-adding (AC-2.3, AC-2.6).
- One pure pick on the shared, tracker-neutral model: dated beats undated; earliest link date; among undated,
  earliest Feature `CreatedDate`; then lowest key, ordinal (AC-2.1, AC-2.2).
- The placed line says which rule decided, with the date where there is one, and names the candidates set
  aside (AC-2.4).
- Same links and stored Features → same parent, full and delta refresh (AC-2.5).

## OUT of scope

- Portfolio grain (slice 03).
- Any new Jira request; a truncated changelog uses the existing full re-read (AC-2.6).
- Telling the administrator *why* the customer's tracker has two Feature links; Lighthouse only says which won.

## Learning hypothesis

**Disproves, if it fails**: A3 — that the oldest link names the Feature the customer calls the item's parent
("birth parent").

**The measurement**: ask Steve for real Work Items linked to two or more Features, ideally three or more,
and which Feature the customer calls each one's parent; compare with the placed line after the upgrade.
**Denominator**: the Work Items Steve names that the placed line shows as placed by the oldest link (not by
narrowing alone, and not by a fallback). **Pre-registered threshold**: every one of them agrees confirms;
**one disagreement in that denominator reopens Q1** with the maintainer, whatever the denominator is. With
fewer than three items the result is recorded as directional only. Record the number of agreements and the
denominator here. Also measured on our own data: a property test permuting link order and repeating the
refresh, **0 flips** over every permutation of the test's candidates (`OUT-PFIL-never-a-guessed-parent`,
rewritten).

**Confirms, if it succeeds**: the rule matches the customer's mental model, and the not-placed figure at
Team grain shrinks to items with no stored Feature among their links.

## Pre-slice SPIKE (≤ 2h) — does the Jira changelog date a link addition? (A4)

On `letpeoplework.atlassian.net`, project `LGHTHSDMO`: add a **"Blocks"** link between two issues, remove
one and add it back, then read `GET /rest/api/3/issue/{key}?expand=changelog` on **both** ends. Record here:
the item's `field` / `fieldtype`, whether `to` / `from` carry the counterpart key, whether both issues get an
entry, and the history's `created` timestamp.

**Gate**: no dated entry on Cloud → the slice ships the undated fallback only (created date, then key), the
words "oldest link" leave every line and label, and the maintainer is told before DISTILL pins them. Data
Center stays unverified (no instance; risk R2); the fallback still gives a deterministic answer there.

Spike result: _to be recorded_.

## Acceptance criteria

AC-2.1 through AC-2.6 in `feature-delta.md` (US-02).

## UI

None new. Refresh History figures from slice 01 move: "not placed" falls, "placed" rises. Log line copy and
the fallback brackets per `feature-delta.md`, "Wave: DISTILL / [REF] Maintainer decisions: UI sketch review
(2026-10-15)".

## Data

- **Dogfood**: on `LGHTHSDMO`, link one issue with "Blocks" to two demo-Portfolio Features a few minutes
  apart; refresh twice, the second time after reordering nothing — same Feature both times.
- **Production**: Steve's customer (A3 question above).

## Dependencies

Slice 01 (the placing step and split count). Spike above.

## Dogfood moment

Same day: the item that slice 01 left "not placed" with two Feature candidates now reads
`… under <Feature> (oldest link, <date>; set aside <other>)` in the Task Manager, and the not-placed figure in
Refresh History disappears for that Team once the window rolls over.

## Docs at finalize

`docs/teams/edit.md:403` describes the rule and its fallbacks in the configurable terms.
