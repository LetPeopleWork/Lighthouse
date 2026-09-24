# Which behaviour settings people switch, and which way

Delivered 2026-09-24 in one slice. Internal telemetry, no ADO work item by the maintainer's decision. Not yet
pushed or released.

The maintainer asked for a usage data event every time somebody switches a behaviour setting, so that the
next "keep it, make it the default, or remove it" decision rests on use rather than on one instance's log,
as the Faster Updates decision in story 5913 had to. This is the eleventh event in the Opt-In Usage Data
vocabulary (Epic 5733). It uses the existing pipe unchanged: the browser notices, our backend checks consent
and the administrator's veto, and our backend forwards.

## What shipped

| Step | Commit | What it changed |
| --- | --- | --- |
| 01-01 | `a60f422ae` | The server reads `OptionalFeatureToggled` with which setting (`optionalFeature`, from usage data's own one-member list, `FeatureOrder`) and which way (`enabled`), and refuses every other shape: a missing half, a setting not on the list, a direction that is not a JSON boolean, and either field on any other event. |
| 01-02 | `b7d9cd6d5` | The collector receives `optional_feature` and `enabled`, left out entirely on every other event. The emit-seam field list gained exactly those two names. |
| 02-01 | `1cd746b66` | Settings → System reports the switch once the server has accepted it, never on the click and never on a refused write. |
| — | `9e4caea0c` | Refactor: one both-ways presence rule for every event part, judged on the whole event. |

No migration, no ADR, no `Program.cs` change, no DEVOPS change. The disclosure page
(`docs/settings/usagedata.md`) gained one event row and two field rows.

## Decisions worth keeping

- **The event cannot say who.** The pipe carries no instance and no person, only a per-browser pseudonym.
  It counts browsers that agreed, not installations. Adding an identity would reopen what the Epic closed on
  purpose, and what the consent copy promises.
- **The veto is never reported, in either direction** (confirmed by the maintainer). Switching *Never send
  usage data* on drops the very batch that would say so. Switching it off is dropped in the browser, whose
  consent answer still reads "stopped" until its hourly refresh. A one-sided count of lifts would mislead, so
  the setting is simply not on the list.
- **Usage data owns its own list of settings.** The product's stored key never travels. A setting reaches
  the census only when somebody adds it to `UsageDataOptionalFeature` deliberately, and the browser sends
  nothing for a key it has no name for.
- **Backend before frontend.** A browser posting a name the server cannot read gets its whole batch refused,
  and that loses every other event in the batch.

## Lessons

- **"Refused" scenarios need a control.** Before DELIVER every message naming the event was refused because
  the name was unknown, so a bare "is refused" scenario would have passed on both sides of the change. Each
  refusal scenario also sends the complete message and expects it accepted.
- **A shape check driven only by acceptance scenarios must be mutated with them.** No unit test calls
  `UsageDataEventShapes.Fits`. The Stryker.NET run included the usage-data acceptance scenarios on purpose;
  excluding them, as this repository's Stryker config usually does, would have reported survivors the suite
  actually kills.
- **Mutate after the refactor, not before.** A first run scored 58/66 on code the refactor then rewrote. It
  was discarded, and the run on frozen code is the verdict.

## Quality

28 of 28 backend scenarios and 7 of 7 frontend specs green; slice 04's every-event sweeps extended to the new
event. Full filtered backend suite 7273 / 0; `pnpm test` 5705; builds clean. Adversarial review approved with
0 findings. Mutation 100 % on changed lines on both stacks —
[mutation-results.md](optional-feature-toggled-usage-event/mutation-results.md). One known gap: no test posts
an integer `optionalFeature`; the guard that refuses one is covered by reading.

## Still open

- SonarCloud, verified by CI after push.
- The first real event appears only after the next release: a build nobody published sends nothing.
- The PostHog insight (`OptionalFeatureToggled` by `optional_feature` × `enabled`) is the maintainer's to
  build after release. `OUT-usagedata-setting-switches` has no baseline until then.
- No release-notes line: internal telemetry, no ADO work item.

Workspace: `docs/feature/optional-feature-toggled-usage-event/`.
