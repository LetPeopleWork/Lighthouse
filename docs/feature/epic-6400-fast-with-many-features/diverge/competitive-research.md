# Competitive Research — epic-6400-fast-with-many-features (DIVERGE, Phase 2)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-23 · **Depth**: lightweight (orchestrator config). Web
research was done directly with WebSearch/WebFetch, not through `nw-researcher`.

This is a performance Epic, so the "competitors" are how comparable tools and well-known architectures keep
large-data web apps responsive while they recompute. Each entry gives what it does for the job (cost per screen
bounded by what it shows; foreground served first), where it fails the job, and what it assumes about users.
Source quality is marked: **[O]** official vendor documentation, **[S]** secondary (community, reverse-engineering,
blog).

## 1. Comparable products

### 1.1 Jira Cloud boards: a display cap with an honest warning [O]

- **Behaviour**: a board, backlog or timeline shows at most 5,000 issues, even when the filter matches more. Above
  that, the admin narrows the JQL or splits the board. Since 2024 an over-limit board shows the first 5,000 with a
  warning, where it used to fail to load. Team-managed boards used to show only a generic "Something's gone wrong"
  (JRACLOUD-85634, -87219).
- **Serves the job**: the screen's cost is capped by construction, and the user is told why.
- **Fails the job**: it truncates. The user does not see all their work, and the remedy is put on an admin.
- **Assumes**: users accept a hard size limit and will reshape their queries.
- **Lesson for us**: the "push back on size" direction exists in the market, and it shows **an honest state at the
  limit**. Before 2024, Jira's failure look was a generic error, the same failure as our W7.
