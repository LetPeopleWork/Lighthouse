# New Portfolios start with default Feature sizing off

Delivered 2026-11-25. No ADO work item (the maintainer chose to do it without one). A follow-up to Story 6504, whose
switch had not been released yet. Run in AFK mode on the standard rigor profile.
Workspace: `docs/feature/new-portfolio-default-sizing-off/`.

## What was delivered

Default Feature sizing confused people, so a new Portfolio now starts with **Infer a size for Features without child
Work Items** switched **off**: a Feature without child Work Items counts as 0 until its owner switches sizing on.

| Slice | What it delivers |
|---|---|
| S1 | A new Portfolio, from the wizard, the API, the Clients or demo data, starts off. When its connection names a Portfolio Size Estimate Field default at create time, it starts **on**, so its Features read the size the field holds (maintainer decision B, taken during DELIVER after the full test run showed the conflict). An explicit value on an API create wins; a clone keeps its source's value; existing Portfolios keep what they store, with no migration. The Portfolio decides once, in `Portfolio.StartOnConnection`, which the create controller calls; the wizard no longer sends the switch. |

Gates:
- Whole filtered backend suite: 0 failed of 14,891 at the GREEN commit. Oracle 79/79 backend, wizard 29/29.
- Whole-candidate review (Haiku) accepted after one revision pass.
- Mutation: backend 82.35 %, frontend N/A (`docs/feature/new-portfolio-default-sizing-off/mutation/results.md`).
- EXAMINE: two Haiku runs found only observation gaps and nwdes refused to close; waived by the maintainer as in Story 6504 (`feature-delta.md`).
- CI run 45699308312 green (release gates waiting), Sonar gates OK on both projects with 0 new violations. The first push broke CI's backend build: the rebase onto Story 6468 merged cleanly but a new test still used `ITerminologyService`'s old namespace, and the build had been skipped. Fixed in `d69b2d735`.

## Commits

```
d69b2d735 fix(portfolio): import the terminology service from its new module in the starting-value test
c12ed6847 test(portfolio): mutation gate for new-Portfolio sizing default — backend 82.35 %
fb8b0d201 docs(portfolio): record the delivery and the EXAMINE waiver for new-Portfolio sizing default
4ec18cf73 docs(portfolio): record the accepted whole-candidate review for new-Portfolio sizing default
5fe1badff test(portfolio): a demo Portfolio starts with default Feature sizing off
02beb63e3 refactor(portfolio): drop the redundant false initializer on the sizing switch
f88918c2b feat(portfolio): a new Portfolio on a connection with a size field default starts sizing on
22300d464 docs(portfolio): record the approved oracle re-review for a connection size field default
ecfcdbdcc test(portfolio): a new Portfolio on a connection with a size field default starts on — oracle
51a81c203 docs(portfolio): a new Portfolio starts on when its connection names a size field default
efb3f83c8 refactor(portfolio): say plainly which Portfolios start with default Feature sizing off
1ff2725d6 feat(portfolio): a new Portfolio starts with default Feature sizing off
b19c80d2d docs(portfolio): record the approved oracle review for new-Portfolio sizing default
413b945d4 test(portfolio): drive the new-Portfolio starting value through the create controller
9e57f4fcc test(portfolio): a new Portfolio starts with default Feature sizing off — oracle
0362c3146 docs(portfolio): fix the examiner's expectations for new Portfolios starting with sizing off
c088a7b8d docs(portfolio): new Portfolios start with default Feature sizing off — DISCUSS, DESIGN, DEVOPS
```

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done.** `docs/portfolios/edit.md`: the section is now "Switching default Feature sizing on and off" and says a new Portfolio starts off, starts on when its connection names a Size Estimate Field for Portfolios, a clone keeps its source's setting, and an existing Portfolio keeps its own; the Default Feature Size intro says it applies only while sizing is on. `docs/features/features.md`: the link follows the renamed heading. `docs/settings/usagedata.md`: unchanged, its row for event 29 still holds. |
| Screenshots | **Deferred by the maintainer to the pre-release pass.** Demo Portfolios now start off, so a demo Feature without child Work Items would show "0 of 0" in the Features page, Portfolio detail and Delivery shots; nobody measured whether such a Feature appears in them, the maintainer will regenerate them before the next release. |
| Demo data | **Done.** Demo Portfolios start off; `DemoDataFactoryTest.A_demo_Portfolio_starts_with_default_Feature_sizing_off` guards it. The demo connection names no Size Estimate Field default, so no demo Portfolio starts on. |
| Website assets | **N/A**, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | **N/A, no release.** `createPortfolio` sends an untyped payload that never names the switch, so a Client create gets the backend's rule. No version bump. |
| Context map, `ARCHITECTURE.md` | **Done.** The ADR-270 glossary row "Default Feature sizing" states the new starting value and the connection exception; `ARCHITECTURE.md` §4 (WorkItems / Sync row) no longer says "on by default". No new context, edge or domain event. |
| Usage-data event | **N/A for a new event**, because the feature adds no new user action. `DefaultFeatureSizingSwitchedOff = 29` is unchanged. |
| Mutation | **Done.** Backend 82.35 % (34 mutants, every mutant on a changed line killed); frontend N/A, its only change deletes one property. |
| Release notes | N/A; written at /release. |

## Left open, for the maintainer

- **Own size field on create.** A create that names the Portfolio's own Size Estimate Field, rather than following the connection default, starts off. Open product question.
- **A "switched on" usage event.** `DefaultFeatureSizingSwitchedOn = 30` would show whether people opt back in on new Portfolios. Not built.
- **Screenshots** of demo Portfolios may show "0 of 0" rows next time they are regenerated.
- **Non-blocking review note:** the Slice10 and Slice11 set-ups switch every Portfolio in their test database on, not only the ones they seed.
- **Brief wording:** one scope-in line of the product brief still says plainly that wizard, API and Clients start off, without the connection exception; the decisions and promises carry it.
- **Found in passing:** `LicenseTab.test.tsx` failed once under a loaded local `pnpm test` (passes alone 10/10); CI warned that `TestHostIsReleasedAfterDisposeTest` took 35.6 s against a 34 s budget (not touched here).
