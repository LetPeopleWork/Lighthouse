# Slice 00 (SPIKE) — Jira Release reality check

**Goal**: find out, against a real Jira Cloud instance, whether a Release can carry a Delivery on the
terms D1, D7 and D8 assume — before any of them is designed.

**Type**: timeboxed PROBE, 2 hours. Ships findings, not code. Any throwaway script is deleted.

## Why this exists

`quiet-jira-writeback` locked six decisions on assumed Jira behaviour, then a spike broke three of them
and deleted a whole slice. Three decisions here rest on the same kind of assumption, and the cheapest
place for them to be wrong is a probe.

## IN scope — the four questions

**Q1 — Confirm the shape of `fixVersions` on the items Lighthouse tracks.**
**Downgraded from a gate to a confirmation (user, 2026-08-22): setting the version on the Portfolio's
Features is a convention the customer owns, not something Lighthouse has to discover or work around.** A
Feature without one simply is not in the Release, the same way a Feature that fails a rule is not in a
rule-based Delivery. The handler matches at the level Lighthouse tracks and does not roll up from
children.
What still needs confirming, because it decides the matching code and not the design: the field's JSON
shape on a real issue — an array of objects, each with `id` and `name` — and that matching on the
version **id** works against it.
Evidence to produce: the `fixVersions` value from one real Feature, verbatim.

**Q2 — Can the Releases be enumerated with their dates, and on both deployments?**
`GET rest/api/3/project/{key}/versions` on Cloud. Confirm the field names actually returned
(`id`, `name`, `releaseDate`, `released`, `archived`, `startDate`?), whether `releaseDate` can be absent,
and what the Data Center path is (or that it differs) — recorded, not built for.
Evidence: one verbatim response body.

**Q2 partially answered already (2026-08-22, live read of the demo instance).** `GET
rest/api/3/project/{key}/versions` returns objects carrying `self`, `id`, `name`, `archived`,
`released`, `projectId` — and `releaseDate`, `userReleaseDate` and `overdue` **only when a date is
set**. Two of the three Releases had no date at all, which is what D11 exists for; `description` was
absent on all three, so slice 04 must create the field rather than append to it. `self` confirms
`/rest/api/3/version/{id}` as the PUT target D7 assumes. Jira computes `overdue` itself, and the one
dated Release was six months past — D5 is exercised by the very first Release anyone binds.
Still open on Q2: the Data Center path.

**Q3 — What permission does writing a Version need, and what happens without it?**
`PUT rest/api/3/version/{id}`. Establish the permission (expected: Administer Projects) and the failure
shape without it — 403 with a body, silent no-op, or partial write. This is the D7/D8 permission bar and
it decides whether slice 05 is a real slice or a log line.
Evidence: one successful write and one refused write, both bodies captured verbatim.

**Q4 — Is the Version description a usable surface, and does a delimiter survive a round trip?**
**Partly answered 2026-08-22**: `description` is a column on the Releases page, so it renders where the
reader already is. What remains is what the block has to survive, now that its content is fixed at four
elements over multiple lines (attribution, write date, 70/85/95, target likelihood):

- its size limit, and its markup — wiki markup, ADF, or plain text,
- whether newlines survive storage and render as newlines,
- **whether a delimiter survives a round trip through the Jira UI.** This is the one that decides the
  slice: if a human opens the Release, edits the description, and Jira rewrites or strips the markers,
  the next write cannot find its own previous block and starts appending. That failure turns the feature
  into description spam, and it is worth provoking on purpose rather than assuming.

Evidence: write a candidate block, screenshot how it renders both in the list column and on the Release,
then edit the description by hand in the UI, re-read it over the API, and check the markers are intact.

## OUT of scope

- Any production code, any migration, any endpoint. Probe scripts only.
- Jira Data Center. Record what is knowable; verify post-release, same posture as `quiet-jira-writeback`.
- Deciding the fallback if Q4 fails — that is a DESIGN call informed by this.

## Learning hypothesis

