# Lighthouse stays fast with many Features — Epic 6400

Delivered 2026-10-24 to 2026-10-27 in six slices, as six Stories that each ship on their own: #6404, #6406, #6407,
#6408, #6409 and #6410 (with Task #6405, the measurement harness, under #6404). Free, on every licence, no setting, no
migration. Raised by one self-hosted customer with Portfolios of 400 to 920 Features across 8 Teams.

On an instance that size, the screens that list Portfolios, Teams and Features read far more than they show, and much
of it grows with every Feature of the instance. On top of that, every Team or Portfolio update starts a forecast that
runs for minutes over all Features of the Teams involved, and while it ran the server stalled and pages waited.

What a user sees now: pages keep answering during a forecast round; the Overview and Settings open in a fraction of a
second; a Portfolio of 920 Features can open its Features list at all (it used to fail with HTTP 414); a Features list
that cannot load says so and offers Retry; a forecast round is about a fifth shorter; and a Portfolio over 100 Features
says on its edit page that it will be slow. The target "first Feature row under 2 s at 920 Features" (K2) was **not**
met (see Readings).

Personas: `platform-operator` (the stalls) and `delivery-forecaster` (the Features list). Job:
`job-reach-and-read-what-i-came-for-on-a-large-instance`, plus the reused
`job-operator-keep-lighthouse-usable-while-it-forecasts`.

## Key decisions

DISCOVER and DIVERGE
- The evidence was one reporter's description, a log excerpt and a read of the code. DISCOVER measured nothing, so
  the measurement became Task #6405, the first commits of Story #6404, not a slice of its own (it cannot be released
  alone).
- DIVERGE chose a stated hybrid: scope every read to what the screen shows (first at 3.95), plus a forecast round that
  leaves the request thread pool (a dedicated lane). K3, "pages stay as fast during a round as idle, no heartbeat
  warning", is an exit criterion of the Epic.
- Numeric triggers decided what a reading means: the round's share of pool queue (Q_r, "dominates" at 0.6 or more) and
  the request-read share of request time (S_q, "minor" at 0.3 or less). Two dissents were written down in advance.
- Overlap with Epic 6264 (modular monolith): this Epic adds only new, database-side reader methods; 6264's ports stay
  as they are, and 6400 lands first.
- Usage data: no new event. The existing "Portfolio tab opened" and "Team tab opened" events carry a closed band of the
  Features count (`featuresHeld`), never the raw number.

DISCUSS and DESIGN (ADR-257 to ADR-262)
- A round's frames run on a dedicated `ForecastLane` with as many threads as the forecast's core limit (ADR-257).
- Readers that leave histograms out for screens that need names and counts (ADR-258); the forecast gather uses them
  and the write-back resolves against a fresh, no-tracking read after the save (ADR-261).
- The licence count is one shared query; while it is loading or failed, only the create entry points refuse and say
  why. Edits behave as before (ADR-259).
- One owner-keyed Features-list endpoint per Portfolio and Team, with `FeatureDto`-named rows (ADR-260).
- The size note is decided on the server from the stored Features, with the threshold of 100 in one place (ADR-262).
- No existing response shape changes and the Lighthouse-Clients stay on `FeatureDto` and `GET /features`; no clients
  release.

Maintainer decisions at DISTILL (2026-10-24)
- Size bands for the usage-data property: `UpTo20`, `From21To50`, `From51To100`, `From101To399`, `From400`, `Unknown`.
- The Features list's looks: a refresh that fails after rows were shown takes the could-not-load look, so a previous
  answer never stays on screen; the all-hidden message names the toggle by its real label and counts the completed
  Features.
- The size note's failure is never silent ("Couldn't check this Portfolio's size."); the log line is a Warning, and it
  lands in Recent Problems, accepted.
- Every create entry point (Overview buttons, onboarding steps, Save on a new Team) follows the licence check.
- Where two stored Features share one reference id, the first in forecast order answers, counted once, with one
  warning per refresh, instead of a server error.
- If the baseline pointed at GC or CPU quota instead of the pool, DELIVER would stop and report before building the
  lane.

Maintainer decisions during DELIVER
- 2026-10-24, after the baseline: go on, on a mixed reading; the lane ships; the 920 list's 414 gets no Bug of its own
  because slice 04's single read removes the request. Order: 01, 03, 04, 02 (backend reads), 05, 02 (the rest), 06.
