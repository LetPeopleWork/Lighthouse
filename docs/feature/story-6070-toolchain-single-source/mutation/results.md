# Mutation testing — 6070 (one place for the Node and pnpm versions)

The run was on 2026-09-25 against `.github/scripts/toolchain-pins.mjs`, the only production code in this story;
everything else is workflow YAML, the Dockerfile and `package.json`. The gate is an 80 % kill rate.

| run | tree | killed | timeout | survived | score |
| --- | --- | --- | --- | --- | --- |
| first | `42fbf66e5` (guard complete) | 378 | 0 | 127 | 74.85 % |
| after test remediation | `0d82d11a6` | 464 | 1 | 40 | 92.08 % |
| final | `fb3fd989d` (trailing-comment fix) | 464 | 1 | 53 | **89.77 %** |

Setup: StrykerJS from `Lighthouse.Frontend/node_modules`, **command runner** over
`node --test .github/scripts/toolchain-pins.test.mjs`. The project's Stryker configs are built around
Vitest, and this script lives outside any package. The sandbox was a `git archive` of only the tracked
files the guard reads, so the tests that check the real repository run against the committed state.
Config: `stryker.6070.json`. Run it from a directory holding `git archive HEAD .github Dockerfile .nvmrc` plus both projects' `package.json` and `pnpm-workspace.yaml`.

## What the first run found

Most of the 127 survivors were regex anchors and `\s`/`\s+` variants. Every fixture spelled each
construct exactly one way, so nothing tested how the guard treats a comment, extra spaces, a
registry-prefixed image, or a neighbouring step's `version:`. The remediation added tests for those
cases without touching production code. Writing them found **four real defects**, all caused by trailing
YAML comments or unusual YAML syntax:

1. `with:  # inputs` on a pnpm step hid any `version:` under it, so a pin slipped through. **Fixed** in `fb3fd989d`.
2. `engineStrict: true # why` was read as off. **Fixed** in `fb3fd989d`.
3. `- ".nvmrc"  # why` in the CI trigger paths was read as not listed. **Fixed** in `fb3fd989d`.
4. Flow-style `with: { package_json_file: … }` is reported as missing it. **Accepted as a known limit.**
   It is a false alarm, not a miss, and nothing in the repository writes steps that way.

## What survives

The 40 survivors after remediation were reviewed one by one. 36 are equivalent for inputs a formatter or
Docker would actually produce: a space before a JSON colon, `"node":` on line 1 of a package.json, a
Dockerfile with no `FROM`, and line-number fallbacks that the `...(line && { line })` spread makes
unobservable. The other 4 marked defects 1 and 2, which are now fixed.

The final run has 53 survivors. That fix rewrote the lines it touched, so the final list has not been
re-reviewed mutant by mutant. What is known about the new ones:

- **Five are in the new comment-stripping regex.** Dropping its `^` is equivalent. The other four weaken
  the quoted-string branches, and survive because no test puts a `#` inside a quoted YAML value. It's a
  real gap, but a small one: the worst outcome is a value containing ` # ` being cut short, which none of
  the matched keys can carry.
- **The rest of the increase** comes from matching moving onto the stripped line, so earlier kills and
  survivors re-map. The score still clears the gate by almost ten points.
