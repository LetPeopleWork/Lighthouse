# The over-time history fill always runs — Story 6084

Delivered 2026-10-15 in one slice of two steps. Free, no longer a Preview, **no setting**. ADO User Story
#6084, the last of the three follow-ups planned with #6053. Story #6053 shipped the fill opt-in, and #6083
switched it on by default in v26.10.3.6. This story removes the switch.

Percentiles Over Time and PBC Over Time now fill in the days Lighthouse missed on every instance. That
includes instances whose System Admin had switched the fill off after #6083.

## What shipped

| Step | What it changed |
| --- | --- |
| 01-01: the fill stops reading the setting | The switch port and its implementation are deleted, along with their registration. The gap reconciler asks the filler whenever days are missing. The filler starts every waiting pass, and the "dropped a waiting pass, switched off" path and its log line are gone. Bounds, the per-pass budget, the memo and the maintenance gate are unchanged. |
| 01-02: the setting and its leftovers go | `OverTimeHistoryFill` joins the optional-feature seeder's deprecated keys, so every start-up removes the row, and the setting's by-name endpoint answers 404. `AppSettingSeeder` removes #6083's one-time "switched on" record by key, beside its unchanged by-Id list, because the record's Id was assigned at runtime. #6083's restore rule is deleted, so a restored backup fills like any other instance. |

Refactor (3 commits): the two acceptance harnesses share one description of the setting as earlier releases
stored it. Names and comments that still said "switch" now name the retired key. The E2E helper lost its
now-single-use wrapper. Docs: the configuration page keeps a short "no setting any more" section, the
predictability page drops the Preview, ADR-207 has a 2026-10-15 amendment, and a vNext release-note line was
added.

No migration, no API contract change apart from the by-name 404 on a removed setting, and no UI change apart
from the row disappearing from Settings → System.

## Decisions (maintainer, 2026-10-15)

- **The DIVERGE, DESIGN and DEVOPS waves were skipped.** The mechanism decisions DESIGN would have taken were
  locked in DISCUSS.
- **Deliberate offs become on, with no opt-out.** The release notes say so and advise taking a backup before
  upgrading.
- **The row is retired in this release, through the seeder.** During a rolling deploy, an old replica that
  restarts can re-add the row. New code never reads it, and the next new-version start-up removes it again.
  This is an accepted risk with no test.
- **The full local E2E run was skipped.** CI runs E2E on SQLite and Postgres after the push, and the touched
  specs passed locally (10/10).
- **Mutation testing was skipped.** The story is almost entirely deletion. The new logic is a key list and one
  where-clause, pinned by acceptance tests that were run red before it existed.

## Quality gates

- **Backend.** The filtered suite (connector categories excluded) gave 10044 passed and 0 failed, rebased onto
  origin/main. The Integration category gave 10 of 10 after the `Program.cs` change. `dotnet build` had 0
  errors; its only warnings are the pre-existing NU1903.
- **E2E.** The touched specs passed locally (10/10). A Predictability → Database Management sequence passed
  9/9. The full suite runs in CI on SQLite and Postgres.
- **Reviews.**
  - DISTILL: Sentinel approved. Eclipse approved after one revision, which added the worked examples, the full
    E2E scope, the restore of a backup holding the fill on, and the screenshot.
  - Roadmap: conditionally approved.
  - Adversarial (strong model): 0 blockers. The one high finding is fixed and proven. With the fill always
    on, a pass left running by an earlier E2E test made the app refuse the Database Management restore. The
    page object now waits that refusal out. A forced-refusal probe failed against the old page object and
    passed against the new one. The six lows are fixed: docs wording, two test tightenings, and comments that
    cited internal references.
- **DES integrity.** Both steps have complete traces.

## Found in passing

- **The Database Management page can get stuck.** Opened while a fill pass is in flight, the page disables
  the whole restore form, and nothing polls the status again. It stays unusable until a reload, even after
  the fill has finished. This predates this story, but always-on makes it likelier. Worth its own Bug.

## Owed at hold

- Regenerate `docs/assets/settings/configuration.png` without the row. Run `rm` on the old PNG first, because
  the regenerator keeps a file whose diff is under 0.5 %.
- Lighthouse-Clients copy, and a patch release, in `packages/client/src/index.ts`, `packages/cli/src/index.ts`,
  `packages/mcp-core/src/index.ts` (four tool descriptions) and
  `skills/lighthouse/references/lighthouse-mechanics.md`. They still describe a Preview that a System Admin
  switches on.
- The ADO `Release Notes` tag on #6084.

Workspace: `docs/feature/story-6084-over-time-history-fill-always-on/`.
