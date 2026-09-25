# Slice 02 — pnpm from `packageManager` everywhere

**Goal:** Every pnpm install (CI, Docker, Dependabot, local) uses the one version named in
`packageManager`, with corepack gone.

## IN scope
- `packageManager` added to `Lighthouse.EndToEndTests/package.json`, equal to the frontend's.
- 6 `pnpm/action-setup` steps: drop `version:`, add `package_json_file:`.
- 4 `corepack prepare pnpm@latest` steps → `pnpm/action-setup`.
- Dockerfile: install the `packageManager` pnpm without corepack or a literal.
- E2E lockfile regenerated under the pinned pnpm if a frozen install rejects it.
- Guard test, pnpm rules of D9.
- `docs/ci-learnings.md` 2026-09-14 amendment (AC3.7).
- Probe branch `features/6070-pnpm-probe`.

## OUT scope
- pnpm 11 upgrade, Node changes (slice 01).

## Learning hypothesis
- Confirms if green: the E2E lockfile and Dependabot's E2E PRs are compatible with a pinned pnpm 10.
- Disproves if it fails: the 2026-09-14 claim that pinning E2E "would create the disagreement" was
  still true — for example, Dependabot ignoring `packageManager` in a directory with no patches. The
  first Dependabot E2E PR after merge is the real test; record its result.

## Acceptance criteria
- Story 3 (AC3.1–3.7), Story 4 pnpm rules.
- Production data: the real probe run, the first `main` run, and the first Dependabot E2E PR.

## Dependencies
- Slice 01 (Node pinned; engine-strict in place).

## Effort estimate
- ~3 h plus waiting for a Dependabot PR (not blocking close-out; record it when it arrives).

## Reference class
- ci-learnings 2026-06-16 and 2026-09-14 fixes (lockfile regenerated under a pinned pnpm).

## Pre-slice SPIKE
- 15 min: `pnpm install --frozen-lockfile` in `Lighthouse.EndToEndTests` under pnpm 10.33.2 in a
  `node:24` container, to learn before the slice starts whether AC3.5 needs a lockfile regeneration.
