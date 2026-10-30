# CI Learnings

Rules for the CI and SonarCloud failures that no tool can catch for us.

**Automate first.** When CI or Sonar finds something, the fix is a check that catches it next time,
before a person has to remember it. That means a Biome rule or GritQL plugin (frontend), an
`.editorconfig` severity, a `BannedSymbols.txt` entry or an ArchUnit/source-scan test (backend), a
`LEDGER-CHECKS` pattern below (any line a regex can see), or a hook. Add a rule to this file only when
none of those can do the job, and say why. A rule that a tool later takes over is deleted from here.

Format: `- **<Rule id or short name>**: <cause>. Fix: <how>. Not automated because: <reason>.`
No dates and no incident stories. Those go in the commit message.

What already runs, so you know what you can leave to it:

- **Frontend**: `pnpm build` runs `biome check --write ./src` (prebuild), then `tsc -b`.
  `Lighthouse.Frontend/biome.json` turns on, at `error`, every Biome rule that matches a rule in the
  Sonar TS profile, and its GritQL plugins in `biome-plugins/` cover Sonar rules Biome lacks (S7761,
  S7770, S7727 for reducers, S4624, S3358 in templates, S6754 setter prefix, MUI `Select` labels, local
  day keys in production code). Vitest pins the zone and checks `NULLABLE_FIELDS` and the E2E test ids.
  `Lighthouse.EndToEndTests/` has its own `biome.json` and plugins (`pnpm run lint`, `pnpm run
  typecheck`, run in CI). Biome does **not** run on pre-push: it belongs to the refactor gate.
- **Backend**: `Directory.Build.props` adds SonarAnalyzer.CSharp to every non-migration project, with
  `TreatWarningsAsErrors`. Sonar-way rules (S2139, S2325, S3218, S4136, S6667, S6964, S3236, S6618,
  S8969, …) already break `dotnet build`. `.editorconfig` raises the imported Roslyn and NUnit rules that
  have failed the gate (CA1806, CA1825, CA1834, CA1859, CA1861, CA1865, CA1869, CA2012, CA2016, SYSLIB1045,
  ASP0015, CS9236, NUnit1028, NUnit2010, NUnit2045, NUnit2046, NUnit2056, NUnit4002) to `warning`, and
  turns on S107, S3776 and S1192, which the NuGet analyzer ships disabled (off again for tests, as on
  SonarCloud). `BannedSymbols.txt` bans the ambient clock and `AsSplitQuery` (RS0030). Tests guard
  pending EF model changes, required columns without a default, the settings DTO round trip, demo-data
  `ReferenceId` uniqueness, and `StartFreshAsync` outside its allowlist. `Architecture/*Test.cs` also
  covers the parallelisation allowlist, calendar-day anchoring, zoned timestamp parsing and Jira route
  literals.
- **Hooks**: `.githooks/pre-commit` runs gitleaks. `.githooks/pre-push` runs the SonarQube CLI
  secret scan, which skips silently when `sonar` is not installed. The patterns below run from
  `.githooks/pre-commit` for every committer and from the Claude Code `PreToolUse` hook on `git commit`.

## Machine-checked patterns

`Scripts/ledger_check.py` blocks a commit whose staged **added** lines match a pattern. Format:
`<rule-key> ::: <comma-separated extensions> ::: <Python regex> ::: <fix hint>`. Remove a line once
Biome, Roslyn or a banned symbol enforces the rule. Both hooks resolve the checkout through git, so they
work in worktrees. Run them by hand with `python3 Scripts/ledger_check.py --staged` (exit 2 means a
staged line matches). A worktree's git hooks are the main checkout's (`core.hooksPath`), so a change to
`.githooks/` only takes effect there once it is on `main`.

