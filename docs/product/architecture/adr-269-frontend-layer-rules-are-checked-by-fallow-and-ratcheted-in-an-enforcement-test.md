# ADR-269: Frontend layer rules are checked by Fallow and ratcheted in an enforcement test

**Status**: Accepted (2026-10-29, DESIGN of Story 6389, propose mode, maintainer sign-off)
**Date**: 2026-10-29
**Feature**: `docs/feature/story-6389-fe1-import-boundary-enforcement/`

## Context

`Lighthouse.Frontend/src` is layered by folder (`components`, `hooks`, `services`, `models`, `utils`,
`pages`), but nothing enforces the layers. A component importing from a page compiles, lints and reads
fine in review. Sixteen production files do it today, in 21 imports. Before the frontend can be packaged
by module, the existing leaks must stop growing and each removal must show.

Biome, which already runs in `prebuild`, has no import graph: its `noRestrictedImports` matches import
strings per file, so an allow-list can only exclude whole files. dependency-cruiser does not support the
repository's TypeScript 7 and scans nothing without a second, older compiler. Fallow builds one module
graph, checks zone rules in 0.4 s, and found the same 21 imports as a hand count and as Biome.

## Decision

- **Fallow checks the layer rules.** `Lighthouse.Frontend/.fallowrc.json` declares one zone per top-level
  `src/` layer. Fallow rules are allow-lists, so each constrained layer allows every other zone except the
  ones it must not reach: components ↛ pages, hooks ↛ pages and components, services ↛ hooks,
  models ↛ pages, utils ↛ services. Only production files are checked. Fallow is pinned to an exact version.
- **One enforcement test owns the list**, following ADR-268. `src/layerBoundaries.enforcement.test.ts`
  runs `fallow architecture --boundaries --production -f json` and compares the reported imports, as
  `from → to`, with an inline list. A new leak fails, naming the import; a listed import that no longer
  leaks also fails, naming it, until its entry is deleted. One entry per import (a `from → to` edge naming both
  files), not per file, so a file already on the list cannot gain a second leak unnoticed; this refines ADR-268's
  "file or type" to the import edge. Entries are counted, so a second identical import is a newcomer too.
- **The list is never loosened.** A new leak is fixed, not listed: move the shared piece to a layer both sides may
  import (usually `components`, `hooks` or `models`), or pass it in from the page. A rule whose leaks are all gone
  keeps its rule with no entries.
- **It runs in the Vitest suite**: `pnpm test` locally, `pnpm run sonarreport` in the frontend CI job. Not in
  `prebuild` as well.
- **Fallow is used for boundaries only.** Its dead-code, duplication and health reports are not enforced.

## Alternatives considered

- **Biome `noRestrictedImports`.** No new dependency and no extra time, but each allow-list entry is a
  whole file. A stale entry passes silently, as it does with the other tools.
- **dependency-cruiser.** It could express the rules and has a baseline. Rejected because it needs a
  TypeScript below 7.
- **Fallow's own baseline file (`--baseline`).** It fails on a newcomer but ignores a stale entry, so the
  list would need a second check anyway. One inline list in one test is simpler.

## Consequences

- One more dev dependency, released often. The repository's 7-day minimum release age and Renovate
  automerge absorb the cadence; a bump that changes the JSON shape goes red rather than silently green,
  because an unparsed result cannot equal the list.
- Module zones (packaging the frontend by module) extend the same `.fallowrc.json`.
- A top-level `src/` folder added later is unconstrained until a zone names it.