**Disproves D7/D8** — now the whole point of this probe — if Q3 shows the permission bar for writing a
Version is one most credentials will not clear, or Q4 shows the description is not a surface a human
reads. Either finding detaches slices 04-05 from the Epic; inbound still ships whole.
**Disproves the matching code, not the design,** if Q1 shows `fixVersions` is shaped differently from
the assumed array of `{id, name}`.
**Confirms** the Epic can proceed as sliced if all four answer as assumed.

## Acceptance criteria

- **AC-00.1** — All four questions have a written answer with verbatim evidence, in this file, under a
  `## SPIKE OUTCOME` heading with the date.
- **AC-00.2** — Every answer that contradicts a locked decision is called out by decision number, in a
  was/now table, exactly as SPIKE-03 did.
- **AC-00.3** — No production code is committed by this slice.

## Dependencies

`letpeoplework.atlassian.net` credentials with, ideally, two identities: one holding Administer Projects
and one without, so Q3 can measure both sides.

**Note on the Jira API key**: hand-exploring the Jira API from this machine has previously tripped rate
limits that surface as unrelated-looking backend test failures. Keep the probe's call volume low and do
not run it alongside a backend suite.

## Effort

2 hours, timeboxed — shorter than first scoped, because Q1 stopped being an open question. Q3 and Q4
are the ones worth the time; if they consume the box, stop and report, because they are what decides
whether slices 04-05 exist.

## Reference class

`quiet-jira-writeback` slice 03 (`slice-03-spike-jira-notification-suppression.md`) — same shape, same
instance, same "assumed Atlassian behaviour" risk. It ran over its box because it chased a Data Center
answer it could not get; this one explicitly does not.

---

## SPIKE OUTCOME — 2026-08-22

Run against `letpeoplework.atlassian.net` with the two identities the repo already provisions for
integration tests: the site-admin `atlassian.pushchair@huser-berta.com`, and the deliberately
under-permissioned `benjamin@letpeople.work` that `JiraSuppressionRetryIntegrationTest` created for
SPIKE-03. Probe scripts were throwaway and are deleted. No production code committed (AC-00.3).

**Verdict: the Epic proceeds as sliced, and slices 04-05 are cleared to be designed.** Three of the
four questions answered outright; one half of Q4 needs a human at a browser (below). Two assumptions
were wrong, one of them recorded in this Epic's own DISCUSS notes.

### Q1 — `fixVersions` shape — CONFIRMED, and richer than assumed

`GET /rest/api/3/search/jql?jql=project = LGHTHSDMO AND fixVersion is not EMPTY&fields=fixVersions`
returns, verbatim, for `LGHTHSDMO-10` (an **Epic** — the level Lighthouse tracks):

```json
"fixVersions": [
  {
    "self": "https://letpeoplework.atlassian.net/rest/api/3/version/10006",
    "id": "10006",
    "description": "",
    "name": "Elixir Project",
    "archived": false,
    "released": false,
    "releaseDate": "2027-02-26"
  }
]
```

An array of objects, as assumed — but each element is the **full version object**, not the assumed
`{id, name}` pair. It carries `releaseDate`, `archived` and `released` inline. Matching on the version
**id** works. Epics carry the field directly, so the membership convention holds at the tracked level
with no roll-up, exactly as D1 assumes.

### Q2 — enumerating Releases — CONFIRMED on Cloud; Data Center still open

`GET /rest/api/3/project/{key}/versions` returns `self`, `id`, `name`, `archived`, `released`,
`projectId` always, and `releaseDate`, `userReleaseDate`, `overdue` **only when a date is set**.
`description` is omitted entirely when empty. `self` confirms `/rest/api/3/version/{id}` as the PUT
target D7 assumes. Two of three demo Releases carry no date, which is what D11 exists for.

Jira computes `overdue` itself — the probe version created with `releaseDate: 2026-03-01` came back
`"overdue": true` without being asked. D5 does not need Lighthouse to derive overdue-ness on the
remote's terms; it already arrives.

