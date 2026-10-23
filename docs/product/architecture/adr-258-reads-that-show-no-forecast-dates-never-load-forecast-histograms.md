# ADR-258: Reads that show no forecast dates never load forecast histograms

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode).
**Feature**: `epic-6400-fast-with-many-features` (Stories #6406 and #6407, slices 02 and 03)
**Relies on**: ADR-135 (a Feature's position is global), the dependency decision's single projection
(`DependencyFacts`), ADR-163 (narrowed repository readers; Epic 6264 generalises it).

## Context

Two include sets serve almost every read of Portfolios and Features:

- `PortfolioRepository.GetAllProjectsWithIncludes` loads every Portfolio with every Feature, its Team work, and every
  forecast with its histogram rows (`SimulationResults`). `GET /portfolios`, `GET /teams`, `GET /teams/{id}` use it to
  show names, counts and Feature references. `GetById` runs `SingleOrDefault` on the materialised `IEnumerable`, so
  reading one Portfolio reads all of them.
- `FeatureRepository.GetFeatures` loads every Feature with completion and start forecasts and their histograms. The
  Features list reads it for the **whole instance** to decide dependency verdicts, once for the rows and once more for
  the parent column, and `TeamMetricsService` reads it once per in-progress Feature through `GetByPredicate(Func)`.

Histogram rows are by far the largest table behind these reads. None of these callers shows a percentile: the
catalogue shows names and counts; the dependency decision reads `CanBeForecast`, which needs each forecast's
`TeamId` and stored `TotalTrials`, not its histogram.

## Decision

1. **`PortfolioRepository.GetById` filters in the database**, with its includes unchanged, so every existing caller
   (detail pages, the forecast updater, `ApiHelpers`) reads one Portfolio.
2. **Catalogue readers** on `IPortfolioRepository`: one for all Portfolios and one for a single Portfolio, both loading
   the root, its connection and options, and its Features with their Team work and Teams, **without forecasts**, read
   with no tracking (with identity resolution, so shared Teams stay one object as today). `GET /portfolios`,
   `GET /teams`, `GET /teams/{id}`, `GET /portfolios/{id}` and `GET /portfolios/{id}/settings` build their unchanged
   DTOs from them.
3. **A whole-graph reader for dependencies** on `IFeatureRepository`: every Feature with its Portfolios, Team work and
   Teams, its dependency references, and its completion-forecast **headers** (`TeamId`, `TotalTrials`), without
   histogram rows and without start forecasts, no tracking with identity resolution. `FeaturesController` feeds it to the
   unchanged `DependencyFacts` / `DependencyDecision`, so verdicts are computed by the same code from the same facts.
4. **In-progress by reference id**: `TeamMetricsService` takes the in-progress Feature references from the Team's WIP
   snapshot (it already does) and stops loading Features to count them. The `featuresInProgress` endpoint resolves the
   references with one database-side read by reference id, full includes, so its body is unchanged. Where two stored
   Features share a reference id (today the in-memory `SingleOrDefault` throws), the first in forecast order answers,
   counted once, with one warning per refresh naming the reference id: the dependency path's existing rule, listed for
   the maintainer (Q-M2).
5. **Names say what is left out** (`…WithoutForecasts`, `…WithoutHistograms`), so a caller that would read a percentile
   from such an entity sees the omission at the call site. Signatures take and return no `IQueryable`, `Expression` or
   `Func` (Epic 6264's rule). Final names are fitted to Epic 6264's S01 glossary when it lands.

## Alternatives considered

- **Projection records and new DTO constructors**: a partly loaded entity becomes unrepresentable, which is safer in
  principle. But every response body must stay byte-identical (D20), which means re-implementing each DTO's mapping from
  a record, a second mapping of the same knowledge. Rejected for this Epic; Epic 6264's S13 (Feature → Forecast
  navigation out of persistence) is where the partial entity disappears for good.
- **A second `DependencyFacts.About` overload over a projection record**: the class exists precisely so the screen and
  the refresh describe a Feature one way ("two projections would be two chances to leave something out"). Rejected.
- **Answer dependencies from the requested Features only**: two screens would disagree on a verdict (C2). Rejected
  before, maintainer Q8.
- **Cache the heavy reads**: stale names and counts after an update, a cache to invalidate per replica, and the memory
  of the very graph that is the problem. Rejected.

## Consequences

**Positive**
- The Overview, detail pages and the Team page read in proportion to what they show; the Features list's instance-wide
  part reads ids, references, Portfolios and forecast headers only.
- No response body, verdict, position or readability rule changes.

**Negative / accepted**
- An entity loaded through these readers carries no forecast histograms (or none at all). Reading a percentile from
  it would silently answer "no answer". Mitigated by the names, by no-tracking (it can never be saved), and by the
  golden tests below; removed for good by Epic 6264 S13.

## Enforcement

- Golden bodies: `GET /portfolios`, `/teams`, `/teams/{id}`, `/portfolios/{id}`, `/portfolios/{id}/settings`,
  `/features/ids`, `/features/references`, `/teams/{id}/metrics/featuresInProgress` are byte-identical before and after
  on the dev backup and the scaled set, SQLite and PostgreSQL.
- Generated graphs (NUnit `TestCaseSource` over seeded generators: cycles, shared references, ignored Portfolios, Teams
  without throughput): verdicts from the full read equal verdicts from the histogram-free read.
- Provider-backed test: `GetById` issues a `WHERE` on the id (SQL captured), never a scan of every Portfolio.
- Duplicate reference ids: with two stored Features sharing one, the in-progress answer counts the first in forecast
  order once and logs one warning naming the reference id.
- Epic 6264's S05a ratchet is per file, so this Epic lowers each touched file's count: `FeaturesController` drops one
  of its three `GetAllByPredicate` calls, `TeamMetricsService` drops its `GetByPredicate(Func)` call, and
  `ForecastService` drops its only call (that file is missing from Epic 6264's recorded baseline; noted for that Epic,
  not edited here). ArchUnitNET: no new caller of `GetAllByPredicate` outside the persistence adapters in the files
  this Epic touches.
