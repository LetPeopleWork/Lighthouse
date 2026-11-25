# New Portfolios start with default Feature sizing off

No ADO work item: the maintainer chose to do this without one ("no story, let's just do it", 2026-11-25).
Product authority: `handoff.json`, rendered in `brief.md`. Builds on Story 6504
(`docs/feature/story-6504-childless-feature-size/`), which is on `main` but not yet released (last tag `v26.11.15.4`).

## DISCUSS — 2026-11-25

### Wave route

- DISCOVER and DIVERGE skipped by the maintainer in as many words ("directly to discuss").
- DISCUSS ran interactively; the maintainer answered every Product decision in the host conversation.
- No Epic, so `/continuous-improvement before <epic-id>` does not apply.

### Maintainer decisions

| # | Question | Decision |
|---|---|---|
| 1 | Default for new Portfolios | Off. Default Feature sizing confuses people. |
| 2 | Existing Portfolios | Keep their stored value (on). No data migration and no "already using it" heuristic; considered and rejected because it would move forecasts on upgrade. |
| 3 | Clone | Keeps the source Portfolio's value. |
| 4 | API / Lighthouse-Clients create without the field | Off, the same as the wizard. |
| 5 | API / Lighthouse-Clients create that names the field | Keeps the named value. |
| 6 | Demo data | Demo Portfolios start off; only a demo Portfolio that must show Default Feature Size behaviour (e.g. a docs screenshot) has the switch on explicitly. Some screenshots and E2E that relied on inferred sizes change. |
| 7 | Quint scenario exploration | Refused by the maintainer: decision 4 already settles the only Quint-relevant value. |

Open, owner DEVOPS: usage event `DefaultFeatureSizingSwitchedOff = 29` counts on → off saves. With off as the new
default it no longer measures opting out for new Portfolios. Keep, change, or add a "switched on" event.

### Slices

| Id | What it delivers | Size |
|---|---|---|
| S1 | A new Portfolio (wizard, API, Clients, demo data) starts with default Feature sizing off; existing Portfolios and clones keep their value; an explicit API value is kept; the switch can still be turned on. | S |

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | N/A, because no new action, page or data. The switch is an existing Portfolio setting written through the existing create/update routes, already gated by `RbacGuard`; only its starting value changes. |
| Lighthouse-Clients CLI / MCP | No client change. `createPortfolio` passes an untyped payload (`lighthouse-clients/packages/client/src/index.ts:2539`) and never names the switch, so a Client create picks up the new backend default (off), as decision 4 wants. No version bump. |
| Website marketing surface | N/A, because this changes a default of a setting, not a capability. Any website line written for 6504 should not say "on by default"; check at finalize. |
| Context map | No new context, term, edge or domain event. Contexts: Portfolio configuration (the switch's starting value), Feature sizing and Forecasting unchanged. Glossary term "default Feature sizing" from 6504 stays; its description of the default changes if ADR-270 states one (check at DESIGN). UI surface: the create wizard and the Portfolio's settings tab, both unchanged in layout. |

## DESIGN — 2026-11-25 (AFK, propose mode)

Authority: `architecture.md` (rendered from `handoff.json`).

- **Owner:** the `Portfolio` entity. `Portfolio.InfersDefaultFeatureSize` starts `false` instead of `true`; every path that builds a new Portfolio (API create, Clients, demo data, tests) inherits it.
- **No migration:** the EF mapping stays `HasDefaultValue(true).HasSentinel(true)`. A false CLR value differs from the sentinel, so EF writes it; an explicit true equals the sentinel, EF omits the column and the column default true applies. Existing rows and restored backups keep their stored value.
- **Create route unchanged:** `PortfolioExtensions` keeps the entity value when the DTO's `bool?` is absent and takes a named value.
- **Frontend:** only the create wizard literal changes (true → false). Clone spreads the source settings (decision 3 holds); the read fallback `?? true` stays, because a stored value is always present.
- **Demo data:** demo Portfolios inherit off. A demo Portfolio is set on in `DemoDataFactory` only where a screenshot shows Default Feature Size behaviour.
- **Docs:** ADR-270 glossary row "Default Feature sizing" and `docs/portfolios/edit.md` change their "on by default" wording; `ARCHITECTURE.md` names no default.
- **Tests:** oracle `DefaultFeatureSizingTest` (its "untouched" cases flip to off); supports `PortfolioSettingDtoCompatibilityTest` (an older-client payload without the field starts off; a named value is kept) and a new SQLite-backed starting-value table, because the in-memory EF provider ignores column defaults and sentinels.
- **Formal check:** none beyond the 6504 Quint model. The only claim is a finite starting-value table; the SQLite table test is its falsifier.
- **Loading / failure looks:** no view is new or changed.
- **Context map:** no new context, term, edge or domain event; ADR-270's glossary wording is updated in S1.

### Decisions taken in AFK mode (for the maintainer to revisit)

- The aggregate holds the new default; setting false in the controller and the demo factory instead was rejected (two owners, `new Portfolio()` still on elsewhere).
- No change to the database column default: it would need a migration per provider and changes no outcome.

## DEVOPS — 2026-11-25 (AFK)

- **Platform risk:** none. No schema change, no migration, no configuration, port, secret or CI change.
- **Usage-data event:** N/A for a new event, because this feature adds no new user action. `DefaultFeatureSizingSwitchedOff = 29` stays unchanged (no rename, no renumber); it still counts Portfolios that leave default sizing. A name-only `DefaultFeatureSizingSwitchedOn = 30` would show whether people opt back in on new Portfolios; left as a follow-up for the maintainer, not built here.

## DELIVER — start (AFK)

- **Rigor profile:** `standard` for S1 (agreed with the maintainer on entry to AFK mode).
- **UI sketch:** none needed. No layout, control or copy changes; the childless-Feature look ("0 of 0", "No child Work Items yet") was sketched and approved in Story 6504. Screenshots of demo Portfolios may change content; they are listed at the hold.
