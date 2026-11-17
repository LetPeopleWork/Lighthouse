# ADR-287: A Cycle Time outlier is above the Cycle Time upper natural process limit, judged by a replaceable backend outlier definition
Status: Accepted (shipped 2026-11-17)

## Context

```text
ADR-282 set Cycle Time scatterplot outliers at five times the median, worked out in the frontend. A team saw more than 20 outliers while its Cycle Time process behaviour chart flagged far fewer, so the two charts told the reader different stories. The maintainer wants the outliers to match the chart, a switch to see every item, no configuration for now, and a rule that can be replaced later.
```

## Decision

```text
The backend Flow metrics module judges outliers through ICycleTimeOutlierDefinition. Its one implementation, the default, sets apart the closed items whose Cycle Time is above the unrounded upper natural process limit of the Cycle Time process behaviour chart: the same baseline, formula and comparison as XmRCalculator, also for a named Cycle Time. Without usable limits it answers NoLimits and nothing is set apart; there is no fallback to the median rule. GET metrics/cycleTime/outliers serves the verdict for a Team and a Portfolio, and the scatterplot splits its items by the ids in that verdict, asking with a query keyed on owner, range and Cycle Time. The verdict and the definition live in the module's Application layer; the limit value object stays in its Domain, shared with the process behaviour chart. A "Hide outliers" switch, on whenever the chart opens and never stored, lets the reader draw every item. Supersedes decisions 1 to 3 of ADR-282 for the Cycle Time scatterplot; its decisions 4 to 7 stay.
```

## Alternatives

```text
Reuse the cycleTime/pbc answer in the frontend: its limit is rounded, so it disagrees with the chart at the boundary, and it has no named Cycle Time variant. Recompute the limits in TypeScript: a second owner of the rule. A frontend strategy: named Cycle Times and the baseline live in the backend. Keep five times the median: disagrees with the chart.
```

## Consequences

```text
One additive route and DTO; Program.cs registers the default definition; the Outlier glossary entry of ADR-270 changes and Upper natural process limit is added; no process behaviour chart output changes. Another outlier definition is one new class and one registration. The scatterplot now waits for one more request before it sets anything apart, with a loading and a could-not-load look.
```