<!-- LEDGER-CHECKS:START -->
typescript:S4782 ::: ts,tsx ::: \w\?\s*:\s*[^;=(){}]*\|\s*undefined\b ::: S4782 — an optional member (`name?:`) already includes `undefined`. Drop the `| undefined`, or drop the `?` if callers must pass it.
vitest:storage-prototype-spy ::: ts,tsx ::: spyOn\(\s*Storage\.prototype ::: `vi.spyOn(Storage.prototype, …)` intercepts nothing in this jsdom setup, so the test passes against code without the guard. Spy on the storage object itself (`localStorage`), and restore it by hand in `afterEach`.
mui-x:series-class ::: ts,tsx ::: MuiLineElement-series- ::: MUI-X 9 emits no per-series class. Target `& .MuiLineChart-line[data-series="<id>"]`, as `DeliveryBurnupChart.tsx` does.
pnpm:settings-in-package-json ::: json ::: ^\s*"pnpm"\s*:\s*\{ ::: pnpm settings (`overrides`, `onlyBuiltDependencies`, …) belong in `pnpm-workspace.yaml`. In `package.json` they break CI's `--frozen-lockfile` install.
<!-- LEDGER-CHECKS:END -->

## Frontend

### Sonar TS rules with no Biome rule (or a Biome rule that disagrees with Sonar)

- **S3776 (cognitive complexity, including render bodies)**: Sonar caps a function at 15. A third
  branch or mode added to a function that is already near the limit tips it over, and so does a
  conditional nested in JSX inside a `&&`. Fix: extract the nested JSX subtree or the per-mode decision
  into a named component or helper before you add the branch. To measure it, run
  `pnpm exec biome lint --only=complexity/noExcessiveCognitiveComplexity <file>`, which reads close to
  Sonar but usually a little lower. Not automated because: Biome's rule counts nested callbacks
  differently. At `error` it flags 37 production functions that Sonar passes.
- **S2004 (functions nested more than 4 deep)**: inline `.find()`/`.map()` callbacks inside branches
  of an async handler or `useEffect` count as nesting. Fix: give each find-then-act step its own named
  helper. Not automated because: Biome has no equivalent rule.
- **S6767 (prop declared, never used)**: removing a control leaves its prop on the interface, then a
  caller local, then the value that fed it. Fix: delete the prop, and every call site and local that
  fed it, in the same change. Not automated because: neither Biome nor `tsc` reports unused props.
- **S6759 (component props must be read-only)**: any function that returns JSX, private helpers
  included. Fix: `Readonly<XProps>` or `Readonly<{ … }>` (`React.FC<Props>` also passes). Not
  automated because: Biome has no rule for it. A GritQL plugin could cover the inline-type case; it is
  listed as a low-priority proposal.
- **S4144 (two functions with identical bodies)**: this happens when a new mock-service factory is
  copied from a sibling, or when DISTILL scaffolds two service methods that throw the same `SCAFFOLD:`
  message. Fix: extract a shared helper and delegate to it; give every scaffold its own message (name
  what that method is for). Not automated because: Biome cannot compare function bodies, and the
  installed `typescript` 7 is the native build with no JS compiler API for a source-scan test.
- **S7727 for single-argument callbacks (`.map(ownFn)`)**: pass built-in unary functions directly
  (`.map(Number)`, enforced by the `iterator-callbacks` plugin) and wrap our own reducers (enforced by
  `wrap-own-reducer`). Wrap an own single-argument callback too when Sonar asks. Not automated because:
  about 160 existing `.map(ownFn)` calls pass the gate, so a plugin for them would flag code Sonar accepts.
- **S8786 (super-linear regex)**: a presence-check regex that opens with an unanchored `\d+` ahead of
  optional parts or an alternation. Fix: start at the first literal word, or anchor with `^`. Use
  `(?:…)` when nothing reads the capture. Not automated because: Biome does not analyse regex
  complexity.
- **S6754 (`useState` pair naming)**: the pair must be `[thing, setThing]`, even when a hook exports
  a wrapped setter. Give the wrapper its own verb (`chooseBackground`). The `use-state-pair-naming`
  plugin checks the `set` prefix. Not automated because: GritQL cannot capitalise, so it cannot check
  that the setter matches the state's name.
- **Promise handling that Biome cannot see (S9383, S6544)**: Biome does not recognise react-router's
  `navigate(...)` as a promise, and it misses a promise-returning function assigned to a `void`-typed
  slot. Fix: send every `navigate(...)` through `reportFailedNavigation` (or await it), and declare
  side-effect functions to return `void`. Not automated because: Biome's type inference is shallower
  than Sonar's.

### Tests (Vitest, React Testing Library)

- **Pin copy against a literal**: a test that compares a rendered value with the constant it came from
  passes when the constant is blanked. Fix: assert user-visible strings against the literal at least
  once, and anchor RTL name matchers (`toHaveAccessibleName`), because `name: /x/i` matches substrings.
  Not automated because: telling a meaningful assertion from a self-satisfying one takes judgement.
  Mutation testing catches it only for module constants; StrykerJS does not mutate JSX text.
- **Invariance assertions need an anchor**: `expect(f(a)).toBe(f(b))` passes when `f` returns a
  constant. Assert that per-series styles **differ**, not that they match. Fix: pin one side to a
  literal and compare a `Set` against the expected count. Not automated because: this is about what a
  test proves, which no linter can see.
- **A busy button outliving the test**: a test that ends while an `ActionButton` is still awaiting
  fails the run with `Errors 1 error` and `window is not defined` under an all-green count. Fix: wait
  for `aria-busy` to clear (in `afterEach` when several tests press it). Not automated because: the
  failure is a timing race that only shows up after teardown.
- **Fresh values in a `renderHook` callback or an effect dependency list**: building an entity that
  carries `new Date()` inside the callback, or putting an inline `new Date()`/`[]`/`{}` into
  `useEffect`/`useMemo` dependencies, loops forever. Under `--coverage` it kills the worker, and live
  it causes React #185. Fix: build it once outside the callback, or memoise it. Before pushing, run
  touched test files with `CI=true pnpm run sonarreport`. Not automated because:
  `useExhaustiveDependencies` checks which dependencies are listed, not whether they are stable.
- **`Test timed out in 5000ms` in files you did not touch**: added tests shift the load on marginal
  files. Fix: prove the cause by excluding your new files, then give the marginal tests an explicit
  timeout. Do not decide from their solo runtime. Not automated because: contention depends on the
  whole run, not on any single file.
- **Use the cheap query unless the role is what you are testing**: in DataGrid tests, `*ByRole` costs
  1.25–2× the time. Use it only where the role is the assertion, and never inside a per-row loop. MUI's
  `Switch` has `role="switch"`, and MUI-X hides chart SVG from the accessibility tree. Not automated
  because: whether a query is the assertion or only a way to find the element is intent.
- **Data contracts at the Zod boundary**: a backend `T?` arrives as JSON `null`, not absent.
  `.optional()` rejects it, and `z.coerce.date()` turns `null` into 1970. Fix: use `.nullable()` or
  `.nullish()` (the shared `optionalString`/`optionalDate` helpers), after checking the C# DTO's
  nullability. Not automated because: the nullability lives in C#, and nothing compares the two sides.
- **A query key carries what makes its entity change**: when an effect that depended on an entity
  object becomes a keyed TanStack query, put the owner's `lastUpdated` in the key, not just its id.
  Otherwise a server-side refresh never reaches an open view. A spec that is green on SQLite and red on
  Postgres is a timing hint, not a flake. Not automated because: what changes an entity is domain
  knowledge.
- **Dates crossing a boundary in tests use `src/utils/date/localDate.ts`** too: plugins ban
  `toISOString().split("T")` and `new Date("YYYY-MM-DD")` in production code only. Not automated
  because: test files use these forms on purpose in hundreds of places, so the ban would be noise there.

### E2E (Playwright page objects)

`Lighthouse.EndToEndTests/` runs Biome and `tsc` (`pnpm run lint`, `pnpm run typecheck`), and its
plugins ban `this.container.page().getBy…`, a bare `getByRole("tooltip")`, `getByTestId("…Icon")`, a
page-wide row `filter({ hasText })` and `expect(page.url())`. A page-wide lookup that is right on purpose
carries a `// biome-ignore lint/plugin: <reason>`.

- **Scope every locator to the component that owns it**: a page-wide `getByTestId`, `getByLabel`,
  `getByRole("tooltip")`, a row matched by `filter({ hasText })` (rows carry dependency names too), or
  `this.container.page().getBy…` in a per-row page object all work while there is one match. They fail
  with a strict-mode error the day a second instance renders, or a section renders sooner. Fix: scope to
  the owning card, heading, row or name cell (`[data-field="name"]`), and pass `{ exact: true }` for
  short or prefix-colliding names. Not automated because: whether a locator is unique depends on the
  pages and data at run time; the plugins catch only the fixed shapes.
- **Renaming copy breaks a page object in another project**: an accessible name or label prefix that a
  page object keys on fails silently or vacuously (`toHaveCount(0)` on a dead selector passes). The
  `@screenshot` suite is excluded from the normal E2E run. Fix: in the same edit,
  `grep -rn "<old text>" Lighthouse.EndToEndTests/`, and grep for any page object that parses a value
  whose display format you change (a `~`/`>` prefix, capping, units). Test ids are checked by
  `src/tests/e2eTestIds.test.ts`. Not automated because: copy is matched by text and regex in too many
  shapes to resolve statically.
- **A page object must not map "not found" to a real value**: a getter returning `0`, `""` or `"none"`
  for "not rendered yet" makes one-sided assertions pass on the loading sample. MUI also strips
  `data-testid` from icons in production builds. Fix: wait for something that only the target renders,
  bound measurements on both sides, return a distinct `"unknown"`, and never key on an icon test id.
  Not automated because: it is about what the assertion means (the icon test-id shape is banned by a
  plugin).
- **Wait for the settled state, not a snapshot**: after a redirect, `waitForURL` the final pattern, or
  use `expect(page).toHaveURL`. A `goTo*` waits for content only the target view renders, and a baseline
  is captured only after its data has loaded (`expect.poll`). Postgres runs are slower and expose every
  one-shot read. A widget outside Flow Overview waits on its own request (`applyAndWaitFor`). Not
  automated because: what counts as "settled" differs from view to view.
- **A spec sets its own preconditions, and demo-data defaults are global**: a spec that depends on the
  absence of a setting must set it absent itself. Changing a `DemoDataFactory` default, or demo
  throughput or types, changes every E2E that loads demo data. Never pin a spec to a live work-item id on the real board. Fix: grep
  `Lighthouse.EndToEndTests/` for the setting before pushing, and run the specs that depend on the data
  live. Not automated because: which spec depends on which piece of demo data is not written down
  anywhere a tool can read (`ReferenceId` uniqueness has a test).
- **Run a new spec or page object against a live Lighthouse before committing it**: `pnpm build`
  only proves it type-checks. When a new E2E test is green on its first run, break its expectation once
  to prove it can fail. A modal that opens by itself is suppressed in `playwright.config.ts`, not in
  `LighthouseFixture.ts`, because seven specs bypass the fixture. Not automated because: only running
  it proves a locator resolves.
- **Optional features flipped in a spec are reset in `afterEach`**: a passed-on-retry test in the
  verify job summary is a race, not luck. Not automated because: it is a review habit.

## Backend

### Sonar rules no local analyzer reproduces

- **S9129 / S8733 and Sonar rule rollouts**: SonarCloud ships new rules that apply retroactively to the
  new-code period. They redden the gate on files nobody touched, and S9129 and S8733 do not fire in a
  local build even when raised to `warning` (probed against SonarAnalyzer 10.35). Fix: check the rule's
  `createdAt` (`curl` the public `/api/rules/show` API, because the `sonar` CLI is not installed), fix
  the hits anyway, and for S8733 use a narrow pragma (`.AsSplitQuery()` is a banned symbol). Not automated because:
  the rule only exists on the server.
- **Adopting a Sonar or analyzer version bump**: a Dependabot `SonarAnalyzer.CSharp` bump can add
  error-severity rules (S8969/S8970) that redden `Package App` but not `Verify Backend`. Fix: apply the
  bump locally, run `dotnet build`, fix or promote what it surfaces, then let the bump through. Not
  automated because: the new rules do not exist until someone takes the bump.
- **S6639 (memory allocation from an unbounded request list)**: an endpoint that takes a list from the
  request body (`reportIds`, owner ids) and hands it to a service that allocates or loops per item fails
  the security rating. Fix: refuse the request in the controller when the list is missing, empty or longer
  than the run's ceiling, before any service sees it, as bulk report creation does with
  `IReportBulkCommands.MostOwnersPerRun`. That check alone does not satisfy the analysis: never size a
  collection from the request either (`new List<T>(ids.Count)` is still flagged behind the check), so leave
  the capacity out. Not automated because: it is SonarCloud's taint analysis, which runs only on the server;
  SonarAnalyzer.CSharp in the local build has no security rules.

### Design rules a compiler cannot see

- **S6964 on a new settings or request DTO property**: making it `[JsonRequired]` silences the rule
  and breaks every payload written before it (E2E helpers, the clients repo, users' scripts). Fix: make
  the new property nullable and coerce it at the read site. `[JsonRequired]` is only for properties that
  have been required since the type shipped, and then every sender must send it. Not automated because:
  both forms satisfy the analyzer, so choosing between them takes judgement.
- **A new settings field goes through the whole path**: `SettingsOwnerDtoBase` → the `Sync*With*Settings`
  helper (checked by `SettingsOwnerDtoRoundTripTest`) → any hand-built `JsonObject` PUT payload in the
  tests → any pinned payload-shape test (`grep -rn "PropertyNames" Lighthouse.Backend.Tests`). Not
  automated because: the hand-built payloads are test data, and which of them must carry the field is a
  judgement.
- **`[FromQuery]` / `[FromRoute]` parameters are required by default**: under nullable reference
  types, model binding returns 400 before the action runs. Fix: declare the parameter nullable when the
  action handles its absence (OAuth callbacks, for example). Not automated because: whether a parameter
  may be absent is a decision about the API contract.
- **Widening a cross-cutting interface**: implementations that spell the interface fully qualified,
  and test doubles in the test project, are easy to miss. Fix: grep for both the short and the
  fully-qualified name, in production and test projects. Not automated because: the compiler does catch
  it (CS0535), but only one build later, and the rule is there to save that build.
- **An awaited `Save()` in async handlers**: a domain-event handler that calls a repository `Save()`
  from a `void` helper and returns `Task.CompletedTask` desyncs Npgsql. It shows up as a Postgres-only
  flake. Fix: `await` it, in a genuinely `async Task` method. Not automated because: S3168 and CS4014 catch
  `async void` and an un-awaited call inside an async method, but nothing flags a `Save()` dropped inside a
  plain `void` method.
- **A queue that dedups by key must coalesce, not drop**: a trigger raised while the same key is in
  flight carries intent the running job has not read. A status handover must never pass through a
  terminal status. Fix: park the trigger and run once more on completion, and decide the follow-up
  before marking the key terminal. Not automated because: it is a concurrency design invariant, covered
  by behaviour tests only once someone writes them.
- **Rebuilding state from a whole-table read is unsafe under concurrency**: `Clear…()` followed by a
  re-add from a repository query loses rows when two kinds of refresh overlap. Only the demo-data E2E
  sees it. Fix: find every rebuild pass before allowing concurrent refreshes. Not automated because: it
  is a design review question.
- **A caching metrics read inside a recording handler warms the shared UI cache**: refresh events can
  fire on partly settled data. Fix: invalidate the owner's metrics cache after recording. Not
  automated because: it depends on which getter is cache-backed.
- **Runtime-state directories and `.gitignore`**: moving or creating a directory where key material is
  written (Data Protection keys, the OAuth state secret) can drop it into an untracked, unignored path,
  and gitleaks does not recognise XML key files. Fix: add the path to `.gitignore` in the same commit,
  and verify with `git check-ignore -v <a real file in it>`. Not automated because: the path is known
  only at run time.
- **Key-store files shared between boots**: `File.Exists` followed by a write races when two hosts
  share a directory, as the whole integration suite does. Fix: stage the file under a unique name, move
  it into place without overwriting, and let the loser read the winner's file. Not automated because:
  it is a concurrency design rule.
- **Secrets from CI are empty, not absent**: an unset `${{ secrets.X }}` arrives as an empty string, so
  `??` never falls back. Fix: test with `string.IsNullOrWhiteSpace`. Not automated because: the
  difference only exists at the CI boundary.
- **Jira calls name the Data Center route as well as the Cloud one**: `JiraRouteLiteralsTest` forces
  every route through `JiraRestRoutes`, but cannot tell whether a route has a Data Center answer. Fix:
  add the Data Center route and its response shape next to the Cloud one. Not automated because: a
  Cloud-shaped fixture passes either way.
- **A shared validate endpoint is not gated on one workflow's permission**: a create-only RBAC guard
  on a validate endpoint returns 403 to scoped admins who are editing. Fix: leave it unguarded when it
  has no side effects and redacts secrets, or work out the gate from the request's intent. Not automated
  because: it is an RBAC design decision.

### Tests (NUnit, integration, live connectors)

- **Negative assertions on a shared mock cannot fail**: NUnit reuses one fixture instance, so a mock
  built in the constructor keeps the calls earlier tests made, and `Times.Never` passes vacuously. Fix:
  build or reset the mock in `[SetUp]`. Not automated because: telling a shared mock from a per-test one
  requires following the fixture's lifecycle (a source-scan guard is possible, but would raise false
  alarms).
- **Fire-and-forget work under test**: asserting on a mock that a `Task.Run` calls races the thread
  pool, and `Task.Delay` only widens the window. Fix: signal a `TaskCompletionSource` from the mock's
  `.Callback` and await it with a timeout. Not automated because: it is a race in how the test is
  designed.
- **Blocking waits park the thread pool**: a `Barrier`, latch, `.Wait()` or `.Result` on `Task.Run`
  threads slows unrelated tests to 40–60 s. Fix: give each participant its own thread
  (`LongRunning`/`new Thread`) and bound the wait. Check by running beside another class with
  `DOTNET_PROCESSOR_COUNT=4`. Not automated because: the effect only shows under load.
- **A polling watcher re-checks the forbidden condition in the same iteration** as the observation,
  not only in the loop guard. Two reads with a scheduling point between them see two different moments.
  Not automated because: this is about the race semantics of the test.
- **Timing assertions**: never assert wall-clock time in CI against a number measured locally. The
  agent is 10–20× slower, so size a timing check to catch a hang. A test whose duration sits near a poll
  deadline is failing slowly. Not automated because: whether a number is a budget or a coincidence takes
  reading.
- **A Monte Carlo test on a fixed dataset uses pinned randomness**: `SeededRandomNumberService` plus
  `DrawsFromAPinnedStartingNumber`. Never re-run a ±1 `Is.InRange` miss or tighten the range to chase a
  seed. Not automated because: whether a test is meant to be a golden baseline is intent.
- **Host lifecycles in `WebApplicationFactory` tests**: resolve every service you will assert on before
  `StopApplication()`. A host shared by a fixture may hold per-test state only in objects the fixture
  resets in `[SetUp]`, never in a DI registration that closes over a per-test field. Not automated
  because: what counts as per-test state differs from fixture to fixture.
- **FK-dependent behaviour runs on SQLite**: EF InMemory does not enforce foreign keys or cascades.
  Fix: a test asserting an FK violation or a cascade must not use `UseInMemoryDatabase`. Not automated
  because: telling which tests depend on FKs takes understanding what they assert.
- **ArchUnit dependency edges inside `async` bodies exist in Debug only**: assert against fields,
  constants or signatures. Verify Release-sensitive tests with `-c Release`. Not automated because: the
  edge exists or not depending on how the code is compiled.
- **Connector test fixtures use connection ids no other fixture uses**: the static `ClientCache` and
  field-key cache are keyed by connection id, and `Id=0` collides when fixtures run in parallel. Not
  automated because: the cache keys are runtime state.
- **Before narrowing a `catch` in a connector, run that connector's live category**
  (`TestCategory=AdoIntegration`, …): the swallow may be pinned only there. Read a Stryker `NoCoverage`
  with the run's category filter in mind. Not automated because: the live categories are excluded from
  the default run on purpose (CLAUDE.md).
- **Live-connector writes use scratch items and polled reads**: a test that writes creates its own
  item and deletes it in teardown. Read-backs through JQL search are eventually consistent, so poll them
  and widen the budget rather than re-running. Not automated because: it is about external systems.
- **Redis `When.Exists` is legal only on `StringSet`**: for a hash-set-if-exists, use a
  `HEXISTS`-guarded script. Run `Integration/Containers` locally whenever you touch it. Not automated
  because: the compiler cannot see which overloads are legal.
- **`pg_terminate_backend` is asynchronous**: wait for the backend to leave `pg_stat_activity` before
  asserting that a lock was reclaimed. Not automated because: it is a fact about the server's
  behaviour.
- **A Kind == Utc assertion does not prove the day is right**: the converter restores `Kind` after
  shifting. Read the row back through a fresh context and assert the **day**. Not automated because:
  it is about what the assertion proves.
- **A docs-enforcement test scans the whole `releasenotes.md`**, never the `vNext` section, which
  `/release` renames. Not automated because: it is a test-design choice.

### EF migrations

- **Generate migrations with `Create-Migration.ps1`, never a bare `dotnet ef`**: the script builds the
  migrations projects and the solution first and checks port 5432. After generating, rebuild the
  migrations projects and then the solution with `--no-incremental` before verifying, or the backend
  still loads the old snapshot. Not automated because: a hand-run `dotnet ef` cannot be intercepted.
- **No non-default CLR initialiser on a persisted property** (`= true`, `= string.Empty` on a
  `string?`): EF reads it as the sentinel and the model differ never settles. Fix: leave the property
  bare and put defaults on the DTO or in the frontend. Not automated because: no analyzer knows EF's
  sentinel rules; `NoPendingModelChangesTest` catches the symptom.

### Mutation testing (Stryker)

- **Check what ran before you trust a score**: StrykerJS can exit 0 having tested nothing (a missing
  `plugins`, an empty `include:` match, a missing `vitest/config`). Stryker.NET ignores `file.cs{a..b}`
  line ranges and mutes everything. A `test-case-filter` that matches no fixture reports every mutant
  alive. Fix: read the per-file table in `mutation-report.json`, run vitest standalone first, write
  `include:` from `ls`, copy the frontend config into `Lighthouse.Frontend/`, and mutate whole files on
  .NET. Not automated because: these are run-time configurations that are gitignored and rebuilt for
  every slice.
- **Judge survivors rather than count them**: a `ThrowIfNull` right before a LINQ call over the same
  argument is an equivalent mutant, so record it as equivalent on the first run. A hand-applied probe
  must keep every parameter read (`_ = dep;` plus the wrong behaviour), because deleting a call trips
  CS9113, a bare block S1199, `if (false)` CS0162 and a discarded parameter S1172, all build errors
  here. Not automated because: deciding equivalence takes judgement.
- **Stub-first RED does not compile here**: S2325 and S4487 reject a stub that ignores instance state.
  Give the stub an instance-derived exception message (`throw new NotImplementedException($"{dep} …")`).
  Not automated because: the analyzers that force it are deliberate. This is the way through, not a gap.

## Pipeline & infra

### Reading a red run

- **Find the first failing step before you diagnose**:
  `gh api repos/<o>/<r>/actions/jobs/<id> --jq '.steps[] | select(.conclusion=="failure")'`. A failure
  in `Set up job`, a 5xx from `deploy-pages`, a keyserver failure in the Sonar scan action, a
  quay.io/mcr/ports.ubuntu.com pull error, a `startup_failure`, or a Detect Changes 502 is
  infrastructure. Re-run, check githubstatus.com, and change nothing. Not automated because: telling
  them apart needs the log.
- **A red `sonar-gates` with `Quality Gate has PASSED` twice in the log** failed in the audit step.
  A GHSA id there is a real transitive advisory: fix it with an `overrides:` entry in
  `pnpm-workspace.yaml` plus the lockfile, in one commit. A network error is infrastructure. `a newer
  report has already been processed` means a concurrent merge won, and a 403 from the gate action is a
  transient API failure. Not automated because: the job reports all of these as the same red.
- **A red backend job skips Sonar**: the next green push inherits every violation nobody analysed, on
  files it never touched. Sonar analyses every push to `main` and judges a batch of commits as one
  diff. Fix: clean up the inherited issues (`git log` the named files), and never read a failing run's
  Sonar result as clean. Not automated because: it is how the pipeline is ordered.
- **One unrelated backend test red among thousands is a flake**: SQLite `disk I/O error` from
  `EnsureCreated()`, a rotating `Integration/Containers` failure, or a test that took several times its
  solo time on a loaded machine. Re-run it, and never quarantine it. The same test failing twice, or a
  deterministic failure such as a pinned payload-shape test, is real. Read the test **name** before
  accepting "flake". Not automated because: the call depends on history across runs.
- **Rate limits and shared quotas**: the live connectors and the GitHub release tests share
  quotas with CI, the Linear key in particular. Look for `429`/`Retry-After`/`RateLimitExceeded` before
  suspecting the change, and do not explore an API by hand within the hour before a push. Not automated
  because: the quota is external.
- **`connector_shared=true` does not mean your change touched a shared path**: a stale run listing can
  move the change-detection base back by weeks. Check `Found last successful run:` in Detect Changes.
  Adding a line to `Program.cs`, a csproj, the `.sln` or a shared interface really does run every live
  connector suite (about 258 more tests, quotas spent, and Sonar skipped if one goes red). Not automated
  because: the maintainer keeps Detect Changes as it is.
- **ServiceNow integration failures: `curl` the instance root first**. A 502 from `snow_adc` means the
  instance was reclaimed and must be rebuilt (`Scripts/DemoEnv/README.md`). Its 401s are byte-identical
  whatever the cause. Not automated because: it is an external environment.
- **`e2etests` runs the prebuilt Docker image (the last release)**: only `verifysqlite` and
  `verifypostgres` build from source, so a green `e2etests` says nothing about your change. Not
  automated because: it is how the pipeline is built.

### Dependencies, analysis scope and packaging

- **NU1605 naming projects the change never touched**: a direct pin elsewhere sits below a new
  transitive floor. Grep for a direct `PackageReference` to the named package and raise the pin. Never
  re-run, and never pin the consumer down. Not automated because: the conflict only exists once the
  bump arrives.
- **CI's pnpm and `packageManager` must agree**: reproduce `ERR_PNPM_LOCKFILE_CONFIG_MISMATCH` with
  the workflow's exact pnpm (in a `node:24` container), because other versions accept both lockfile
  shapes. Not automated because: the mismatch is between a workflow file and `package.json`, and the
  maintainer chose not to add a cross-file check.
- **A new top-level directory of non-C# scripts goes into `sonar.exclusions`** (`ci_backend.yml`).
  The backend scanner indexes everything that is not excluded, so a Python or shell helper can fail the
  backend gate. `.ps1` files under `Lighthouse.Backend/` are still analysed: use `Write-Output`,
  `$null -eq $x`, no trailing whitespace, and invoke hyphenated binaries via `& $name`. Not automated
  because: no local PowerShell analyzer matches Sonar's.
- **A new real-API test suite is path-scoped**: add `classify_x` in `path-classifier.sh`,
  `[Category("XIntegration")]`, the output in `ci_changes.yml` and the input in `ci_backend.yml`, and
  mirror it in `dev-test.{sh,ps1}`. Not automated because: it is wiring across workflow files.
- **Test-time budget**: a test file (frontend) or test class (backend) over its budget is a failure to
  fix, although `check_budget.py --warn-only` only warns today. Not automated because: the
  maintainer keeps the budget warn-only.
- **Installing a just-published package by exact version**: poll the registry first, never a fixed
  sleep. Not automated because: it is a race with an external registry.

### Shell, Helm and local tooling

- **Shell scripts under `set -o pipefail`**: never pipe a variable or a long output into `grep -q`
  or `head`. Use a here-string, or capture first. Never chain `[[ ]] && [[ ]] || …`. CI's shellcheck
  is 0.9.0, so check with `koalaman/shellcheck:v0.9.0`, not `:stable`. Not automated because: the
  maintainer chose not to add shellcheck to pre-commit.
- **Chart tests hold appVersion fixed** across the upgrade they test. Bump appVersion locally once
  before calling a chart test done. The workload never binds a port below 1024. `chart/README.md` is
  generated by helm-docs. A job that pushes to `main` after waiting at an environment gate rebases
  first. Not automated because: these are about how the chart tests are designed and how the release
  flow is built.
- **Local verification traps**: never chain `dotnet test --no-build` after a build that may have
  failed. Assert `0 Error(s)` first. A concurrent `dotnet build` rewrites `bin/` under a running suite.
  A stale `node_modules` in a worktree runs an old Biome (compare `pnpm biome --version` with the pin).
  Never pipe a gate's output through `tail` or `head` when its verdict is what you are checking. Not
  automated because: these are about how commands are run locally.
- **gitleaks findings in fixtures**: compute a 32-byte key rather than writing it as base64, and use
  repeating-digit GUIDs. Never allowlist the path in `.gitleaks.toml`. Not automated because: the
  hook catches the problem, but the right fix (not the allowlist the hook suggests) needs a person to
  choose it.
