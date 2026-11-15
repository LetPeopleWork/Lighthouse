# Story 6601 — Linux standalone builds no longer break when GitHub retires a runner image

ADO #6601 (User Story, tag `Continuous Improvement`, unparented). The plan is the ADO description; there is no
catalogue brief. Follows up on the quick fix `cc4d4ed60` (`runs-on: ubuntu-24.04`) and `a6d1d3a6b` (Renovate keeps
that pin).

DISCOVER, DIVERGE and DISCUSS: skipped by the maintainer in as many words (2026-11-15). Waves start at DESIGN.
Rigor: standard (Sonnet crafter, acceptance designer and architect; Haiku reviewers). AFK mode, per-slice pushes.

## Inherited [REF]

- `CLAUDE.md` → Wave Sequence, Quality Gates, Commits & Shared Contracts.
- `docs/ci-learnings.md` → the CI / pipeline section.
- ADR-283 (`docs/product/architecture/adr-283-…pkcs11-provider.md`): the precedent for a pinned container image built
  by its own pipeline (Story 6651).
- Memory: CI changes extend existing workflows; a dedicated workflow keeps its own required check.

## The problem, re-counted (2026-11-15)

| Fact | In the ADO description | Today |
|---|---|---|
| Linux packaging runner | `ubuntu-24.04` (quick fix) | `ci_package-linux-standalone.yml:15` `runs-on: ubuntu-24.04` |
| WebKitGTK pins | `2.44.0-2` | six packages pinned to `=2.44.0-2` (lines 69–74) |
| Renovate | — | a packageRule disables the `ubuntu` runner bump for this file |
| Other `*-latest` runners | macOS, Windows named | `macos-latest` in package-macos and verifymacos; `windows-latest` in package-win and verifywindows; `ubuntu-latest` in the verify jobs (no glibc exposure: they ship nothing) |

## Acceptance (from ADO)

1. The Linux standalone builds without naming a GitHub runner image that will be retired, or there is a documented,
   alarmed path for when it is.
2. The minimum glibc and distribution is written down (docs/ or ADR), and CI fails if a build exceeds it.
3. The AppImage still starts on Ubuntu 24.04 LTS.

## Checklist (improvement rules)

- [ ] Behaviour-free for users: the AppImage starts on the same distributions as today (Ubuntu 24.04 LTS, Debian 13).
- [ ] Ratchet in the same change: the glibc-floor check lands with the change that makes it pass.
- [ ] Moves separate from edits.
- [ ] Numbers re-counted at pick-up (above).
- [ ] Mutation: N/A unless C# or TypeScript production code changes.
- [ ] Architecture docs in the same change (ADR for the glibc floor).
- [ ] No user-facing release notes unless the maintainer says so.

## DESIGN

Architecture and decisions: `architecture.md` in this folder, ADR-284. Maintainer decisions (2026-11-15): the runner goes
back to `ubuntu-latest`; the build environment stays Ubuntu 24.04 (glibc floor 2.39); the six exact WebKitGTK pins are
dropped (rendering risk accepted: the maintainer starts the new AppImage once on a desktop before the release); no pull
request workflow for the oracles. Design review: one revision (ICU / setup-dotnet removed, silent-pass guard, loader job
specified, floor oracle case), then approved.

## DEVOPS

- Platform lens: carried in DESIGN (runner, container, Renovate, rollback by reverting the S2 commit, the first real
  container build is seen on main).
- Usage-data event: N/A, because this is release tooling that no Lighthouse user runs.

## DELIVER start

- Rigor: `standard` for S1 and S2 (Sonnet acceptance designer and crafter, Haiku reviewers and examiner), as agreed.
- UI sketch: N/A, because nothing a user sees in the app changes (the WebKitGTK update is covered by the manual desktop
  start above).
- Loading and failure looks: N/A, no view.