- 2026-10-25: an unreadable owner's Features list answers 404 like every other Portfolio and Team read (not 403).
- 2026-10-26: the create-Team wizard and the create-Portfolio wizard follow the licence check; a copied Team or
  Portfolio is not saved while the check loads or failed and says "Not saved: Couldn't check your license. Reload the
  page to try again." in its save indicator; at the free limit the wizards' Create is disabled with the premium
  tooltip.
- 2026-10-26: two older leaks stay as they are (see Open items). One before/after reading at the end, not per slice.
- Rigor: `standard` for slices 03, 04, 02 and 06; slice 05 a custom profile with a sonnet reviewer, because it changes
  how forecasts are saved and replaced.
- 2026-10-27, end of Epic: no follow-up Epic for now. "We see how it evolves and behaves for people."

## Work completed

| Slice | Story | What shipped |
| --- | --- | --- |
| 01 | #6404 (Task #6405) | Forecast rounds run on their own threads (`ForecastLane`), so pages keep answering during a round. The Portfolio and Team tab-opened usage events carry a closed `featuresHeld` band. A dev-only harness (`Lighthouse.EndToEndTests/perf`) builds the scaled set and classifies the stalls. |
| 03 | #6407 | The Features tab reads leave out forecast histograms. Two Features sharing a reference id warn once instead of erroring. |
| 04 | #6408 | One Features-list request per Portfolio and Team tab, with loading, could-not-load (with Retry), empty and all-hidden looks. The 920-Feature list loads. |
| 02 | #6406 | Overview, detail and Settings read names and counts without forecasts. The licence check is one shared query whose create actions wait while it loads and say why when it failed. |
| 05 | #6409 | Forecast rounds gather without histograms and write back from their own read. Rounds are 18 to 22 % shorter. |
| 06 | #6410 | A Portfolio over 100 Features shows an information note on its edit page, writes one Warning per successful update over 100 (listed in Recent Problems), and `GET api/latest/portfolios/{id}/size` answers its size. |

Quality gate: mutation testing per slice, 80 % kill rate on changed code. Final scores after one kill round each:
#6404 backend 82.81 % and frontend 96.63 %; #6406 backend 82.35 % and frontend 99.34 %; #6407 backend 90.00 %; #6408
backend 86.49 % and frontend 84.68 %; #6409 83.89 % over its files; #6410 frontend 92.31 % (backend in
`mutation/results-6410.md`). Per-file tables and configs are in `docs/feature/epic-6400-fast-with-many-features/mutation/`.

## Readings

Harness: SQLite, the 920 scale point (1,420 Features, 8 Teams), backend pinned to 4 cores. Opens are the median of 3
after 1 warm-up; windows run 300 s or until their round ends. The baseline (2026-10-24) was taken while another
worktree was mutation-testing, so its opens carry that load; the end reading (2026-10-27) ran on a quiet machine. Raw
tables are in `perf/out/runs/` (not in git).

