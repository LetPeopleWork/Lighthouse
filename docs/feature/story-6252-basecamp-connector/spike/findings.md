# Spike findings: Basecamp connector (ADO #6252)

Concluded 2026-10-12. Probe-only: the findings are the deliverable, nothing was built into Lighthouse.

## The question

We were asked whether Lighthouse could support Basecamp. Before estimating a connector we wanted to
know what Basecamp makes possible: above all, whether it keeps the history of state transitions, or
whether Lighthouse would have to store them itself, as it does for blocked items in some systems.

Background research is in `research.md` (API, auth, plan, gap analysis) and `hierarchy-research.md`
(how Basecamp and Shape Up model "a big thing made of small things"). The probe ran against a Free
Basecamp account (one project with a Card Table and a To-do set), fed by hand and by the nightly demo
updater `Scripts/DemoEnv/BasecampSystemUpdater.py`.

## Verdict per question

| Question | Verdict | Evidence |
|---|---|---|
| State-transition history | **Available, retroactively.** `GET /buckets/<project>/recordings/<card>/events.json` returns every column move as an `adopted` event with `parent_id_was`, `new_parent_id` and a timestamp. A move made before the API app or token existed was returned. | Probe, card "Teset" |
| Blocked | **Available, retroactively.** A column's on-hold section has its own id. Holding and releasing a card are `adopted` events into that id and back. Blocked spells can be rebuilt from history; Lighthouse does not need to snapshot them. | Probe, hold by hand 11:17:59Z, release by the updater 11:21:18Z |
| State categories | **Given by Basecamp.** Each column has a `type`: `Kanban::Triage` (To Do), `Kanban::Column` (Doing, user-defined), `Kanban::DoneColumn` (Done), `Kanban::NotNowColumn`. Moving a card into Done also sets `completed: true` on the card. | Probe |
| Auth | **OAuth only.** The Free plan offers no personal access token, despite the CLI docs suggesting one. Launchpad OAuth: each customer registers an app; access tokens last 14 days; refresh tokens do not rotate, so one stored refresh token keeps working. Fits Lighthouse's existing OAuth bearer strategy with a per-connection client id and secret. | Probe |
| Querying | **No query language, and none needed.** Like Linear, a team is chosen by name (a Card Table / project) rather than by a query. | `research.md` c; Linear connector |
| Hierarchy | **Card = Work Item, the card's project = Feature, all projects = the portfolio.** This is the Linear pattern. A team working in one project gets team metrics only. To-dos and hill charts stay out at first. | `hierarchy-research.md` |
| Estimates, custom fields | **None.** No size field, so no writeback; accepted, as for other connectors. | `research.md` e |
| API on the Free plan | **Works**, reads and writes. | Probe |
| Timestamps | **UTC** (`Z` suffix). | Probe |

## To-dos

To-dos only know open or completed: there is no started date, so they give throughput and done dates
but no cycle time. To-dos can also sit directly in the to-do set with no list ("loose" to-dos), so a
to-do list is not a dependable parent. Shape Up, 37signals' own method, models a bet as a project and
its scopes as to-do lists, but rejects backlogs and discovers tasks as the work goes. Forecasting a
to-do based Feature from what is left would therefore mislead. A "Feature = to-do list" mode can be
added later for throughput only.

## Ease verdict for a Basecamp connector: **3, leaning 4**

The plumbing is plain REST with OAuth we already support, the state categories come from Basecamp, and
history including blocked is retroactive. Basecamp gives more history than ServiceNow did. What keeps
it from a clear 4:

- History is read one card at a time. A full refresh of a large board costs one call per card, which
  meets Basecamp's rate limits. The account event feed (`GET /events.json`, `card.moved` with both
  columns) would allow incremental refreshes but is unverified.
- No parent links between items: the hierarchy comes from the container (project), not from the item.
- Every customer must register a Launchpad app before connecting.

## Unverified

- Account event feed: whether it reports moves with both columns, how far back it reaches, whether it
  is usable as an incremental sync.
- Account-wide card listing (`/projects/recordings.json?type=Kanban::Card`) as a single sweep.
- Rate limits and conditional requests (`ETag` / `If-None-Match`).
- To-do completion time reliability and how reopening is recorded.
- That older events stay unchanged over days (low risk: they are stored records).
- Anything across several projects (card moves between projects, project archive dates, several card
  tables per project). The Free plan allows one project; this needs the 30-day paid trial.

## Environment left in place

- The Free Basecamp account (LetPeopleWork GmbH) with "My Project", plus a Launchpad app.
- The nightly `updatedemoenv.yml` step running `BasecampSystemUpdater.py`, with the secrets
  `BASECAMPCLIENTID`, `BASECAMPCLIENTSECRET`, `BASECAMPREFRESHTOKEN`. These are tied to the
  maintainer's own Basecamp login; switch to a dedicated demo user if the connector is built.
- This is the integration-test environment for a future connector.
