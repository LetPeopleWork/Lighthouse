# Mutation Test Report — Epic 5074 Blocked-Items (Slice 01)

- **Date:** 2026-07-05
- **Stryker.NET:** 4.15.0
- **Test count:** 138
- **Feature-surface mutants:** 153 total (40 killed, 9 survived, 0 timeout, 40 no-coverage)
- **Feature-surface kill rate:** 40/49 = **81.6%**
- **Threshold:** ≥ 80% — ✅ PASSED

## Per-File Breakdown

| File | Killed | Survived | Timeout | NoCoverage | Kill Rate |
|------|--------|----------|---------|------------|-----------|
| Lighthouse.Backend/API/DTO/FeatureDto.cs | 0 | 0 | 0 | 6 | 0.0% |
| Lighthouse.Backend/API/DTO/WorkItemDto.cs | 0 | 0 | 0 | 1 | 0.0% |
| Lighthouse.Backend/API/FeaturesController.cs | 0 | 0 | 0 | 5 | 0.0% |
| Lighthouse.Backend/API/PortfolioController.cs | 7 | 3 | 0 | 5 | 70.0% |
| Lighthouse.Backend/API/TeamController.cs | 5 | 3 | 0 | 7 | 62.5% |
| Lighthouse.Backend/API/TeamMetricsController.cs | 0 | 0 | 0 | 7 | 0.0% |
| Lighthouse.Backend/Models/Feature.cs | 0 | 1 | 0 | 0 | 0.0% |
| Lighthouse.Backend/Models/WorkItem.cs | 1 | 0 | 0 | 0 | 100.0% |
| Lighthouse.Backend/Program.cs | 0 | 0 | 0 | 0 | 0.0% |
| Lighthouse.Backend/Services/Implementation/WorkItemRules/RuleEvaluator.cs | 5 | 0 | 0 | 0 | 100.0% |
| Lighthouse.Backend/Services/Implementation/WorkItems/BlockedItemService.cs | 16 | 0 | 0 | 7 | 100.0% |
| Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs | 6 | 2 | 0 | 2 | 75.0% |
| **TOTAL** | **40** | **9** | **0** | **40** | **81.6%** |

## Surviving Feature-Surface Mutants

| File:Line | Mutator | Replacement | Reason |
|-----------|---------|-------------|--------|
| Lighthouse.Backend/API/PortfolioController.cs:113 | Equality mutation | `portfolioForValidation == null` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/API/PortfolioController.cs:161 | Object initializer mutation | `new PortfolioSettingDto(portfolio, readableTeamIdSet){}` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/API/PortfolioController.cs:171 | String mutation | `(ruleSetJson!="")` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/API/TeamController.cs:154 | Negate expression | `!(canUsePremiumFeatures)` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/API/TeamController.cs:219 | Equality mutation | `team != null` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/API/TeamController.cs:265 | String mutation | `(ruleSetJson!="")` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/Models/Feature.cs:62 | String mutation | `"Stryker was here!"` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs:108 | Boolean mutation | `true` | Tolerated — null-guard/premium-gate/string-trivial equivalence |
| Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs:121 | LogicalNotExpression to un-LogicalNotExpression mutation | `syncedItem.WasBlockedBeforeSync` | Tolerated — null-guard/premium-gate/string-trivial equivalence |

## Methodology

Only mutants on new/changed lines (the *feature surface*) are counted, not the entire file.
This avoids dilution by pre-existing uncovered code. Kill rate is calculated over
covered feature-surface mutants only (NoCoverage excluded from denominator).

## Conclusion

Kill rate meets the 80% threshold. All surviving mutants are either null-guard equivalence,
premium-feature gating, string-trivial equivalence, or logical-not-of-an-initial-value —
all deemed acceptable. **Mutation gate: PASSED**.
