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

### Spike result (2026-10-15): POSITIVE — Cloud dates every link addition and removal

**Run**: `letpeoplework.atlassian.net`, two Stories with no links before or after, `LGHTHSDMO-30` and
`LGHTHSDMO-31` (no test or slice-01 data uses either). `POST rest/api/2/issueLink` (type `Blocks`), ~60 s,
`DELETE rest/api/2/issueLink/10215`, ~60 s, `POST` again (link `10216`), then
`GET rest/api/2/issue/{key}?expand=changelog` on both ends. The connector reads v2, so v2 was read rather
than the v3 the plan above names. Finally `DELETE rest/api/2/issueLink/10216` and a re-read of
`fields=issuelinks` on both: `[]` on each. Nothing else on the instance was touched.

**Captured `Link` history entries** (newest first, as Cloud returned them; `author` left out):

| Issue | history `id` | `created` | `field` | `fieldtype` | `from` | `fromString` | `to` | `toString` |
|---|---|---|---|---|---|---|---|---|
| LGHTHSDMO-30 | 95860 | `2026-10-15T18:52:24.496+0200` | `Link` | `jira` | `null` | `null` | `LGHTHSDMO-31` | `This work item is blocked by LGHTHSDMO-31` |
| LGHTHSDMO-30 | 95858 | `2026-10-15T18:51:21.741+0200` | `Link` | `jira` | `LGHTHSDMO-31` | `This work item is blocked by LGHTHSDMO-31` | `null` | `null` |
| LGHTHSDMO-30 | 95856 | `2026-10-15T18:50:18.595+0200` | `Link` | `jira` | `null` | `null` | `LGHTHSDMO-31` | `This work item is blocked by LGHTHSDMO-31` |
| LGHTHSDMO-31 | 95859 | `2026-10-15T18:52:24.441+0200` | `Link` | `jira` | `null` | `null` | `LGHTHSDMO-30` | `This work item blocks LGHTHSDMO-30` |
| LGHTHSDMO-31 | 95857 | `2026-10-15T18:51:21.679+0200` | `Link` | `jira` | `LGHTHSDMO-30` | `This work item blocks LGHTHSDMO-30` | `null` | `null` |
| LGHTHSDMO-31 | 95855 | `2026-10-15T18:50:18.558+0200` | `Link` | `jira` | `null` | `null` | `LGHTHSDMO-30` | `This work item blocks LGHTHSDMO-30` |

Each entry holds exactly one item. The items carry no `fieldId`.

**Answers to DESIGN's open shape question**

- **Field**: `field: "Link"`, `fieldtype: "jira"`.
- **Where the key is**: the counterpart's key sits in `to` (on an addition) or `from` (on a removal), in the
  shape `PROJECT-123`. It is not a link id. The sentence in `toString` / `fromString` names the key too.
- **Addition and removal**: an addition has an empty from side, with `to` and `toString` set. A removal is
  the mirror: `from` and `fromString` set, the to side `null`.
- **Both ends**: yes. Each issue gets its own entry, naming the other issue, with its own side's phrase
  (`is blocked by` on one end, `blocks` on the other). The two ends' timestamps differ by a few tens of ms.
- **`created`**: a timestamp with an offset, with millisecond precision. The offset is the **reading
  account's zone** (`+0200`), **not UTC**. So whoever reads the link date must convert it to UTC
  (`AdjustToUniversal`). `IssueFactory` reads status-change dates today with `AssumeUniversal` alone, which
  turns an offset into *local* time. The link-date reader must not copy that.
- **Order**: Cloud returned the histories newest first, so a reader that walks them in the order given
  gets the removal and re-adding backwards. `Entries_out_of_order_are_read_by_when_they_happened` covers
  this.
- **Sentence wording**: Cloud now says "This work item …", not "This issue …". Nothing should match the
  phrase. Only the key, which the item carries anyway, is read.

**Builders**: `JiraWireFormat.ALinkAddedAt` and `ALinkRemovedAt` already built this item shape. They are
no longer marked provisional. Every history entry the fixtures build now stamps `created` at a `+0200`
offset, as Cloud does. The signatures are unchanged, so no caller changed.
`ALinkAddedNamingTheCounterpartOnlyInItsSentence` (a link id in `to`) is **not** what Cloud wrote. It is
kept, and described as a shape reported for other Jira versions, so that a history in that shape still
dates its links. Data Center stays unverified (risk R2).

**Gate**: positive. 02-02, 02-03, 02-04, 02-05, 03-01 and 03-02 keep their main branch, and "oldest link"
stays in the copy.

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
