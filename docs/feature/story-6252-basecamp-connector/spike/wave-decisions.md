# SPIKE Decisions -- story-6252-basecamp-connector

## Assumption Tested
- Can Lighthouse get Work Item state-transition history (including blocked) from Basecamp for items that already exist, or would it have to record transitions itself from the moment a connection is made?

## Probe Verdict
- WORKS: per-card events return every Card Table column move and every on-hold/release as an `adopted` event with the old and new parent and a UTC timestamp, including moves made before the API app existed. Column types give To Do / Doing / Done directly. Auth is OAuth only (Launchpad), with non-rotating refresh tokens. Ease for a connector: 3, leaning 4.

## Promotion Decision
- DISCARD (no walking skeleton): the spike was scoped as probe-only, to get a gut feeling for the work. The findings are the deliverable. The free account, Launchpad app and nightly demo updater stay in place as the future integration-test environment.

## Design Implications
- Work Item = Card Table card; Feature = the card's project; portfolio = all projects in the account; a team is chosen by Card Table / project name, as with Linear.
- State mapping can be pre-filled from column `type` (`Kanban::Triage`, `Kanban::Column`, `Kanban::DoneColumn`, `Kanban::NotNowColumn`).
- Blocked = time in a column's on-hold section, read from history; no blocked snapshotting needed.
- Auth reuses the OAuth bearer strategy with a per-connection Launchpad client id and secret; refresh runs routinely, as access tokens last 14 days.
- To-dos and hill charts stay out of a first version; "Feature = to-do list" is a later, throughput-only option.
- No writeback: Basecamp has no estimate or custom fields.

## Constraints Discovered
- No personal access tokens on the Free plan; every customer registers a Launchpad app.
- History is per card: a full refresh costs one call per card, so rate limits shape the refresh design. The account event feed may allow incremental refreshes (unverified).
- No parent links between items; the hierarchy comes from the container.
- To-dos have no started date, and can sit outside any to-do list.
- The Free plan allows one project, so multi-project behaviour needs a paid trial to verify.
