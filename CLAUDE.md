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
- **Write the configurable term, not one tracker's word for it.** Everything a user can rename under Settings → Terminology (feature, work item, team, portfolio, delivery, cycle time, throughput, WIP, blocked, SLE) renders as *their* word. Docs, release notes and UI fallback defaults use the seeded default from `TerminologySeeder.cs` — `Feature`/`Features`, `Work Item`/`Work Items`, … — never "Epic", "Initiative" or "Story", which name a heading a Jira or Linear reader never sees. A literal work-tracking-system **value** (a filter matching type `Epic` in ADO) is the exception and stays as written.

### Commits & Shared Contracts

- Conventional commits with scopes: `feat(payment): …`, `fix(user): …`, `refactor(order): …`, `test(payment): …`. Refactor commits separate from feature commits.
- Before editing a shared contract (DTO, API payload, cross-cutting interface): grep for usages and extend the relevant test factory/builder first to bound the blast radius.
- DRY = don't repeat *knowledge*, not code. Don't abstract structurally-similar code that represents different business concepts (e.g., `validatePaymentAmount` and `validateTransferAmount` may look identical but evolve independently).
- **Comments are rare, and they are written for a stranger.** Write one only when the *why* cannot be read off the code — then say the why itself, in plain language, in a line or two. The test is: someone opening this file cold, with no other tab open, must get the whole point from the comment alone.
  - **Never cite an internal reference as the explanation.** `A2`, `DT5-1`, `DDD-5`, `AC-5.2`, `SA-2`, `D8` and friends name sections of feature documents that get archived; `ADR-134` names a file nobody has open. A reader six months from now cannot resolve any of them, so a comment built on one explains nothing. Write the reason, not the pointer to it.
  - Never narrate what the code does, restate a decision already in the commit body, or leave a running commentary of alternatives considered.
  - **The one exception is a narrow suppression.** `#pragma warning disable <RULE>` must carry its justification inline, because that is where a reviewer reads it — still in plain language, and an ADO item number (`Bug #5567`) is fine there since it resolves to something a human can actually open.
  - Long rationale belongs in the commit message, where it is versioned, searchable, and attached to the change that needed it.
  - When you touch a file carrying comments that break these rules, fix them as you go; don't open a separate cleanup pass for it.

### EF Migrations

- Use the existing `CreateMigration` PowerShell script to generate EF migrations across all supported database providers — do not invoke `dotnet ef migrations add` directly.

## Running Lighthouse Locally

Start the backend with `Lighthouse.Backend/Start-DevServer.ps1`. It keeps the dev key ring outside the
repository, where `git clean -xfd` and worktrees cannot throw it away — and a lost ring is what leaves
stored credentials unreadable. A bare `dotnet run` is fine too; it keeps its ring in `dev-keys/` inside
the project directory, which those destroy. Never point a key store at `data-protection-keys`: that name
is the legacy store the app carries over and compares against, and a ring there collides with the one the
test suite mints in `keys/`. `/run-dev` (`.claude/commands/run-dev.md`) carries the mechanism and the
recovery paths, including the `FATAL: Two key rings were found` signature a pre-2026-08-31 checkout still
triggers.

## Quality Gates (CI parity)

A change is **not done** until every gate below passes locally. CI enforces them; failing them wastes a CI cycle.

### Frontend (`Lighthouse.Frontend/`)

- `pnpm test` — must pass. This is the baseline prerequisite; nothing else counts as done while tests are red.
- `pnpm build` — must complete with zero errors and zero warnings (runs `tsc -b` then `vite build`).
- Biome — zero errors and zero warnings on `./src`. `pnpm biome check ./src` runs as the `prebuild` hook, so a clean `pnpm build` implies a clean Biome check.

### Backend (`Lighthouse.Backend/`)

- `dotnet build` — must succeed with zero warnings (`TreatWarningsAsErrors` makes any warning a failure, but verify locally before pushing).
- `dotnet test` — all NUnit suites green. **Exclude the live-connector categories unless you are
  actually changing a connector:**

  ```
  dotnet test --filter "TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration"
  ```

  Those tests talk to **real** Jira, Linear, Azure DevOps and ServiceNow instances over the network.
  They do **not** skip when a credential is missing — they `throw NotSupportedException`, so a bare
  `dotnet test` either makes real API calls or reports failures that look like regressions and are
  not. The Linear API key is shared with CI, so repeated local runs can rate-limit (429) the next CI
  build, and only one of the resulting failures ever names the 429. Run the connector categories
  deliberately, when the connector is what you changed.

  Failure signatures that are **environmental, never regressions** — but check which one you have,
  they are not interchangeable:

  - In a git worktree, exactly **2 Licensing** tests fail on the gitignored
    `valid_not_expired_license.json`. Absent from every worktree; copy it from the main checkout.
  - **10 failures in `ReleaseServiceTest`** (the unit class) under a parallel or loaded run are
    contention — re-running that class alone goes green.
  - **8 failures in `LighthouseReleaseServiceIntegrationTest`** (`InstallUpdate_SupportedPlatform_*`,
    "the update process never reached exit within 10 s") are **NOT** contention. They reproduce when
    the class is run alone on unmodified `main`; the fixture spawns a real update process. Do not
    "re-run alone to confirm" this one — it confirms nothing. The filter above excludes them.
  - `GetAllReleases_*` / `GetLatestVersion_*` failing with `Octokit.RateLimitExceededException`
    (`Limit: 60, Remaining: 0`) means repeated full runs exhausted the **unauthenticated** GitHub API
    quota. Another reason not to run the suite unfiltered in a loop.

### SonarQube Cloud (both stacks)

- CI runs SonarQube Cloud analysis on every PR. Do not introduce new issues of any severity (bugs, vulnerabilities, code smells, security hotspots) — the gate fails on new violations even if existing debt remains.
- Common Sonar rule families to watch are tracked in `docs/ci-learnings.md` (see CI Learnings above) — that ledger is the canonical list, not this section.
- If a Sonar rule conflicts with a deliberate decision, suppress narrowly at the call site with a justification, not project-wide.