- Sources: [Jira Cloud: issue limits in company-managed projects](https://support.atlassian.com/jira-software-cloud/docs/issue-limits-in-company-managed-projects/);
  [JRACLOUD-85634](https://jira.atlassian.com/browse/JRACLOUD-85634); [JRACLOUD-87219](https://jira.atlassian.com/browse/JRACLOUD-87219);
  [JRACLOUD-86010](https://jira.atlassian.com/browse/JRACLOUD-86010).
- Fetched 2026-10-23. The 5,000 display cap and the 2024 change are as the Atlassian page and the tickets read that
  day.

### 1.2 Jira Plans (formerly Advanced Roadmaps): the calculation is bounded, the user scopes the plan [O]

- **Behaviour**:
  - Current Cloud docs give a plan load limit of 10,000 work items.
  - Legacy Server docs give 5,000 issues, 2,000 per hierarchy level and 100 projects.
  - Atlassian states why: limits "prevent the schedule of a plan from becoming too large ... to calculate", and an
    over-large plan "can cause your Jira instance to time out".
  - Recommendations: remove work not relevant to planning, split large plans, narrow the sources, and **exclude
    completed work and old releases**.
- **Serves the job**: it protects the instance from the expensive calculation. This is our forecast round (W8, W8b).
- **Fails the job**: the user does the work, by splitting and excluding.
- **Assumes**: planners will split plans; completed work is rarely needed in the planning view.
- **Lesson for us**:
  - The **calculation** is the thing a planning tool has to bound, not only the list.
  - "Exclude completed work" is the vendor's first lever. Lighthouse already has two levers of that kind: its
    Closed Items Cutoff, and a client-side hide-completed.
- Sources: [Performance recommendations for plans (Cloud)](https://support.atlassian.com/jira-software-cloud/docs/performance-recommendations-for-advanced-roadmaps/);
  [Working with plans (Server)](https://confluence.atlassian.com/advancedroadmapsserver0329/working-with-plans-1021218730.html);
  [Limits on plan size (community)](https://community.atlassian.com/forums/Advanced-Planning-in-Jira/Limits-on-plan-size/td-p/2322675).
- Fetched 2026-10-23. The 10,000-work-item figure is from the Cloud performance page as it read that day; the 5,000
  figure is from the legacy Server docs. Plan limits change, so re-check them before quoting.

### 1.3 ActionableAgile Analytics (55 Degrees): scope the load at the source [O, release notes]

- **Behaviour** (2020 release notes):
  - They loaded items in larger batches (10 to 50) to speed up loading.
  - They "significantly reduce[d] the data fetched ... to import custom fields".
  - They fixed a "Load issues created after X date" filter, which is their size lever.
  - No published item limit was found.
- **Serves the job**: it fetches less per load, which is the same move as our W2–W5 fixes, at the tracker boundary.
- **Fails the job**: date filters push the scoping onto the user. Analytics tools mostly recompute on load, so
  repeat visits pay again.
- **Assumes**: an analyst loads one data set per session and accepts a load step.
- **Lesson for us**: the nearest flow-analytics peer improved speed by **fetching only the fields it shows**, not by
  caching or architecture.
- Source: [ActionableAgile 2020 releases](https://55degrees.atlassian.net/wiki/x/CwC_cw). Nave was not checked
  (lightweight depth) and is not cited.

### 1.4 Non-obvious alternative: Linear's local-first sync engine (different architecture, same job) [S]

- **Behaviour** (secondary and reverse-engineered sources, not Linear's own docs):
  - The browser keeps a local copy in IndexedDB.
  - Core models are fetched at bootstrap, heavier ones lazily or per query.
  - Later sessions load only **deltas**, and writes land locally first.
- **Serves the job**: navigation and lists are instant after the first sync, which is exactly the reporter's wish
  for a cached scaffold (Maintainer answers, 3).
- **Fails the job here**: the first sync on a large workspace is the cost we already have. It needs a sync protocol,
  per-model permissions (our C1 RBAC readability) and conflict handling, which is far beyond a self-hosted
  analytics tool's needs.
- **Assumes**: the data a user needs fits on their device, and the server can push changes.
- **Lesson for us**: the cheap half of the idea is to **render the shell from cache and revalidate**. TanStack Query
  already provides it (stale-while-revalidate: stale data renders at once while a background refetch runs), and it
  is set up in `App.tsx:237`. The expensive half (sync, deltas) is not needed.
- Sources: [performance.dev: What makes Linear feel instant](https://performance.dev/what-makes-linear-feel-instant);
  [reverse-linear-sync-engine](https://github.com/Aslam97/reverse-linear-sync-engine);
  [TanStack Query: important defaults](https://tanstack.com/query/latest/docs/framework/react/guides/important-defaults).

## 2. Architectural prior art

### 2.1 Precomputed results read cheaply: recording rules and read models [O/S]

- **Prometheus/Grafana recording rules** [O, Grafana docs]: an expensive expression is evaluated on a schedule and
  stored as a new series. Dashboards read the stored result instead of recomputing it on every refresh. The cost is
  extra stored series and another definition to maintain.
- **CQRS read models / materialised views** [S]: denormalised read models are kept in step by events, so queries
  avoid joins. They are **eventually consistent** and add complexity; they fit read-heavy systems.
- **Lesson for us**: Lighthouse's forecasts are **already** a recording rule. They are computed in the background
  and stored on each Feature. The stored result is not the problem. The problem is that the reads around it load
  every Feature with its histograms when a screen needs only names, ids or references (W2, W4, W5). A full read
  model is one way to fix that; a projection at query time is a cheaper one. Our `FeaturePositionMap` already
  projects three columns over all Features cheaply (W4b, X2).
- Sources: [Grafana: recording rules](https://grafana.com/docs/grafana/v10.4/alerting/fundamentals/alert-rules/recording-rules/);
  [archman.dev: CQRS and read models](https://archman.dev/docs/distributed-systems-and-microservices/data-in-microservices/cqrs-and-read-models).

### 2.2 Keep the request pool for requests: thread-pool starvation in .NET [O/S]

- **Microsoft's guidance** [O]: thread-pool starvation makes an app respond slowly when no pool thread is free for
  new work. It is confirmed with `dotnet-counters` (queue length, thread count), then located with `dotnet-stack`
  and `dotnet-trace`.
- **Common causes** [S]: synchronous I/O and sync-over-async (`.Result`, `.Wait()`) block pool threads; the fix is
  async end to end. Long-running CPU work on the pool has the same effect. A Kestrel heartbeat warning is a
  symptom, not a diagnosis.
- **Lesson for us**:
  - W8 lists exactly these ingredients: a synchronous gather and save, `Parallel.For` workers on the pool, and
    synchronous EF reads on every request.
  - The measurement slice's step 6 uses the official diagnosis route. Isolation is a separate lever from cost: it
    helps even if every read stays as heavy as today.
- Sources: [Microsoft Learn: debug thread-pool starvation](https://learn.microsoft.com/nb-no/dotnet/core/diagnostics/debug-threadpool-starvation);
  [C# Corner: diagnosing thread-pool starvation](https://www.c-sharpcorner.com/article/diagnosing-thread-pool-starvation-in-asp-net-core-apis/).

## 3. Synthesis: assumptions each approach makes

| Approach | Product / prior art | Moves | Assumes | Relevant here because |
|---|---|---|---|---|
| Cap and warn | Jira boards | per-screen cost | users reshape queries | O7, the nudge; an honest state at the limit |
| Bound the calculation, exclude done work | Jira Plans | calculation cost | completed work is rarely needed | O9, K4; done-first scoping |
| Fetch only what is shown | ActionableAgile | per-load cost | the shown fields are known | O2, O3, O4; W2–W5 |
| Shell from cache, revalidate | Linear (cheap half), TanStack Query | perceived first paint, repeat visits | stale-for-a-moment is acceptable when marked | O1, O8; "Design for a slow connection" |
| Precompute the expensive answer | recording rules, CQRS read models | read cost, at write-time cost | reads ≫ writes; eventual consistency is acceptable | forecasts already are; lists are not |
| Isolate background work from the request pool | .NET starvation guidance | stall during recompute | the stall is starvation, not locks or GC | O5, K3, V1a |

**Gate G2**:
- Real products named: Jira Cloud boards, Jira Plans, ActionableAgile, Linear. **PASS**
- Non-obvious alternatives: Linear's sync engine (a different architecture for the same job) and recording rules
  (a different category: monitoring). **PASS**
- No generic market claims; every claim is sourced and graded [O] or [S]. **PASS**
- Gaps: Nave not checked; Linear known only from secondary sources.
