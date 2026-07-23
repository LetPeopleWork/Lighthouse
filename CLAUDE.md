<!-- lean-ctx -->
<!-- lean-ctx-claude-v3 -->
## lean-ctx — Context Runtime

Canonical lean-ctx tool-mapping lives in the **global** `~/.claude/CLAUDE.md` (applies to every
project) and the `lean-ctx` skill (loads on demand). Not duplicated here — one place to update.
<!-- /lean-ctx -->

# Lighthouse — Claude Code Project Instructions

## Development Paradigm

This project follows the **object-oriented** paradigm. Use @nw-software-crafter for implementation.

- Backend: C# .NET 10 ASP.NET Core (OOP, ports-and-adapters / hexagonal architecture)
- Frontend: React 18 + TypeScript (functional-leaning hooks, but overall OOP project)
- E2E Tests: Playwright with Page Object Model pattern
- Test Framework (backend): NUnit 4.6 + Moq + Microsoft.EntityFrameworkCore.InMemory + Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory)
- Test Framework (frontend): Vitest + React Testing Library
- Test Framework (E2E): Playwright

## Mutation Testing Strategy

`per-feature` — Run Stryker.NET for backend C# mutations after each feature delivery. Run Stryker for TypeScript/React frontend mutations. Minimum kill rate: 80%.

## Architecture

Ports-and-adapters (hexagonal) on the backend. All RBAC business logic flows through `IRbacAdministrationService`. No component may fetch `/api/latest/authorization/my-summary` directly — all UI gating derives from the `useRbac()` hook.

See `docs/product/architecture/brief.md` for full architecture documentation.

## CI Learnings

**Before writing or changing ANY code, consult `docs/ci-learnings.md` and pre-apply every rule it lists.** It's the durable ledger of rules harvested from prior CI / SonarCloud failures — many of these sit below build-warning severity and only ever surface in CI, never in a local build. Re-introducing a rule already in the ledger burns a CI cycle. The `/clean-ci` command maintains this file; treat it as the source of truth rather than duplicating its contents here.

## ADO Work-Item Sync

Source of truth for "what's in flight" is the Azure DevOps board at `dev.azure.com/letpeoplework` (project `Lighthouse`). The `/ado-sync` slash command encodes the full sync workflow (Epic → child Stories/Bugs, state auto-transitions, pause-before-push, confirm-before-create/remove/Release-Notes-tag) — apply its rules proactively, not only when invoked.

## DISCUSS Wave & DELIVER Wave

`nw-discuss` and `nw-finalize` each carry their own checklists (RBAC impact, Lighthouse-Clients CLI/MCP versioning, website marketing surface for DISCUSS; docs prose, per-feature screenshots, demo data, website asset freshness for DELIVER). Apply those checklists in full when running those waves — see the command definitions for the current rules rather than this file, so there's one place to update them.

Two standing principles worth keeping visible here because they're easy to skip silently:

- **No silent N/A.** Every checklist item gets an explicit answer, including "N/A, because …" — never an implicit skip.
- **Per-feature, not batched.** Docs/screenshots/client updates happen at feature finalization, not deferred to `/release`. If `/release`'s `update-docs` pass finds a lot of drift, that's a signal the per-feature discipline was skipped — fix it at the feature level.

### Commits & Shared Contracts

- Conventional commits with scopes: `feat(payment): …`, `fix(user): …`, `refactor(order): …`, `test(payment): …`. Refactor commits separate from feature commits.
- Before editing a shared contract (DTO, API payload, cross-cutting interface): grep for usages and extend the relevant test factory/builder first to bound the blast radius.
- DRY = don't repeat *knowledge*, not code. Don't abstract structurally-similar code that represents different business concepts (e.g., `validatePaymentAmount` and `validateTransferAmount` may look identical but evolve independently).

### EF Migrations

- Use the existing `CreateMigration` PowerShell script to generate EF migrations across all supported database providers — do not invoke `dotnet ef migrations add` directly.

## Quality Gates (CI parity)

A change is **not done** until every gate below passes locally. CI enforces them; failing them wastes a CI cycle.

### Frontend (`Lighthouse.Frontend/`)

- `pnpm test` — must pass. This is the baseline prerequisite; nothing else counts as done while tests are red.
- `pnpm build` — must complete with zero errors and zero warnings (runs `tsc -b` then `vite build`).
- Biome — zero errors and zero warnings on `./src`. `pnpm biome check ./src` runs as the `prebuild` hook, so a clean `pnpm build` implies a clean Biome check.

### Backend (`Lighthouse.Backend/`)

- `dotnet build` — must succeed with zero warnings (`TreatWarningsAsErrors` makes any warning a failure, but verify locally before pushing).
- `dotnet test` — all xUnit suites green.

### SonarQube Cloud (both stacks)

- CI runs SonarQube Cloud analysis on every PR. Do not introduce new issues of any severity (bugs, vulnerabilities, code smells, security hotspots) — the gate fails on new violations even if existing debt remains.
- Common Sonar rule families to watch are tracked in `docs/ci-learnings.md` (see CI Learnings above) — that ledger is the canonical list, not this section.
- If a Sonar rule conflicts with a deliberate decision, suppress narrowly at the call site with a justification, not project-wide.