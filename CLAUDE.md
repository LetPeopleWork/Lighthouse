# Lighthouse — Claude Code Project Instructions

## Development Paradigm

This project follows the **object-oriented** paradigm. Use @nw-software-crafter for implementation.

- Backend: C# .NET 8 ASP.NET Core (OOP, ports-and-adapters / hexagonal architecture)
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

**Before writing or changing ANY code — every agent, and the @nw-software-crafter in DELIVER above all — consult `docs/ci-learnings.md` and pre-apply every rule it lists.** It captures durable rules harvested from prior CI / SonarCloud failures (formatting quirks, Sonar rule keys, recurring foot-guns). The `/clean-ci` command maintains this file.

This is non-negotiable, because **a clean local `dotnet build` / `pnpm build` does NOT mean the CI quality gate will pass.** SonarCloud's `new_violations = 0` gate plus many Roslyn `CAxxxx`, NUnit-analyzer, and Biome rules sit *below build-warning severity* — they surface only in CI, never in a local build. Re-introducing a rule already in the ledger is a process failure that burns a whole CI cycle. So before writing C#/TS, grep the ledger for the recurring rule families (`NUnit4002`, `NUnit2056`, `CA1859`, `CA1861`, `S2325`, `S107`, `S3267`, `S2971`, `S6608`) and apply its rules to any surrounding lines you touch.

## ADO Work-Item Sync

This project's source of truth for "what's in flight" is the Azure DevOps board at `dev.azure.com/letpeoplework` (project `Lighthouse`). Mirror nWave work onto the board as it happens — Epic → child Stories/Bugs — and auto-transition states (`New` → `Active` → `Resolved` → `Closed`/`Done`). Pause before every `git push` so the user can review. The `/ado-sync` slash command encodes the full workflow; apply its rules proactively, not only when invoked. Confirm before any create / remove / `Release Notes` tag — never silently.

## DISCUSS Wave — Cross-Cutting Impact Checklist

Every feature run through `nw-discuss` MUST explicitly address these three impact surfaces. **Even when the answer is "no change needed," record it as an explicit "N/A, because…" — never leave them implicit.** They belong in the requirements/journey output of the DISCUSS wave so DESIGN inherits them. **These extend DoR Item 7 (technical notes / constraints) as a hard gate: a story is not Ready until all three are answered with evidence.**

- **RBAC** — State how the operation interacts with authorization: which roles/permissions gate it, and that it flows through `IRbacAdministrationService` with UI gating derived from `useRbac()` (per Architecture above). If the operation has no authorization effect, say so explicitly and why.
- **Lighthouse-Clients (CLI + MCP)** — Decide whether the change needs a matching update to the CLI and MCP clients. Any new or changed API contract usually means the clients follow; call out the required client change or explain why they're unaffected.
- **Website** — Decide whether the public website needs an update — e.g. a new premium feature should be surfaced/marketed there. Note the required website change or explicitly mark it N/A.

## Coding Conventions

