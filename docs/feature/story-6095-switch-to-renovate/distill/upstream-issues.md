# Story 6095 — upstream issues found in DISTILL

None of these is a contradiction between waves, so none blocks DISTILL. Items 1 and 2 need a
fix in DEVOPS text before or during DELIVER. The rest are for information.

## 1. KPI-7's toolchain check checks nothing (DEVOPS, Monitoring Contracts)

KPI-7 says to pair it with `node .github/scripts/toolchain-pins.mjs`. That module has no entry point.
It only exports `findToolchainPinViolations`, so running it prints nothing and exits 0 whatever the
tree holds. Verified on 2026-09-29: `node .github/scripts/toolchain-pins.mjs; echo $?` gives `0` with
no output.

Fix: use `node --test .github/scripts/toolchain-pins.test.mjs` (the real-repository tests read the
checked-out tree), or read the `Verify Workflow Scripts / workflow-scripts` job result on that merge.
That job is the other option KPI-7 already names. The `.feature` file uses the job result.

## 2. Slice 01 now has production code to mutate (DEVOPS, Mutation Testing Strategy per Slice)

DEVOPS rated slice 01 "N/A, `.github/scripts` is untouched". DISTILL puts two guards there:
`action-pins.mjs` and `dependency-bot.mjs`. Both are production code that runs in a required check,
so the 80% gate applies. Recorded under DISTILL Changed Assumptions, with the StrykerJS
command-runner precedent from Story 6070.

## 3. "Seven required checks" is eight everywhere except the DEVOPS resolution

DEVOPS added `Verify Workflow Scripts / workflow-scripts` to the `main` ruleset on 2026-09-29 and said
to read "eight" wherever "seven" appears. The text still says seven in D2, AC-01.4, the US-01 elevator
pitch, DES-5, the DESIGN driven-ports table and `environments.yaml` (the ruleset precondition lists 7
names). The `.feature` file says eight. Correct the others when the feature is finalized.

## 4. A toolchain PR is expected at the first run, not only on the next Node LTS

DES-3 says "only LTS majors are proposed. Node 26 isn't LTS yet, so nothing is proposed today". That
is true for Node. But DES-3 also says pnpm always jumps to the latest version, and pnpm 12.x is newer
than the pinned 10.33.2 (clients already runs 12.5.x). So `renovate/node-and-pnpm-toolchain` should
open at T0. DES-9 step 4 does not list it. That is the moment DoD 5 and the open DES-4 item (which
pnpm writes the lockfile on the toolchain PR) get answered. The `.feature` scenario for US-02 expects
it at T0.

## 5. ACs that no test in this repository can execute

These are testable, but only by observation. They sit in the `.feature` file as `@manual` scenarios
with the exact `gh` command, and they are not wired to a runner:
AC-01.2, AC-01.3, AC-01.4, AC-01.5, AC-01.6, AC-01.7, AC-01.8, AC-01.9, AC-01.10, AC-02.1, AC-02.2,
AC-02.3, AC-03.1 (behaviour), AC-04.2, and the rendering half of AC-04.4.

AC-01.1's "passes `renovate-config-validator`" is a one-off check at the cutover commit. A CI gate would
have to download the full `renovate` package on every run for a file that changes a few times a year,
which goes against the standing minimal-CI preference.
