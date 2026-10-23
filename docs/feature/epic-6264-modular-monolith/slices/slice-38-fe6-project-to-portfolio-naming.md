# Slice 38 — FE6 "Project" → Portfolio naming in the frontend

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M**

## Goal

Frontend code names follow the ubiquitous language. **S01's glossary is the source of truth** for every rename.

## IN

- About 21 production and 17 test files (survey 2026-10-22):
  - `ModifyProjectSettings`
  - `ProjectMetricsService` / `IProjectMetricsService`
  - `projectSettings`, `getProjectSettings…`
  - `projectId` in `UpdateSubscriptionService` / `MetricsService`
  - `createMockProjectSettings` in the tests

## OUT

- **The wire field `Feature.projects`**: an API contract read by Lighthouse-Clients CLI/MCP. It needs a versioned-API decision and is recorded as out of scope.
- **Not drift**, because these are tracker-native "projects": Jira `projectKey`, `getStatesForProjects`, `/suggestions/*/projects`.
- **Backend counterpart** `GetFeaturesForProject`: handled in S06 (connector port), not here.
- User-visible copy: it already comes from Terminology and stays unchanged.

## Hypothesis

The renames are mechanical. **Disproved if** a rename leaks into a wire field or route. Stop there, because those are out of scope.

## Acceptance criteria

- [ ] `grep -rnwi "project" src --include=*.ts* | grep -v test` lists only the documented exceptions.
- [ ] No route, wire field or visible text changes. Tests green.

## Dependencies

- Hard: S01 (glossary).
- Soft: FE1.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