**Data Center path remains unverified** — no DC instance is reachable from here. Out of scope by the
brief; same posture as `quiet-jira-writeback`, verify post-release.

### Q3 — the write permission — ANSWERED, and the assumed failure shape is WRONG

`PUT /rest/api/3/version/{id}` by the under-permissioned identity, verbatim:

```
HTTP 400
{
  "errorMessages": [
    "You must have global or project administrator rights in order to modify versions."
  ],
  "errors": {}
}
```

**The refusal is HTTP 400, not 403.** Detection code that keys on 403 will not see it. The message is
human-readable and worth surfacing as-is rather than paraphrasing.

The write is **dropped whole** — a read-back with the admin identity showed the description byte-identical
to its pre-call value. No partial application, so no recovery logic is owed.

**The permission bar is low, which is the finding that matters.** The *same* non-site-admin identity:

| Project | Kind | `ADMINISTER_PROJECTS` | `PUT` version description |
|---|---|---|---|
| `SPIKEPRM` | team-managed, restricted access | `false` | **HTTP 400, refused** |
| `LGHTHSDMO` | company-managed, default scheme | `true` | **HTTP 200, succeeded** |

Measured both by `GET /rest/api/3/mypermissions` and by an actual write (since reverted). On a default
company-managed project any licensed user clears the bar. Refusal is the exception — a team-managed
project with restricted access — not the norm.

### Q4 — the description as a surface — ANSWERED on the API side; one half needs a human

- **Plain text, not ADF, not wiki markup.** A description written through `PUT` round-trips
  **byte-identical**, verified by equality assertion, not by eye.
- **Newlines survive storage.** `\n` is stored and returned as `\n`.
- **Delimiters survive storage.** `[lighthouse:forecast]` … `[/lighthouse:forecast]` came back intact
  around a four-element block (attribution, write date, 70/85/95, target likelihood).
- **Size ceiling is not a constraint.** 5,000 characters stored without truncation. 32,768 refused with
  a named error: `"The version description entered is too large, it must be less than 16,384 bytes."`
  The designed block is ~200 characters.

**The UI round trip — CLOSED, and it passes.** The maintainer opened `SPIKEPRM` version 10040 in the
Jira UI, edited the human sentence by hand, and saved. Read back verbatim:

```
'Human-written text that must survive. EDITED BY HAND.\n\n[lighthouse:forecast]\nForecast by Lighthouse - written 2026-08-22\n70%: 2026-09-15\n85%: 2026-09-29\n95%: 2026-10-13\nTarget 2026-10-01: 88% likely\n[/lighthouse:forecast]'
```

All eight newlines intact, both markers intact, only the intended edit applied. The edit control is a
real multi-line textarea and Jira stores what it is given. **Marker-keyed replacement is safe** — the
description-spam failure mode the brief feared does not occur.

**But the surface is worse than assumed — a finding the brief did not think to ask for.** The Releases
**list column collapses the newlines**. The description renders there as a run-on paragraph wrapped at
column width rather than at `\n`: observed directly in the UI, the column breaks as
`…text that must / survive. [lighthouse:forecast] / Forecast by Lighthouse - / written 2026-08-22 70%: / …`
— mid-sentence, not at the stored line boundaries. The block is legible only inside the edit dialog,
which is a modal nobody opens in order to read a forecast.

This contradicts the premise recorded in this brief on 2026-08-22, that `description` "is a column on
the Releases page, so it renders where the reader already is". It renders there. It does not render
*readably* there.

**The Release detail view does honour the line breaks**, which resolves it. The description panel on the
release page renders all nine lines as lines. So the surface is real and the multi-line block survives
as designed — it is the list column, not the description, that is the poor surface. Slice 04 keeps the
four-element block and treats the list column as a teaser rather than the read path.

### Q4b — the marker can be an emoji (maintainer request, measured the same day)

The bracketed `[lighthouse:forecast]` delimiters read as machine junk in a field humans also write in.
Tested whether an emoji can carry the same job:

```
Human-written text that must survive. EDITED BY HAND. Test 123

🔮 Lighthouse forecast, written 2026-08-22
70 percent: 2026-09-15
85 percent: 2026-09-29
95 percent: 2026-10-13
Target 2026-10-01: 88 percent likely
🔮
```

Written over the API, then hand-edited in the Jira UI and saved. Read back: both markers intact, stored
as the literal codepoint U+1F52E, **no shortcode substitution** (`:crystal_ball:` never appears), every
newline preserved, and only the human's edit applied. 210 bytes against a 16 KB ceiling.

**Emoji delimiters work.** The recommended shape, and the reason it is not a bare paired emoji:

- **Open** on the whole line `🔮 Lighthouse forecast - updated YYYY-MM-DD`; **close** on a lone
  `🔮`.
- Detection anchors on the **opening line**, not the bare emoji. A human will not type that line by
  accident, so marker collision stops being a practical concern — which a bare paired `🔮` cannot
  claim, since identical open and close markers cannot tell which half was lost when a user deletes one.
- **If the markers are unbalanced or absent, append a fresh block — never infer a range to delete.** The
  worst case becomes one visible duplicate block a user can remove, instead of Lighthouse silently
  eating text it did not write.

### AC-00.2 — answers that contradict a locked decision

| # | Was | Now | Consequence |
|---|---|---|---|
| D8 / Q3 | Permission bar assumed possibly prohibitive; slice 05's existence in doubt | Administer Projects confirmed as the bar, but **any licensed user holds it on a default company-managed project** | Outbound is viable for the common case. Slice 05 shrinks toward a log line plus a surfaced message — but does not vanish, because the restricted case is real and measured |
| D7 / slice 05 | Refusal assumed to be `403` | Refusal is **`400`** with the reason in `errorMessages[0]` | Any refusal detection keyed on 403 is wrong before it is written. Carry the message verbatim |
| D5 (this Epic's DISCUSS note, 2026-08-22) | "the one dated Release was six months past — D5 is exercised by the very first Release anyone binds" | The dated demo Release is `2027-02-26`, `overdue: false` — six months in the **future** | **Disproven.** D5's past-date path is *not* exercised by default. Exercising it needs a deliberately past-dated Release; the probe created one (`2026-03-01`, `overdue: true`) to confirm Jira reports it |
| Q1 shape | Assumed array of `{id, name}` | Full version objects, carrying `releaseDate`, `archived`, `released` inline | Matching by id is unaffected. Noted, not designed on: the version's date arrives on the issue read, so a future optimisation could avoid a separate lookup |
| Q4 surface (this brief, 2026-08-22) | "`description` is a column on the Releases page, so it renders where the reader already is" | The **list column collapses** the newlines; the **detail view honours** them | The block stays multi-line and keeps its four elements. The list column is a teaser, not the read path — do not design the block for it |

### Design implications for slices 04-05

1. Refusal detection keys on **400 + `errorMessages[0]`**, never on 403.
2. The block is plain text with literal `\n`; no ADF builder, no markup escaping.
3. A 16 KB ceiling means the write must still be *idempotent by marker replacement*, not append — the
   ceiling is far away but appending forever reaches it, and the spam problem bites long before.
4. Because the bar is cleared by most credentials, the refusal path is an **exception report**, not a
   gate — do not make users prove permission before offering the feature.
5. Marker-keyed replacement is safe against hand edits — a human editing around the block leaves it
   intact, so the write can find and replace its own previous block rather than appending.
6. The block stays **four elements over multiple lines**. The detail view renders them as lines; only the
   list column collapses them, and that column is a teaser.
7. **Delimiters are emoji, anchored on the opening line** - `🔮 Lighthouse forecast - updated
   <date>` to open, a lone `🔮` to close. Unbalanced or missing markers mean append, never infer a
   deletion range.

### Not done, and why

- **Jira Data Center path** — no reachable instance. Recorded, not built for, per the brief's OUT scope.
- Nothing else. Every question in the brief is answered, and the two follow-ups it raised - the detail
  view's rendering and the emoji delimiter - were measured the same day.
