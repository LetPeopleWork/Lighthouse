# Story 6504 — Choose whether a Feature without children gets an inferred size

Delivered 2026-11-25. ADO #6504 (stand-alone Story, no parent Epic). It was run in AFK mode on the standard rigor profile.
Workspace: `docs/feature/story-6504-childless-feature-size/`.

## What was delivered

A Portfolio switch, **Infer a size for Features without child Work Items**, sits in the Portfolio's advanced settings, in the Default Feature Size group. It is on by default for existing and new Portfolios.

| Slice | What it delivers |
|---|---|
| S1 | **The switch.** When it is off, a Feature without child Work Items counts as 0: no Team carries work for it, and its own forecast and a containing Delivery's count it as 0. The dependent settings are greyed out and keep their values. A new Portfolio starts with the switch on. The usage event `DefaultFeatureSizingSwitchedOff` (29) is sent once on an accepted on-to-off save. |
| S2 | When the switch is off, the State Override no longer replaces real child counts. |
| S3 | When the switch is off, a childless Feature says *No child Work Items yet* in the warning spot. This shows in the Feature list, on the Delivery timeline, in the Delivery's warnings column and in its export. The flag is derived once on the Feature (`HasNoChildWorkItemsYet`) and sent on both Feature payloads. |
| EXAMINE follow-up | The childless row shows **0 of 0**, as the approved sketch does, instead of "Could not determine work". |

Gates:
- Full filtered backend suite: 14,801 passed. Full frontend suite: 10,351 passed. Both builds have 0 warnings.
- Whole-candidate review accepted (twice, the second time after the EXAMINE follow-up).
- Mutation: backend 92.82 %, frontend 83.87 % (`mutation/results.md`).
- EXAMINE: three Haiku runs, none found a contradiction. nwdes could not close it. The maintainer chose to record the outcome in `feature-delta.md` instead.

## Commits

```
076358ea4 feat(portfolio): switch to turn default Feature sizing off
fb1db6cc8 refactor(portfolio): one owner for reading an absent default-sizing switch as on
cc2a5ba68 feat(portfolio): override states respect the default sizing switch
800475257 revert(portfolio): keep the disabled-list drag fix out of story 6504
a76b5bd9c feat(portfolio): say when a Feature has no child Work Items yet
154834fad refactor(portfolio): one owner for an open Feature without child Work Items
978c5ab9a feat(portfolio): a Feature without child Work Items shows 0 of 0
```

The oracle, support and mutation tests are in the `test(portfolio)` commits between them. The nwdes records are in the `docs(portfolio)` commits.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done.** `docs/portfolios/edit.md` gains a "Switching default Feature sizing off" section. `docs/features/features.md` gains a Warnings bullet. `docs/portfolios/detail.md` adds the note to the Warnings column. `docs/teams/detail.md`: N/A, because with the switch off a childless Feature carries no Team work, so it never appears on a Team page. `docs/metrics/portfolio-features.md`: N/A, because its rule (Features using the default size are left out) is unchanged. |
| Screenshots | **N/A**, because no page under `docs/` shows the Default Feature Size group or a childless row in a screenshot. The switch is on by default, so no existing shot changes. |
| Demo data | **N/A**, because the demo Portfolios keep default sizing on, the default. Switching one off would change the demo forecasts the other docs and screenshots rely on. `DemoDataFactoryTest` is unaffected and green. |
| Website assets | **N/A**, because no asset under `docs/assets/` changed. The one-line marketing mention DISCUSS suggested is left for the website owner. It needs no asset. |
| Clients (CLI, MCP) | **N/A, no release.** The client's `updatePortfolio` passes the settings payload through untyped, so `infersDefaultFeatureSize` reaches the server. An omitted field keeps the stored value. The Feature payloads gain `hasNoChildWorkItemsYet`, which clients pass through. |
| Context map, `ARCHITECTURE.md` | **Done in S1.** The ADR-270 glossary row "Default Feature sizing" is in. `ARCHITECTURE.md` §4 (WorkItems / Sync row) describes the switch. There is no new context, edge or domain event, which matches what shipped. |
| Usage-data event | **Done in S1.** `DefaultFeatureSizingSwitchedOff = 29` is emitted by `useDefaultFeatureSizingSavedReporter` and listed in `docs/settings/usagedata.md`. |
| Mutation | **Done.** Backend 92.82 % (181 mutants), frontend 83.87 % (62). No survivor is on a changed member. See `docs/feature/story-6504-childless-feature-size/mutation/results.md`. |
| Release notes | N/A; written at /release. |

## Left open, for the maintainer

- **Note placement.** The sketch drew an info icon beside "0 of 0" carrying the note. The build puts the note on the existing warnings icon, in the spot the confirmed decision named ("where today's warning icon is").
- **Team pages show no note.** A childless Feature with the switch off is not listed on any Team page. This follows from "no Team carries work for it".
- **Disabled-list drag follow-up.** `ItemListManager` still lets a chip be dragged while the list is disabled. The fix was reverted out of this Story (`800475257`) as its own follow-up.
- **Non-blocking review notes kept:**
  - test placement: the render-free cases and the decode cases sit in `PortfolioFeatureList.test.tsx`, and the port scenario sits under `Tests/API/DTO`;
  - `CreatePortfolioWizard.test.tsx` is not declared as an S1 support;
  - a test title still says "four settings";
  - `FetchShapingPropertyGuardTest` uses a string where it could use `nameof`;
  - no database round trip of an explicit false;
  - no create-path assertion for the usage event.
