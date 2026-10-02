# Upstream changes — DESIGN → DISCUSS artefacts (epic-5510-5881-refinement)

**From**: DESIGN (Morgan, 2026-10-02, PROPOSE). **For**: DISTILL to apply when it writes the acceptance tests; the
story text in `feature-delta.md` is left as DISCUSS wrote it (append-only). Each row says what to read instead.
Rationale: `feature-delta.md` → "Wave: DESIGN / [REF] Changed Assumptions".

| Story / AC | As written | Read as | Source |
|---|---|---|---|
| AC-2.1 | "in the same backlog order the Team's forecasts use" | "in backlog order: the work tracking system's rank (`Order`) compared the way Feature order is compared, ties by id" | DSN-6, ADR-215 |
| AC-2.2 | "id + link, name, state, category, age" | "… age for Doing rows (Work Item Age); To Do rows show no age" — pending MQ-4 | DSN-6 note, MQ-4 |
| AC-5.1 | "same history window and settings as its forecasts" | unchanged in wording; explicitly **includes the Team's forecast filter** when set — pending MQ-1 | DSN-8, ADR-215 |
| AC-5.5 | "facts (counts, band, date, verdict enum)" | facts also include `lowPercentile`, `highPercentile`, `horizonWorkingDays`, `isRefinementDay`, `unavailableReason`, `lineAfterPosition`, `fewerListedThanHigh` | DSN-9 |
| AC-10.1 | "over the Team's metrics window" | "over the Team's Throughput history window" | DSN-10 |
| AC-11.2 | "Auth off: name per browser, required once, editable" | "… name **and a random voter key** per browser; 'my vote' and take-back follow the key, not the name" | DSN-12, ADR-216 |
| AC-11.4 | "Readers may vote (TeamRead)" | "Readers may vote (`TeamContribute`, which equals Team read)" | DSN-13, ADR-217 |
| AC-11 (new error paths for DISTILL) | — | vote on a Work Item not in a refinement state → refused `work-item-not-in-refinement`; auth off without a name → `voter-name-required` | DSN-12, driving ports |
| AC-12.2 / DD-11 | "comment-only marks open question" | "… while the asker has no current vote; voting clears it" | DSN-15, ADR-218 |
| AC-13.2 / DD-5 | precedence rule → stage → votes | "a matching rule decides stage **and** readiness (votes cannot lift a rule-Waiting row); several matching rules → Ready > Being refined > Waiting" | DSN-14, ADR-218 |
| AC-14.2 | "no bypass except in presenter mode" | "presenter mode reveals splits only to Team admins when RBAC is on (everyone when auth or RBAC is off)" — pending MQ-2 | DSN-16, ADR-218 |
| AC-15.2 | "TeamRead suffices — flagged for DESIGN" | "the write is guarded by `TeamContribute`, mapped to the Team read predicate; non-readers get 404" | ADR-217 |
| US-09 pitch / AC-9.3 | `lighthouse teams refinement 3`, `get_team_refinement` | `lh refinement get --team-id 3`, `lighthouse_team_refinement_get` | DSN-21 |
| US-17b pitch | `lighthouse teams refinement vote 3 GR-051 --yes-but "…" --as "Ana Lima"`, `vote_on_work_item` | `lh refinement vote --team-id 3 --work-item GR-051 --answer yes-but --comment "…" --as "Ana Lima"`, `lighthouse_team_refinement_vote` | DSN-21 |
| US-17b scenario 2 | "refused with a message asking for the name" | the CLI refuses before calling; the MCP tool and the server refuse with `voter-name-required` | DD-19 (b) verdict |
| US-17b (mcp-http) | — | the hosted MCP bridge refuses votes on an auth-off instance — pending MQ-3 | DD-19 (b) verdict |
| AC-18.3 | "entries saved while presenting are marked live" | `channel = LiveSession` on each entry | DSN-17 |
| DISCUSS checklist, "EF migrations" | 01, 11, 13, 03, 04, 07, 08 | **01 and 11 only** | ADR-214, DSN-11 |
| Slice 01 OUT | "Terminology key (02)" | the `refinement`/`refinements` keys land in **01** (the brief allowed DESIGN to move it) | DSN-20 |

No story changes scope; no slice changes order.