Synthesized from `.github/instructions/*.md`. Rules below extend (don't restate) the paradigm/architecture/test-framework choices above.

### TDD & Tests (`testing.instructions.md`, `workflow.instructions.md`)

- TDD is non-negotiable: every line of production code exists to satisfy a failing test. Verify the test fails for the *right* reason before implementing.
- One RED → GREEN → REFACTOR cycle at a time. Do not queue multiple tests before any implementation. Refactor commit is separate from the feature commit.
- Test behavior through public APIs only — never private methods, internal state, or framework plumbing. Implementation-detail files (validators, helpers) are covered transitively. No 1:1 file mapping required.
- Skip framework-behavior tests (e.g., "renders without crashing" only verifies React/the runtime works — not your code). Every test must assert a business behavior.
- Use factory functions `getMockX(overrides?)` for test data — no shared mutable state via `beforeEach`/`Setup`. Validate factory output against the production schema/type; never redefine schemas inside tests.
- Mock only abstractions you own (e.g., `IPaymentGateway`); never mock third-party libraries directly (`fetch`, axios, DB drivers).
- For high-fan-out DTOs, extend a shared factory/builder rather than repeating object literals. Don't `as`-cast to silence the compiler — fix the factory.

### When to refactor (`workflow.instructions.md`)

After every GREEN, classify and act:

- **CRITICAL — fix immediately**: type-safety violations (`any`, unconstrained `dynamic`), data mutation, security holes, perf regressions affecting users.
- **HIGH — fix this session**: repeated *business* logic (semantic duplication), >30-line functions, magic numbers/strings encoding business rules, missing validation at trust boundaries, naming that obscures intent.
- **NICE — defer**: minor naming, structural simplification, test-readability tweaks.
- **SKIP**: code already clean; structurally similar code that represents different business concepts (don't pre-abstract — it will diverge).

### Immutability (`code-style.instructions.md`)

- Never mutate data. Forbidden: `push/pop/splice/sort/reverse` on arrays, property assignment on objects, `Object.assign`, mutable `List<T>`/`Dictionary<,>` for state.
  - TS: spread, `.map`, `.filter`, `.slice`, destructure-omit.
  - C#: `ImmutableList<T>` / `ImmutableDictionary<,>`, `record` types, `with` expressions (nested `with` for nested updates).
- Prefer LINQ / array methods over imperative accumulation loops. Pure functions where possible.

### Type Safety

- C# (`backend-csharp.instructions.md`): every `.csproj` requires `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<WarningLevel>5</WarningLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`. No unconstrained `dynamic`.
- TS (`frontend-typescript.instructions.md`): strict mode (all `strict*` flags) required. Never `any` — narrow `unknown` with a type guard. Use `type` for data; `interface` only for behavior contracts.
- Schema-first at trust boundaries (API responses, form input, persisted data) using Zod; derive types via `z.infer`. Plain `type` aliases only for purely internal data.

### Code Structure

- Max 2 levels of nesting. Use early returns; never nested `if/else` chains.
- One responsibility per function. Compose small functions over long methods.
- Options object/record when a function takes >3 params or any optional param.
- Errors: return `Result<T, TError>` (record/discriminated union) or use `TryGet` pattern. Don't throw for control flow.
- Async all the way: never `.Result`/`.Wait()`, never `async void` outside event handlers. Pass `CancellationToken` through.
- C#: prefer `switch` expressions, type patterns, and property patterns over chained `if`s.

### Comments — strong default: don't write any

The code, names, and constants do the explaining. **Applies equally to production code and test code.** If a future reader needs a comment to understand a line, the line itself is the problem — fix the name, extract a helper, or use a constant.

**Banned** (in production AND test files):

- Decorative dividers and section banners — `// =====`, `// -----`, ASCII-art frames, `// ── helpers ──`. If a file needs visual sectioning, it's too long — split it.
- Section labels that restate structure — `// Setup`, `// Helpers`, `// Scenario 3: ...`, `// Arrange / Act / Assert`. The describe/test/it title and the function name already do this.
- Per-line Gherkin commentary — `// Given:`, `// When:`, `// Then:`, `// And:`. The assertion text and helper names carry the meaning. A test reading like a story is the goal; narrating the story in comments is not.
- Restating what the next line does — `// log in as the test user` above `await loginAs(testUser)`, `// switch user` above `switchUser(...)`. This is noise.
- Provenance / attribution — `// Added for feature X`, `// Targets mutants in report Y`, `// Per ADR-NNN`, `// Closes #123`. That metadata belongs in the commit message or PR description, never in the file (it rots the moment the code moves).
- Commented-out code, `console.log` / `Console.WriteLine` debug breadcrumbs, `TODO` / `FIXME` / `HACK` / `XXX`. Open an issue instead.

**Allowed (rare, minimal)**:

- A single line explaining a non-obvious **WHY** when removing it would mislead a future reader: a hidden invariant, a workaround for a specific upstream bug with a link to that bug, surprising platform behavior the code has to compensate for.
- JSDoc / XML doc on **public** APIs — one paragraph max, focused on the contract, not the implementation.

If you find yourself wanting to write a comment, first ask: would a better name or a small helper remove the need? Almost always: yes.

**Boy Scout rule for existing comments**: when you touch a file for any reason, delete any banned comments you encounter in the same change. No separate cleanup PR, no "out of scope" excuse — leaving them in place after editing nearby code is a tacit endorsement. The only exception: if removal would balloon the diff and obscure the actual change, leave a one-line note in the commit body and open a follow-up.

### Commits & Shared Contracts (`workflow.instructions.md`)

- Conventional commits with scopes: `feat(payment): …`, `fix(user): …`, `refactor(order): …`, `test(payment): …`. Refactor commits separate from feature commits.
- Before editing a shared contract (DTO, API payload, cross-cutting interface): grep for usages and extend the relevant test factory/builder first to bound the blast radius.
- DRY = don't repeat *knowledge*, not code. Don't abstract structurally-similar code that represents different business concepts (e.g., `validatePaymentAmount` and `validateTransferAmount` may look identical but evolve independently).

### Naming

- C#: `PascalCase` types/methods/properties, `IPascalCase` interfaces, `_camelCase` private fields, `camelCase` locals/parameters, `PascalCase` constants. (`code-style.md` and `backend-csharp.md` conflict on fields/constants — follow standard .NET conventions used here.)
- TS: `PascalCase` components/types, `camelCase` functions/variables, `UPPER_SNAKE_CASE` module-level constants, `kebab-case.ts(x)` filenames.

### EF Migrations (`backend-csharp.instructions.md`)

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
- Common Sonar rules to watch: cognitive complexity, duplicated blocks, unused variables/parameters, empty `catch`, `TODO`/`FIXME` left in code, missing `await` on async calls, `any` in TS, mutable public fields in C#. The conventions above (no comments-as-TODOs, options objects to cap params, early returns, immutability) already prevent most of them — keep it that way.
- If a Sonar rule conflicts with a deliberate decision, suppress narrowly at the call site with a justification, not project-wide.