| Reading | Baseline | End |
| --- | --- | --- |
| Overview, first paint, idle / during a round | 176 ms / 192 ms | 160 ms / 196 ms |
| Settings usable, idle / during a round | 248 ms / 360 ms | 248 ms / 247 ms |
| Portfolio Features tab (920), first row, idle / during a round | never (HTTP 414) | 5.0 s / 6.8 s |
| Team Features tab (397 Features), first row, idle / during a round | 15.4 s / 36.8 s (slice 01's quiet reading: 15.3 s / 20.1 s) | 4.2 s / 5.0 s |
| Computed forecast round, alone / with page traffic | 448 s / 543 s | 368 s / 425 s |
| Pool queue integral while a round runs with page traffic | 1,879 | 60 |
| GC pause share, same window | 10.4 % | 8.9 % |
| Heartbeat warnings | 0 | 0 |

Against the targets:
- **K1** (first paint and Settings usable under 1 s): met.
- **K2** (first Feature row under 2 s at 920 Features, idle and during a round): **not met.** The 920 list now loads
  and the Team tab is about 3.6 times faster, but both lists are still more than twice the target. In no document
  should this Epic be described as getting lists to about 2 s.
- **K3**: half met. K1 holds during a round and the pool queue fell by 97 %, but this machine never produced a
  heartbeat warning, in the baseline or at the end, so K3's own measure cannot be shown here. The reporter's next log
  is the production reading.
- **K4** (round length): computed rounds are 18 % shorter alone and 22 % with page traffic. The gain is all in save
  (166 s down to 89 s); simulate (about 277 s) is untouched. No skipped round started inside a window, so there is no
  skipped-round figure.
- Dissent B (S_q above 0.3) did not trigger: S_q was 0.243.

What the reading says is left: a 920-Feature list still takes about 5 s idle. Of about 4.5 s server time, only about
0.24 s is database command time, and the open moves 2.9 MB over 34 requests, so the time goes in shaping and sending
the list. During a round the server answers 3 to 4 times slower than idle with a small pool queue, which points at CPU
shared with the simulation on 4 cores.

## Lessons learned

- **Measure first and keep the harness dev-only.** The baseline found what no desk read had: the 920 Portfolio list
  never loaded at all (a 13.7 KB address against Kestrel's 8 KB limit), so the "list takes 50 s" report was the Team
  tab, and the Portfolio tab needed a different fix.
- **A reading taken on a loaded machine is not a baseline.** The first full run overlapped a Stryker run, so its
  during-round Team tab figure (36.8 s) compared unfairly; the slice 01 reading on a quiet machine became the fairer
  "before".
- **A lane that fixes the pool does not fix the page.** The lane cut pool queueing by 73 % at slice 01, yet the Team
  tab stayed 1.3 times slower during a round. What remained was payload and CPU, which is what slices 03 and 04
  addressed.
- **Failure must not look like "no data".** The size note, the licence check and the Features list each got an
  explicit failure look; a silent failure would read as "under 100", "no limit hit" or "no Features".
- **Per-feature mutation needs a kill round.** Every slice landed below 80 % on its first run (one at 64.86 %) and
  above it after one round of targeted tests.
- **One before/after reading at the end is honest when slices cannot be attributed.** Slices 03, 04, 02 and 05 took no
  readings of their own, so the end reading compares the whole Epic with the baseline and gives no per-slice shares.

## Issues encountered

- The baseline's first run was disturbed by foreign load; its windows were re-run once on a quiet machine, and two
  earlier end-reading attempts were stopped from outside (low memory, a stop signal), so the final run waited for the
  other work to finish.
- The usage-data size bands changed after DIVERGE's first draft (four to six bands) at the maintainer's request.

## Open items

- **A round stopped by the host** reads Completed in the queue but Cancelled in Refresh history; the two should agree.
- **No size warning when an update stores more than 100 Features and then fails later.** The Warning is written only
  after a successful update.
- **One Recent Problems entry per oversized update.** A large Portfolio updated often can push other problems out of
  the 200-entry list; the maintainer accepted this.
- **The size note does not refetch** when an update completes while the edit page is open; reload to see the new count.
- **K2 is unmet and there is no follow-up Epic for now.** Candidates if it comes back: shaping and sending the Features
  list (about 2.9 MB per Portfolio open), the round's simulate phase (about 277 s), and the other follow-ups listed by
  DIVERGE (async request paths, forecast less often, a client cache, paging).
- **Two older leaks stay as they are** (maintainer, 2026-10-26): `featuresInProgress` on the Team metrics endpoint
  names Portfolios the caller cannot read, and a withheld dependency keeps its verdict reason.
- **Skipped-round length and the production K3 reading** were not measured here; the reporter's next log after release
  is the K3 reading.
- **Release notes** are not part of this finalize; the maintainer writes them at release.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | `docs/portfolios/detail.md` and `docs/teams/detail.md` (the Features list's four looks), `docs/portfolios/edit.md` (the size note, the threshold, the log line), `docs/settings/taskmanager.md` (the Recent Problems line), `docs/licensing/licensing.md` (the shared licence check on create actions). |
| Usage-data page | Already lists `featuresHeld` and its six bands on both tab events (`docs/settings/usagedata.md`); no change needed. |
| Screenshots | N/A, because demo Portfolios hold fewer than 100 Features, so the note never shows, and a loaded list looks the same. The new looks are described in text. |
| `ARCHITECTURE.md` | Updated: the forecast lane, catalogue readers, the owner-keyed list endpoint, the shared licence query and the size note, plus an index row for ADR-257 to ADR-262. |
| Lighthouse-Clients | N/A, because no existing response shape changed and the new endpoints are additive; no changeset or version bump. |
| RBAC | No new permission. The list and size endpoints use the Portfolio and Team read permission, and an unreadable owner answers 404. |
| Website | N/A, because no new capability or screen. Whether to claim "hundreds of Features per Portfolio" is the maintainer's question, and K2 is unmet. |
| Release notes | Not drafted, by the maintainer's choice. |
| Migrations | None. |
| Demo data | No change. |

## Migrated out of the workspace

Nothing was moved. ADR-257 to ADR-262 and the DESIGN section of the brief already live under
`docs/product/architecture/`, and the journey under `docs/product/journeys/epic-6400-fast-with-many-features.yaml`.
The workspace `docs/feature/epic-6400-fast-with-many-features/` stays as delivery history.
