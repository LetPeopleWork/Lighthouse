# One place for the Node and pnpm versions — Story 6070

Delivered 2026-09-25 in two slices. It changes the build toolchain only and no product behaviour.
Pushed to `main` at `25afd35c9`; run 36140548736 was green through every verification job and
`sonar-gates`.

Nothing in the repository said which Node to use. CI hardcoded `24` in thirteen places, and pnpm
resolved to three different versions depending on the job. So a local green on Node 26 was no evidence
of a CI green. That's how Bug #6068 reached `main`, and ci-learnings already recorded two red builds
from the same kind of pnpm mismatch.

## What shipped

| Slice | What it changed |
| --- | --- |
| 01 — Node from `.nvmrc` | 12 `setup-node` steps read `node-version-file: '.nvmrc'`. The Dockerfile takes `ARG NODE_VERSION` with no default, passed from `.nvmrc`, so a build without it fails loudly and Dependabot no longer bumps the node image on its own. `.nvmrc` triggers CI and counts as a frontend and end-to-end change. Both projects declare `engines.node: 24.x` with `engineStrict: true`. |
| 02 — pnpm from `packageManager` | Every `pnpm/action-setup` reads `package_json_file:` with no `version:`. Corepack is gone from the four end-to-end workflows and the Dockerfile. `Lighthouse.EndToEndTests` is pinned to the same pnpm as the frontend. The Dockerfile installs pnpm with npm from `packageManager`. |

A dependency-free guard, `.github/scripts/toolchain-pins.mjs`, runs in `Verify Workflow Scripts`. It fails
CI, naming file and line, when a version is written anywhere else or the copies disagree. On the
maintainer's machine, `fnm` switches to the `.nvmrc` version on `cd` in fish and in the non-interactive
zsh shells agents use.

## Decisions worth keeping

- **Proof with a value you can tell apart.** A `24` read from a file looks the same as a literal `24`. So
  each single source was proved on a throwaway `features/` branch that set an older patch. Every job had to
  report `24.20.0`, and every pnpm install `10.33.0`. Runs 36136635678 and 36136640072.
- **No corepack.** Node 25 and later no longer ship it, so keeping it would have made the next Node major
  a multi-file breakage again.
- **One pnpm version for the repo.** This reverses the 2026-09-14 ledger rule that the end-to-end
  project was "deliberately not pinned". That rule held only while its workflows asked for `@latest`. The
  ledger entry has an appended note rather than a rewrite.
- **A Node major bump is three lines, not one:** `.nvmrc` plus both `engines.node`. The guard refuses a
  partial edit.

## Lessons

- **Starting CI isn't the same as running the right jobs.** The first probe showed an `.nvmrc`-only commit
  started CI, but change detection classified it as touching no project and skipped the frontend and
  end-to-end jobs. The acceptance criteria had named only the trigger paths. Checking which jobs *ran*,
  not just that the run started, is what caught it.
- **Mutation testing found missing specification, not weak assertions.** The first score was 74.85 %. Every
  fixture spelled each construct exactly one way. Specifying comments and formatting variants raised it to
  92 %, and turned up three real defects: a trailing `# comment` let a pnpm pin slip through, and made two
  valid lines read as violations. All fixed; final score 89.77 %.
- **Reviewer output needs checking.** One review approved while misspelling paths and citing line numbers
  that don't exist. Its main claims were re-checked by hand before being relied on.

## Quality

Guard suite 86/86. Mutation 89.77 % (gate 80 %), recorded in
`docs/feature/story-6070-toolchain-single-source/mutation/results.md`. Two adversarial reviews approved.
DES integrity: all 7 steps have complete traces.

## Still open

- The inline YAML form `with: { package_json_file: … }` is reported as missing the key. It's a false alarm,
  and nothing in the repository uses that form.
- No test puts a `#` inside a quoted YAML value, so four mutants on the comment-stripping regex survive.
- Upgrading to Node 26 (LTS from 2026-10-28) is now `.nvmrc` plus two `engines` lines.

Also fixed on the way, unrelated: `LicenseStatusPopover`'s 30-day renewal test added calendar days where
the component measures 30 × 24 hours. It failed every frontend run on the day before a clock change fell
30 days out.
