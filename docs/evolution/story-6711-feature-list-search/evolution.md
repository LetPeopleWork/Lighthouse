# Story 6711 — Search on the feature list

**Story**: ADO #6711 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6711), no parent Epic.
**Route**: spine waves with nwdes DELIVER, size S, rigor profile `standard`. The maintainer skipped DISCOVER and
DIVERGE explicitly on 2026-11-27; DISCUSS, DESIGN, DEVOPS and DELIVER ran, DELIVER in AFK mode.
Product, design and acceptance facts: `docs/feature/story-6711-feature-list-search/` (`handoff.json`, `brief.md`,
`architecture.md`, `acceptance.md`, `feature-delta.md`).

## What shipped

Every full feature list (the Features page, a Portfolio's and a Team's Features tab, a Delivery's Work Items tab)
has a **Search Features** box at the left of its toolbar. Typing part of anything the list shows (a Feature's ID,
name, parent, state, Portfolios or Teams) narrows the list, in any letter case. A search that matches nothing says
`No Features match "<text>".`, distinct from the empty-list message; the search works together with column
filters and goes back to the first page. The box starts empty on every visit. The Name column's own filter now
also finds a Feature by its ID, and sorting by name still orders by name alone.

The box is an opt-in `search` prop on the shared `DataGridBase` that drives MUI's own quick filter; every column
is matched by the text it shows (`quickSearch.ts`), so a yes/no or a count never matches. A name-only usage-data
event, `FeatureListSearched` (33), counts the first search of each visit to a list.

| Slice | Delivers | Commits |
|---|---|---|
| S1 | Search box on the Features page (ID, name, parent); usage event | 8b869f15d, ace95b800 |
| S2 | No-match message; search with column filters; first page | 990b2e9c2 |
| S3 | The box on the Portfolio, Team and Delivery lists | 32d680432 |
| S4 | Name column filter finds the ID; sort stays by name | 99979ea38 |
| Final | Refactor, docs, parent-ID test, mutation kill tests | f37292d24, 40c0d4693, 045bb3905, 67f0adefc |

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done: "Searching the list" in `docs/features/features.md` (40c0d4693), in the configured terms. It also names the Portfolio, Team and Delivery lists that carry the same box. |
| Screenshots | **Deferred** (AFK 15-minute default, 2026-11-27 21:02, no reply): `docs/assets/features/featuresview.png` does not yet show the box. It is not wrong, only missing the box. Re-run its `@screenshot` test per theme with `--grep` on the next docs pass or at `/release`. |
| Demo data | N/A: the demo data already fills every feature list; search needs nothing new. |
| Website assets | N/A: no asset the website hot-links from `docs/assets/` changed (the deferred screenshot would be the first). |
| Clients (CLI, MCP) | N/A: no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | N/A: Work Items context only, frontend only; no new concept, term, edge or domain event. |
| Usage-data event | Done: `FeatureListSearched = 33`, name only, emitted once per list per visit (S1), listed in `docs/settings/usagedata.md`. |
| Mutation | Done: frontend **90.14 %** (128 of 142) after one kill round, from 76.76 %; six new test files, no oracle file changed. 13 survivors accepted with reasons and one left unproven, in `docs/feature/story-6711-feature-list-search/mutation/results.md`. Backend N/A: one appended enum value, no logic. |
| Release notes | N/A; written at /release. |

## Decisions taken in AFK mode, worth a look

- Columns with no text value (Progress, Forecasted Start and Completion, Likelihood, Warnings, Active Work) are not
  searched, though the maintainer's answer was "every visible column". The story's named fields are all covered.
- `noMatchMessage` is optional on the shared grid's `search` prop (the design showed it required).
- The 5-minute review windows were skipped for this story (maintainer, 19:46).
- The usage-data event was designed as 31. Trunk took 31 and 32 (the Within SLE events) while this story was in
  flight, so at the rebase onto `origin/main` it became 33; no published value was renumbered. The design documents in
  `docs/feature/story-6711-feature-list-search/` still say 31 and are history.

## Gates

- Whole-candidate review (Haiku): rejected once with two findings: no test typed a parent's ID, and there was no clean
  full `pnpm test` run. A parent-ID test was added to the S1 oracle (proven by a deliberate break, oracle re-approved),
  and a quiet full run went green (716 of 716 files, 10,471 of 10,471 tests). Re-review accepted with 0 findings.
- EXAMINE (Haiku, source-blind): E1 to E10 `not_examined`, an observation gap: the only public probe is the frontend
  test runner, and the examiner counts its output as producer evidence, not an independent capture; no console renders
  the lists with fixture data. No violation was observed. nwdes refuses every close that is not accepted
  (`RoleNotAccepted`), so, as for Stories 6601, 6648 and 6651, the verdicts live here.
- After the rebase onto `origin/main`: `dotnet build` 0 warnings; filtered `dotnet test` 15,605 passed, 2 failed under
  load (an ArchUnit regex timeout and the test-host release check), both classes green alone (31 of 31).
  `pnpm build` clean; full `pnpm test` 10,549 of 10,549 passed, with 9 time-budget offenders in files this story does
  not change (one renders the feature list); those 7 files alone took 0 offenders (90 of 90).

## Left open

- The `featuresview.png` screenshot refresh (see the checklist).
- An examine probe that renders a feature list with fixture data would let EXAMINE judge frontend stories
  independently; none exists yet.
- Columns without text (Progress, forecasts, Likelihood, Warnings, Active Work) are not searchable; revisit if
  users ask.
