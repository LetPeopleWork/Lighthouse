# Feature: story-6385-s11-publishdir-outside-project

Story #6385 · S11 `PublishDir` outside the project folder · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-29-s11-publishdir-outside-project.md`](../epic-6264-modular-monolith/slices/slice-29-s11-publishdir-outside-project.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → S11.
- [REF] Cross-Cutting Impact Checklist (answered for this Story below).
- [REF] Outcome KPIs + guardrails: "Published binary size within noise" and "Single binary and the deployment
  topologies unchanged" are the guardrails this Story is measured against. No KPI row is touched.
- [REF] Wave Decisions and [REF] Risks / Open questions: nothing specific to S11.
- [REF] Improvement log: nothing since 2026-10-29 that changes S11.
- ADR-268 (ratchet convention): applies if DESIGN adds a guard against the old path.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Publish output lands in `Lighthouse.Backend/Lighthouse.Backend/publish/` and nests on the next publish: the main checkout holds `publish/linux-x64/publish/linux-x64/publish`, 151 MB. |
| 2 Persona | PASS | `lighthouse-maintainer` and the CI build. |
| 3 Domain examples | PASS | The four consumers in the re-count below; the nested folder in the main checkout; `.dockerignore` sending that folder into a Docker build context. |
| 4 UAT G/W/T | PASS | The brief's three AC, already Given/When/Then; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS | 1:1 with the brief's AC. |
| 6 Right-sized | PASS | Size S, likely a single slice (brief). |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None (brief and graph agree). |
| 9 Outcome measurable | PASS | Published file set and size, before and after, per RID. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-10-31)

| Brief says | Today |
|---|---|
| Output nests: `…/publish/linux-x64/publish/...` | Confirmed, three levels deep in the main checkout: `publish/linux-x64/publish/linux-x64/publish`. |
| ~151 MB locally | **151 MB** (`du -sh` on the main checkout). |
| Consumers: CI workflows, release workflow, Dockerfiles, Standalone packaging scripts | **4 files, 14 references**: `.github/actions/build-backend/action.yml` (6: three `-o`, three `rm` of `appsettings.Development.json`), `.github/actions/package-app/action.yml` (3: `chmod`, two artifact paths), `.github/workflows/ci_package-macos-standalone.yml` (1: `ARM64_DIR`), `Lighthouse.Frontend/package.json` `update-sidecar` script (3 in one line). |
| Dockerfile | Publishes to `/app/publish` inside the image already, so not a consumer. **But** `.dockerignore` excludes `**/bin` and `**/obj`, not `publish/`, so a local publish folder enters the Docker build context through `COPY ["Lighthouse.Backend/", "."]`. For DESIGN to weigh. |
| Website / helm repos | `website`, `lighthouse-platform`, `lighthouse-clients`: **no reference** to the old path. |
| `ci_verifymacos.yml` / `ci_verifywindows.yml` mention `publish/` | Not consumers: a `./publish/` folder of the downloaded DMG and the Standalone's runtime pid/log, unrelated to the project folder. |
| `.gitignore` | `publish/` already ignored (line 180). |

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no code path or authorization rule changes; only build output locations move.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes, and the clients repo has no
  reference to the path.
- Website: N/A, because nothing user-visible changes and the website repo has no reference to the path.
- Usage-data event: provisionally N/A, because nothing is added that a user could use; DEVOPS confirms.
- UI sketch: N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: published file set identical per RID, size within noise; routes, DTOs, pixels, topologies unchanged.
- [ ] Existing API, E2E and response tests green unmodified.
- [ ] Ratchet in the same change: whatever guard DESIGN chooses against the old path coming back (or "none, because …" stated).
- [ ] Moves separate from edits (here: the path change is one edit commit per consumer group, no code moves).
- [ ] Re-count before designing (done above).
- [ ] Mutation last, on frozen code: N/A expected, because no production C# or TS changes; DESIGN confirms.
- [ ] `ARCHITECTURE.md` and any ADR whose decision changed, updated in the same change.
- [ ] Payoff proven: local nesting gone after two publishes in a row; CI and release pipelines green on the new path.
- [ ] No user-facing release notes.
- [ ] One context at a time: N/A, because no context is packaged.
- [ ] Terminology: N/A, no new names in user-facing text.
- [ ] Cross-cutting re-check answered (above).
- [ ] `pnpm build` and `dotnet build` clean before push.
