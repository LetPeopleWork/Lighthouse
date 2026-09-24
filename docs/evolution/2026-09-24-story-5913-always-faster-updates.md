# Faster Updates without a switch — Story 5913

Delivered 2026-09-24 in one slice. Free. Not yet pushed or released.

Epic 5687 made refreshes download only what moved on Jira Cloud, Jira Data Center and Azure DevOps, and
shipped it behind a *Faster Updates* switch with a stated end: once it held on real instances, the switch
would go, because a gate nobody removes becomes a permanent second code path. This story is that removal.
An instance that turned the switch off, or upgraded while it was still opt-in and never turned it on, kept
re-downloading every Work Item on every refresh.

## What shipped

| Step | Commit | What it changed |
| --- | --- | --- |
| 01-01 | `1c7e88b71` | The refresh stopped asking. `SyncModeResolver` lost its opt-in parameter and leading `Full` branch; `WorkItemService` lost the flag read and the repository it was the only reader of; the team, portfolio and parent-Feature paths always scan. |
| 01-02 | `1bd0e78c6` | The switch went. The seeder retires the `DeltaSync` row at start-up, whichever way it was set, and never seeds it again. |
| 01-03 | `a226c4e96` | `docs/settings/configuration.md` and `systeminfo.md` describe how every update works, not a toggle. |
| — | `7fe595517` | Comments that still explained the opt-in, rewritten to stand on their own. |

No migration, no API contract change, no frontend production change. Settings → System has one row fewer.

## Decisions worth keeping

- **A previous "off" is not honoured.** The row is deleted, so the choice goes with it. The release notes
  say so plainly; there is no start-up warning.
- **No kill switch of any kind.** The safety net is the one that already existed: a failed or refused
  identity scan downloads the whole query and logs a `WARNING`. A misbehaving connector is fixed with a release.
- **The epic's evidence gate was accepted on one long-running instance.** The dev instance logged 118 delta
  refreshes over six weeks, none unsuccessful, every failed scan falling back to a full download. The log
  records failures, not drift, so this is a maintainer acceptance rather than a measurement.
- **The seeder removes the row, not a migration.** `DeltaSyncKey` stays as a constant only because the
  retired-keys list names it.
- **Downgrading brings the switch back on.** An older seeder re-adds a missing row with its fresh-install
  default, and the operator's earlier "off" is gone.

## Lessons

- **Order was the whole design.** Removing the row first would have been read as "switched off" by the code
  that still asked, and turned every instance back to full refreshes. The read went first, proven by
  scenarios 4–6; the row second, proven by 1–3.
- **A shipped setting had quietly become a test fixture.** Three behaviour-settings and usage-data fixtures
  used Faster Updates as "the shipped row that is not premium". With it gone none ships, so those fixtures
  now seed their own. One of them is stronger for it: the fixture row starts off, so "stored on" can now fail.
- **Removal stories mutate cheaply.** The changed lines in `WorkItemService` are deletions with nothing left
  to mutate; the behaviour is pinned at the port by scenarios that fail the moment any scan stops running.

## Quality

7 of 7 story scenarios green; full filtered backend suite 7243 / 0; `pnpm test` 5698. Adversarial review
approved with 0 findings. Mutation 100 % on changed lines — [mutation-results.md](story-5913-always-faster-updates/mutation-results.md).

## Still open

- Live check on a real instance (maintainer).
- SonarCloud, verified by CI after push.
- `docs/assets/settings/optionalfeatures.png` regeneration, deferred to the `/release` update-docs pass.
- Posting the drafted release-notes line to the ADO item.
- Both KPIs (`OUT-5913-delta-without-asking`, `OUT-5913-no-resurrection`) are measured after release on real logs.

Workspace: `docs/feature/story-5913-always-faster-updates/`.
