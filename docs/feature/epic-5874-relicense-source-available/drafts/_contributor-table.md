### The historical outside contributions, measured

The business decision record behind this project recorded *"seven commits from
two people"*. That was wrong, and we corrected it from the repository on
2026-09-11. The actual position:

| Contributor | Commits | Dates | Contactable via |
|---|---|---|---|
| Lorenzo (`mr.milgauss@hotmail.com`) | 4 | 2025-03-10 → 2025-03-25 | email |
| Sascha Lucius (`sascha.lucius@posteo.de`) | 1 | 2026-04-13 | email |
| ZylkaGreger | 3 | 2026-06-23 → 2026-07-17 | GitHub only — commits carry a `users.noreply.github.com` address |

**Eight commits from three people, not seven from two.** All were accepted into
the repository while it carried the MIT License and its standard notice. No CLA
was in force at any time, and the repository has never carried contribution terms
beyond the licence itself.

What still survives on `main`, by `git blame`:

| Contributor | Shipped product code | Tests | Editor config / docs | Total |
|---|---|---|---|---|
| Lorenzo | 0 | 0 | 28 (`.vscode/`) | 28 |
| Sascha Lucius | 38 | 237 | 0 | 275 |
| ZylkaGreger | 21 | 129 | 1 (`docs/index.md`) | 151 |
| **Total** | **59** | **366** | **29** | **454** |

Per-file detail:

```
Lorenzo
    12  .vscode/launch.json
    16  .vscode/tasks.json

Sascha Lucius
   237  .../DeliveryGrid/DeliverySection.test.tsx
    34  .../DeliveryGrid/DeliverySection.tsx
     2  .../Portfolios/Detail/PortfolioFeatureList.tsx
     2  .../Teams/Detail/TeamFeatureList.tsx

ZylkaGreger
    47  .../Charts/EstimationVsCycleTimeChart.test.tsx
     3  .../Charts/EstimationVsCycleTimeChart.tsx
    38  .../Charts/FeatureSizeScatterPlotChart.test.tsx
     4  .../Charts/FeatureSizeScatterPlotChart.tsx
    33  .../MetricsView/BaseMetricsView.test.tsx
     5  .../MetricsView/BaseMetricsView.tsx
    11  .../MetricsView/widgetInfoMetadata.test.ts
     7  .../MetricsView/widgetInfoMetadata.ts
     2  .../MetricsView/WidgetShell.tsx
     1  docs/index.md
```

Three observations we would want you to weigh:

1. **Only 59 lines reach the distributed product.** Tests and `.vscode/` files
   are in the public repository but are not shipped in the Docker image or the
   standalone archives. They are still part of the work being relicensed.
2. **Lorenzo's surviving contribution is entirely editor configuration.** His one
   commit touching production code (an Azure DevOps query fix) is in a file that
   no longer exists.
3. **One contributor is reachable only through GitHub.** If written consent is
   the route you recommend, that is the constraint on it.

We have not contacted any of them. We would rather ask you first whether consent
is needed at all than approach three people with a request that turns out to be
unnecessary — and if it is needed, we would rather ask once, with wording you
have approved.

