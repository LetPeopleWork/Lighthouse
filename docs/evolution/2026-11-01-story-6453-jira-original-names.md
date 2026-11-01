# Jira works against the original status and work item type names — Story 6453

Delivered 2026-11-01, ADO User Story #6453 ("Jira: work against the original (untranslated) status and work item
type names"). Pushed to `main` in `ad145be1d..d5527352f` (28 commits). CI run 41293660335 is green: every verify
job passed and the Sonar gate is OK with no new issues. Only the Standalone release gates wait, as on every main
run. The Story is Closed.

A customer whose Jira token belonged to an account set to German found that Load from board listed German state
names, sometimes twice, and that a refresh stored German states against a Team configured in English. Jira Cloud
answers every name in the token account's language. Only `/rest/api/latest/status` and `/issuetype` carry the
`untranslatedName`, and the search API does not. Lighthouse therefore had no way to tell "In Arbeit" from
"In Progress".

## What shipped

- **Load from board and every refresh use the original names.** On a Cloud instance, and on one whose deployment
  cannot be identified, the connector reads the status list and the issue type list and resolves every status and
  type id through them, using `untranslatedName` and falling back to `name`. The status category comes from its key
  (`new` / `indeterminate` / `done`), never from its translated display name. Names are de-duplicated ignoring case.
  An identified Data Center behaves exactly as before.
- **A list Lighthouse cannot trust stops the refresh, visibly and before anything is stored.** This covers a refused
  answer, a body that is not a JSON list, an empty list, and an id that is still missing after one more read of
  that list. The Task Manager carries the approved copy. A board load keeps its degrade-with-warning behaviour.
- **Each list is read once per connection per connector**, so a refresh and a board load share the read, and each
  list gets one re-read when it lacks an id.
- **One full refresh after the upgrade.** A Jira-only reading-version marker in the fetch fingerprint makes every
  Jira Team and Portfolio take one full refresh, which repairs items stored with translated states. Other trackers
  keep a byte-identical fingerprint. The marker is its own commit (`9444ea6cf`) so it can be reverted alone.
- **Suggestions list each name once, whatever its letter case**, keeping the spelling of the oldest Team or
  Portfolio.
- Docs: `docs/concepts/worktrackingsystems/jira.md`, a new section "Status and Work Item Type Names".

## Key decisions

- Work against the original names everywhere; never fall back to a translated name, which would reproduce the bug
  silently. Changelog names and JQL are left untouched, after a live probe showed both already use original names.
- No migration. The fingerprint marker repairs Teams configured in English. Teams configured in German still need
  Load from board again, and the release note says so.
- Data Center keeps its names as reported. No Data Center probe was run, so the docs call this untested.
- Changed in DELIVER, with the delta amended to match:
  - Each list has its own re-read, instead of one shared re-read for both lists. A shared re-read let a status miss
    on an early page fail a refresh in which Jira had just gained a new type.
  - The issue factory takes the names as a required argument, so no caller can forget them.
- The final review found the memo kept per connector instance, against the design's "per connection". The memo is
  now keyed by connection id in a `ConcurrentDictionary`, like the link-type memo beside it.
- Suggestions read Teams and Portfolios in Id order, so "the first spelling met" no longer depends on the
  database's unordered answer.

## Numbers

| Gate | Result |
| --- | --- |
| Roadmap | 11 steps in 3 slices; all have complete DES traces |
| Tests | 61 scenarios authored in DISTILL and enabled during DELIVER, plus review and kill-round tests; filtered suite 11,893 pass |
| Mutation (backend) | 89.03 % on the lines the story wrote (138/155); 71.44 % on whole files, a figure dominated by the 4,000-line connector |
| Build / Sonar | 0 warnings; gate OK, 0 new issues, 0 hotspots |

## Lessons

- A wait loop of the form `until ! pgrep -f "dotnet test"` matches its own command line and never ends. One step
  hung for 25 minutes with no test running. Run the command and wait on it instead.
- Stryker.NET mutates whole files. Scoring the mutants on the lines a change wrote
  (`tools/mutation/score_the_slice.py`) is what made a gate on a 4,000-line connector meaningful. The full run took
  9 minutes, not the hour expected.
- Haiku reviewers with no shell cannot diff against a parent commit, so they reason structurally about pins. Hand
  them the diff as a file, and check their git-dependent claims yourself.

## Found in passing

- `JiraOriginalNamesLiveTest` takes about 178 s in CI against the 34 s test-time budget (a warning, not a failure).
  It needs its own budget entry or fewer live comparisons.
- `SecretStateClassifierTests` also went over budget in the same run. It is not related to this story.
- Two repo-scanning tests fail locally on a root-owned `examples/postgres/data/postgres` left by a container. They
  pass in CI.
- Suggestions still list whitespace variants ("Story " and "Story") separately. The frontend calls
  `/suggestions/tags`, which has no backend route.
- Board-load list reads pass `CancellationToken.None`. `IBoardInformationProvider` has no token to pass through.

## Artifacts

- Feature workspace (delivery history): `docs/feature/story-6453-jira-original-names/`. It holds the feature delta,
  the slice briefs, the DELIVER roadmap and DES audit log, and the mutation config and results.
- No architecture documents, ADRs, scenarios or UX journeys were produced for migration.
