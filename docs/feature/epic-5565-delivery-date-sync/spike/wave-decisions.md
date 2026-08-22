# SPIKE Decisions — epic-5565-delivery-date-sync (slice 00, ADO #5827)

Full evidence, verbatim bodies and the was/now table live in
`slices/slice-00-spike-jira-release-reality-check.md` under `## SPIKE OUTCOME`. This file records the
decision, not the measurements.

## Assumptions tested

Four, per the brief: the `fixVersions` shape (Q1), Release enumeration (Q2), the Version-write
permission bar and its failure shape (Q3), and the description as a write surface (Q4).

## Probe verdict

**WORKS.** Releases are enumerable with their dates, Epics carry `fixVersions` at the tracked level,
the description is a plain-text surface that round-trips byte-identically, and the write permission is
held by any licensed user on a default company-managed project. Slices 04-05 are cleared to be designed.

Four assumptions were wrong and are corrected in the was/now table: the refusal is **HTTP 400, not
403**; `fixVersions` elements are full version objects rather than `{id, name}`; this Epic's own DISCUSS
note that the dated demo Release sits six months in the past is **disproven** — it is six months in the
future, so D5's past-date path is not exercised by default; and the Releases list column **collapses the
description's newlines**, so the list is a teaser rather than the read path - though the release **detail
view honours them**, which is what keeps the multi-line block viable.

## Promotion decision

**DISCARD** — findings only, no walking skeleton.

Not a judgement call. The brief scopes this slice as a timeboxed PROBE that "ships findings, not code",
and **AC-00.3 forbids committing production code from it**. Promoting a probe into a walking skeleton
here would violate the slice's own acceptance criteria. The end-to-end value path is slice 01a's to
build, against a design that this probe has now unblocked.

Probe scripts deleted. Nothing was left in `src/`.

## Design implications

1. Refusal detection keys on **400 + `errorMessages[0]`**, never on 403. The message is human-readable
   and should be surfaced verbatim rather than paraphrased.
2. The description is plain text with literal `\n` — no ADF builder, no markup escaping.
3. The 16 KB ceiling is far away, but the write must be **idempotent by marker replacement** rather than
   append; the description-spam failure bites long before the ceiling does.
4. The permission bar is cleared by most credentials, so the refusal path is an **exception report**, not
   a gate. Do not make a user prove permission before offering the feature.
5. Marker-keyed replacement survives a hand edit — measured, not assumed — so the write replaces its own
   previous block rather than appending.
6. The block stays **four elements over multiple lines** - the detail view renders them as lines.
7. **Delimiters are emoji, anchored on the opening line**: `🔮 Lighthouse forecast - updated <date>`
   to open, a lone `🔮` to close. Measured to survive both the API and a hand edit, with no
   shortcode substitution. Detection matches the opening *line*, never the bare emoji, so a stray emoji in
   human text cannot be mistaken for a marker. Unbalanced or missing markers mean **append a fresh block,
   never infer a range to delete** - a visible duplicate beats silently eating a user's text.
5. Matching by version **id** is confirmed. Noted but deliberately not designed on: the version's
   `releaseDate` arrives inline on the issue read, so a later optimisation could avoid a lookup.

## Constraints discovered

- Version `description` must be under **16,384 bytes** (`PUT` refuses 32,768 with a named error).
- A refused write is **dropped whole** — no partial application, so no recovery logic is owed.
- **`ADMINISTER_PROJECTS` is per project, not per site.** The same identity holds it on a
  company-managed project and lacks it on a team-managed one with restricted access. Any capability
  check must be asked per project, never cached per connection.

## Carried open items

| Item | Why it is open | Owner |
|---|---|---|
| Jira Data Center version path | No reachable DC instance. Out of scope by the brief; verify post-release, same posture as `quiet-jira-writeback` | Post-release |

## Cleanup owed

None outstanding. `SPIKEPRM` version 10040 served the UI round-trip and emoji tests and has been
deleted. The demo project was touched once during the permission measurement and was reverted the same
minute - `LGHTHSDMO` version 10004 is back to no description, as found.
