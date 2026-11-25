# Story 6504 — Choose whether a Feature without children gets an inferred size

ADO: User Story 6504 (stand-alone, no parent Epic). Product authority: `handoff.json`, rendered in `brief.md`.

## DISCUSS — 2026-11-25

### Wave route

- DISCOVER and DIVERGE skipped by the maintainer in as many words ("skip discover and diverge for 6504").
- DISCUSS ran interactively: the maintainer answered every Product decision in the host conversation.
- No Epic, because it is a single Story. `/continuous-improvement before <epic-id>` does not apply, as it needs an Epic id.

### Maintainer decisions

| # | Question | Decision |
|---|---|---|
| 1 | Shape of the feature | One general switch to turn default Feature sizing off. Off: a Feature without child Work Items takes 0, "and we live with the consequences". |
| 2 | Forecast when the switch is off | No special handling. Forecasts take the 0 as it comes. |
| 3 | Does off also cover the "override real child count states" rule? | Yes. Off means Lighthouse never puts a default size in place of real children. |
| 4 | Where the switch lives, default | Per Portfolio, in its advanced settings next to Default Feature Size. On for existing and new Portfolios, so nothing changes on upgrade. |
| 5 | What a childless Feature shows when off | 0 remaining plus "No child Work Items yet" where today's warning icon is (Feature lists on Portfolio and Team pages, Delivery timeline). |
| 6 | Size-estimate field when off | No effect: always 0. While the switch is off, Default Feature Size, the percentile option, the size-estimate field mapping and the override states are greyed out; their stored values are kept and come back when the switch is on again. |
| 7 | Stronger "assumed" mark when on | No. Today's warning icon and sentence stay. |
| 8 | Quint scenario exploration | Refused by the maintainer: the off rule has no open combinations left. |

| 9 | Old records | Stay as recorded: Delivery metrics history and archived Deliveries keep the "using default size" flag they were captured with. |
| 10 | What the switch does not touch | Done Features, Features with child Work Items and the percentile calculation of Default Feature Size. |

### Maintainer decisions taken before AFK mode (2026-11-25)

- **Rigor profile:** `standard` for every slice (crafter, acceptance designer, architect on `sonnet`; reviewers and EXAMINE on `haiku`).
- **AFK session:** slices wrap up with refactor + review; mutation runs once in the final gates. Nothing is pushed per slice; everything is pushed together after the final gates.
- **UI sketches, approved as drawn** (reused controls: the MUI `Switch` already in the Default Feature Size group, and today's warning icon + tooltip in `WarningsIndicator`):

```text
S1  Portfolio settings > Advanced > Default Feature Size (existing InputGroup)

  [x] Infer a size for Features without child Work Items   <- NEW, first, MUI Switch
      When off, a Feature without child Work Items counts as 0.

  [ ] Use Historical Feature Size To Calculate Default     (greyed when off)
      Feature Size Percentile [ 85 ]  History in Days [ 90 ]  (greyed)
  Default Number of Work Items per Feature [ 25 ]          (greyed)
  Size Estimate Field [ v ]                                (greyed)
  Size Override State  [chips...]                          (greyed)
  (values kept; switching on brings them back)

S3  Feature list row, switch OFF, no child Work Items

  Name             | Progress            | Forecast ...
  Checkout Revamp  | (i) 0 of 0          | ...
                     tooltip: "No child Work Items yet for this Feature.
                     It counts as 0 because default Feature sizing is
                     off in this Portfolio's settings."
  (same icon spot on the Delivery timeline bar)
```

Copy uses the configurable terms: "Feature", "Work Items" and "Portfolio" render as the user's own words.

Still **Open, owner Design**: the loading and failure looks of each changed view, and the context-map placement (below).

### Slices

| Id | What it delivers | Size | Depends on |
|---|---|---|---|
| S1 | The switch in the Portfolio's advanced settings. Off: dependent settings greyed out (values kept); a Feature without child Work Items shows 0 remaining, no Team carries work for it, forecasts count it as 0. On (the default for every Portfolio): nothing changes. | M | — |
| S2 | Off: a Feature in an "override real child count" state keeps its real child count. | S | S1 |
| S3 | Off: a Feature without child Work Items says "No child Work Items yet" where the warning icon is today. | S | S1 |

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | No new action, page or permission. The switch is a field of the Portfolio settings, written through the existing `PUT` on the Portfolio (`RbacGuard(PortfolioWrite)`, `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioController.cs:105`) and read through `GET …/settings` (`PortfolioRead`, `:203`). The settings UI keeps gating through `useRbac()`. |
| Lighthouse-Clients CLI / MCP | The Portfolio settings payload gains the switch; the Feature payloads gain nothing. The client's `updatePortfolio` passes an untyped payload through (`lighthouse-clients/packages/client/src/index.ts:2544`), so no client code change is needed to carry it. **Constraint for DESIGN:** a client or older UI that sends a Portfolio payload without the new field must leave the switch as it is (or On), never turn it off by a missing-field default. Client version bump: none expected; confirm at finalize. |
| Website marketing surface | One line is worth it, e.g. "Turn off default Feature sizing: a Feature without child Work Items counts as 0, never a guess." No new asset needed; the settings screenshot is refreshed at finalize if the docs show it. |
| Context map | Deferred to DESIGN (allowed at the latest there). Proposed by the Product Owner: contexts Portfolio configuration, Feature sizing and Forecasting; glossary term "default Feature sizing"; a Portfolio settings change triggers the existing Feature recalculation (domain event preferred over an inline call); UI surface the Portfolio's settings tab (ADR-271). DESIGN confirms against ADR-270 and updates it and `ARCHITECTURE.md` §4 if a term or edge is new. |

## DESIGN — 2026-11-25 (AFK, propose mode)

Authority: `architecture.md` (rendered from `handoff.json`), Quint model and receipt in `model/`.

- **Rule owner:** `Portfolio`'s two selectors (`GetFeaturesToExtrapolate`, `GetFeaturesToOverrideWithDefaultSize`) return nothing when the new `InfersDefaultFeatureSize` is off; `WorkItemService` is unchanged.
- **Wire:** `bool?` on `PortfolioSettingDto`, absent keeps the stored value (same as `IgnoreDependencies`), so an older client cannot switch it off. New column NOT NULL, default true (expand-only).
- **Note (S3):** a derived `Feature.HasNoChildWorkItemsYet` on the Feature payloads, so the UI never re-derives the rule. This changes the DISCUSS checklist answer "Feature payload gains nothing": it gains one boolean. Clients pass it through.
- **Context map:** no new context, edge or domain event (the refresh cycle already recomputes from stored settings). New glossary term "default Feature sizing" (the switch) next to "default Feature size" (the number); S1 adds it to ADR-270 and `ARCHITECTURE.md` §4. UI surface: Portfolio settings, Advanced, Default Feature Size group.
- **Loading / failure looks:** every changed view lives inside a view that already has both (settings page gate, `FeatureListDataGrid` status, Delivery timeline); no new look.

### Decisions taken in AFK mode (for the maintainer to revisit)

- **Team page shows no note.** With the switch off a childless Feature carries no Team work, so it is not listed on any Team page at all; the note shows on the Portfolio Feature list and the Delivery timeline only. This follows from the confirmed rule "no Team carries work for it"; listing such Features on Team pages would be a product change.
- The setting takes effect on the next refresh cycle, as Default Feature Size does today.
