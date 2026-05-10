# Lighthouse — Claude Code Project Instructions

## Development Paradigm

This project follows the **object-oriented** paradigm. Use @nw-software-crafter for implementation.

- Backend: C# .NET 8 ASP.NET Core (OOP, ports-and-adapters / hexagonal architecture)
- Frontend: React 18 + TypeScript (functional-leaning hooks, but overall OOP project)
- E2E Tests: Playwright with Page Object Model pattern
- Test Framework (backend): xUnit + NSubstitute
- Test Framework (frontend): Vitest + React Testing Library
- Test Framework (E2E): Playwright

## Mutation Testing Strategy

`per-feature` — Run Stryker.NET for backend C# mutations after each feature delivery. Run Stryker for TypeScript/React frontend mutations. Minimum kill rate: 80%.

## Architecture

Ports-and-adapters (hexagonal) on the backend. All RBAC business logic flows through `IRbacAdministrationService`. No component may fetch `/api/latest/authorization/my-summary` directly — all UI gating derives from the `useRbac()` hook.

See `docs/product/architecture/brief.md` for full architecture documentation.

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
- No code comments — names and constants do the explaining. JSDoc / XML doc on public APIs is OK.
- Errors: return `Result<T, TError>` (record/discriminated union) or use `TryGet` pattern. Don't throw for control flow.
- Async all the way: never `.Result`/`.Wait()`, never `async void` outside event handlers. Pass `CancellationToken` through.
- C#: prefer `switch` expressions, type patterns, and property patterns over chained `if`s.

### Commits & Shared Contracts (`workflow.instructions.md`)

- Conventional commits with scopes: `feat(payment): …`, `fix(user): …`, `refactor(order): …`, `test(payment): …`. Refactor commits separate from feature commits.
- No commented-out code, no `Console.WriteLine`/`console.log` debug, no TODO comments (open an issue).
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
