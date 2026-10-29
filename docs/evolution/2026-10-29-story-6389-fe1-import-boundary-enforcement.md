# The frontend layers stop leaking further — Story 6389

Delivered 2026-10-29 as item FE1, ADO #6389 ("Import-boundary enforcement + ratcheting allow-list"), from Epic #6264
(path to a modular monolith). Pushed to `main` in `0a14f4d31..190c68193`, CI 40919136799 green (only the Standalone
release gates waiting). The Story is Closed; the Epic's state is left to the maintainer. Behaviour-free, so there are
no release notes.

Before this Story, nothing related the frontend's layers. `src/` is split into `components`, `hooks`, `services`,
`models`, `utils`, `pages` and `context`, but a component importing a page compiled, linted and read fine in review.
Sixteen files did so in 21 places, and a 22nd would have gone unnoticed.

Now `pnpm test` (and CI's frontend job, which runs the same Vitest suite) checks five layer rules with
Fallow (the `fallow` npm package), and compares what it finds with a named list:

| Rule | Allowed today (file pairs) |
| --- | --- |
| components never import pages | 12 |
| hooks never import pages or components | 5 |
| services never import hooks | 2 |
| models never import pages | 1 |
| utils never import services | 1 |

A new leak fails the test, naming the importing file and the imported one. A listed leak that has been fixed also
fails it, until its entry is deleted, so the list only shrinks. FE2, FE3 and FE5 remove the 21.

## What shipped

| Step | What it changed |
| --- | --- |
| 01-01 | `fallow` 3.36.0 as an exact-pinned dev dependency; `Lighthouse.Frontend/.fallowrc.json` with the seven layer zones and the components rule; the first walking-skeleton stage finds exactly the 12 listed components → pages pairs (`8cfd4e4f7`). |
| 01-02 | A new component → page import fails as one entry naming both files; test files are not checked; three imports of the same page are one entry (`c4cdac745`). A stray copy under `.stryker-tmp/` is not reported, checked by hand; the evidence is in the commit body. |
| 01-03 | Duplicate and stale entries are refused (counted, not set-based); allowed directions report nothing; a check that cannot run fails quoting what Fallow printed; `ARCHITECTURE.md` §12 bullet (`ebe78d83c`). |
| review | A component nothing imports and a lazy `import()` are both reported, pinned against a later Fallow release that stops doing so; the child process has a 60 s timeout and a fixed working directory; an exit status that disagrees with the reported violations fails (`50feb72df`). |
| 02-01 | The other four rules; the second walking-skeleton stage compares the whole 21-entry list (`a6e7506a0`). |
| refactor | The `ARCHITECTURE.md` fix sentence holds for every rule (`970be60a3`). |

DESIGN, DEVOPS and DISTILL are `b3bb11570`, `fee9bac92` and `141ff0ebc` with their review passes; ADR-269 records the
decision and ADR-268 carries a status note that its ratchet convention now covers the frontend.

## Key decisions

- **Fallow, not Biome or dependency-cruiser.** Biome's `noRestrictedImports` matches import strings per file, so it
  cannot say which layer a resolved import lands in. dependency-cruiser scans no modules under the repo's TypeScript 7.
  Fallow resolves the import graph itself and took 0.4 s over the whole frontend in the trial.
- **The list lives in a Vitest enforcement test, not in Fallow's baseline file.** Fallow's baseline fails a newcomer
  but lets a stale entry pass silently; the test compares both ways.
- **One entry per file pair, counted.** A listed file must not gain a leak to a second target unnoticed, and Fallow
  reports a pair once however many statements connect the two files.
- **It runs in `pnpm test`, not in `prebuild`.** The test already gates CI; a second run would only add time.
- **Mutation testing is not applicable.** The Story adds no production code; the comparison helper lives in the test
  on purpose.
- **The maintainer waived the scenario-count rule of the Definition of Ready** (17 tests over five rules, one
  outcome, two half-day slices); the review later added two more.

## Numbers

- Scenarios: 19 of 19 green, each shown to fail when its rule or guard was broken.
- `pnpm test`: not slower. Alternating runs (with and without the check) under a parallel mutation load: medians
  251.5 s vs 254.3 s after the first slice, 245.8 s vs 249.5 s after the second. The test itself takes about 2.3 s
  in its own worker.
- Epic KPI 8 (frontend import-boundary violations): 21 file pairs in 16 files, now held by the ratchet; target 0.

## Lessons

- **A probe settles a reviewer's "might".** The first slice review blocked on the guess that Fallow might skip a file
  no entry reaches. Running Fallow on a two-minute fixture answered it, and the answer became a pinned scenario, which
  also protects against Fallow's weekly releases (Renovate automerges on green).
- **Time a suite under load by alternating, not by a before/after pair.** A parallel mutation run in another checkout
  made a "before" baseline worthless; alternating runs of the suite with and without the new test put the load on
  both sides.
- **Wrap a slice's review and the final gates into one pass when the last slice is the whole remaining scope.** They
  read the same three files.

## Found in passing

- Fallow's dead-code report (not enforced here): `camelcase-keys`, `lodash.debounce` and `lorem-ipsum` are imported
  nowhere, and `ImageComponent` and `LicenseStatusDialog` are used only by their own tests. Proposed as a catalogue
  item in the Epic's Improvement log; not yet on the board.
- `usageDataEvents.test.ts` ("hands in what it noticed when the tab is put away") failed once in six full runs under
  load, on a `waitFor` that expired. Unrelated to this Story; a flake candidate.
