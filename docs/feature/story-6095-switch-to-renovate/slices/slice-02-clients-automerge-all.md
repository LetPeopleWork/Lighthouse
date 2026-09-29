# Slice 02 — clients-automerge-all

**Goal:** lighthouse-clients auto-merges every Renovate update, majors and security fixes included, once
`verify` is green. That puts it on the same rule as the Lighthouse repo.

**Stories:** US-03 (see `../feature-delta.md`).

## IN
- In `lighthouse-clients/renovate.json`: auto-merge patch/minor/major on all managers; `vulnerabilityAlerts.automerge: true`.
- In `lighthouse-clients/docs/release-model.md`: rewrite the "Dependency Updates" bullets to say so.

## OUT
The 7-day age, changesets, the release workflow, and the ruleset. All stay as they are.

## Learning hypothesis
- Disproves "auto-merging majors behind `verify` is safe for published packages" if a green major merge
  breaks the next release run or a published package.
- Confirms it when the next major (for example `@types/node` or a pnpm major) merges on its own and the
  following release is clean.

## Acceptance
AC-03.1 … AC-03.3.

## Dependencies
None. It is independent of slice 01.

## Effort / reference class
Under an hour. Same shape as #6088's config file.
