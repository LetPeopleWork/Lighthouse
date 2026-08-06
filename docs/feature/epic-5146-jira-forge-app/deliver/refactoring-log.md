# Refactoring log — Epic 5146 slice 01

DELIVER Phase 3, `/nw-refactor`, RPP L1–L6 over `git diff 3fe7bd421..HEAD`.
Behaviour-preserving only. Baseline before any edit: `dotnet build` 0 warnings /
0 errors, `dotnet test` **4493 passed / 0 failed / 0 skipped** (Docker up, so the
`requires-docker` Postgres fixtures are in that count).

## Transformations

| # | File | RPP | Smell removed | Transformation | Module gate |
|---|---|---|---|---|---|
| T1 | `Services/Implementation/Repositories/EmbedSessionTokenRepository.cs` | L4 — missing abstraction | `RedeemedAt == null && RevokedAt == null` spelled out in `TryMarkRedeemedAsync` and `RevokeOutstandingForApiKeyAsync`; one concept, two copies | Extract query helper `Outstanding()`; both call sites compose their own extra clause onto it. Still one conditional `ExecuteUpdateAsync`, identical composed SQL | M1: 0 warnings, 4493/0 |
| T2 | `Configuration/EmbedConfiguration.cs`, `Services/Implementation/Auth/EmbedSessionTokenService.cs` | L4 — duplicated knowledge | The service held private `DefaultTokenLifetimeSeconds = 60` / `DefaultHandshakeOutcomeLifetimeSeconds = 300` duplicating the config class's property initialisers — two copies of a number that must move together | Config class names the three defaults as public consts and initialises its own properties from them; the service's non-positive fallback reads them from there | M2: 0 warnings, 4493/0 |
| T3 | `Services/Implementation/Auth/EmbedSessionTokenService.cs` | L1 — comment noise | 3-line comment on `RecordHandshakeGrantAsync` restating the constraint in prose | Trimmed to the two lines that carry the *why*, naming `CK_EmbedSessionTokens_GrantOrRefusal` and D68 | M2: 0 warnings, 4493/0 |
| T4 | `API/EmbedEntryController.cs` | L1 — dead code | `Refuse()` wrote `Response.StatusCode = 401` and then returned a `ContentResult` carrying the same 401, which `ContentResultExecutor` assigns anyway | Dropped the redundant write; the method becomes `static`, matching `TerminalPage` on the sign-in hop | M3: 0 warnings, 4493/0 |
| T5 | `Tests/TestHelpers/ViewerEmbedTestHost.cs`, `Tests/API/Security/S13_*.cs`, `Tests/API/Security/S14_*.cs` | L3 — duplicated helper / feature envy | `EstablishEmbedCookieAsync` was byte-identical in S13 and S14, and S13's `GrantAndConsumeAsync` was its first two thirds a third time | Hoisted `GrantEmbedSessionAsync` (hops 1+2) and `EstablishEmbedCookieAsync` (hop 3 on top of it) onto the host both fixtures already own. Every assertion carried over predicate for predicate | M4: 0 warnings, 4493/0 |
| T6 | `Tests/Integration/Containers/EmbedSessionSingleUseConcurrencyTests.cs` | L4 — duplicated scaffold | The redemption race and the handshake race each built clients above a `Barrier`, raced, and disposed in a `finally`; D73's rationale was restated in two comments | Extracted generic `RaceAsync<T>(callers, createClient, call, warmUp?)`. D73 is now structural — a third race inherits it | M4: 0 warnings, 4493/0 |
| T7 | `Tests/Integration/Containers/ViewerEmbedStorageGuaranteeTests.cs` | L1 — inconsistency | `CascadeLostMessage` was a computed `private static string =>` declared below the tests while its two sibling messages were `const` at the top | Made it `private const string` beside them | M4: 0 warnings, 4493/0 |
| T8 | `Lighthouse.EndToEndTests/tests/specs/auth/ViewerEmbedSession.spec.ts` | L1/L3 — inline locator | `getByRole("link", { name: "Overview" })` built inline twice while `LighthousePage.overviewLink`, added by this same slice, already spelled it | Routed both through the POM getter; `ensurePremiumLicense` returns the `OverviewPage` it already had instead of discarding it. Locator is byte-identical, so what the browser matches cannot change | M5: Biome 0/0, `tsc --noEmit` clean |

## Gates

| Module | Build | Tests | Commit |
|---|---|---|---|
| M1 domain + persistence | 0 warnings, 0 errors | 4493 passed / 0 failed / 0 skipped | `8a71263e6` |
| M2 application services | 0 warnings, 0 errors | 4493 passed / 0 failed / 0 skipped | `95ccd5b2f` |
| M3 driving adapters | 0 warnings, 0 errors | 4493 passed / 0 failed / 0 skipped | `08290118e` |
| M4 tests + helpers | 0 warnings, 0 errors | 4493 passed / 0 failed / 0 skipped | `751a01951` |
| M5 E2E page objects + spec | Biome 0 errors / 0 warnings (104 files) | `pnpm exec tsc --noEmit` clean | `88abd3281` |

`dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes` was run
before the first edit and again after M4. Zero findings on any slice file both times —
including ASP0015 on `Response.Headers["Referrer-Policy"]`, which confirms that header
has no typed `IHeaderDictionary` property and the string index is correct.

## Deliberately not done

- **Do not split `EmbedSessionTokenService`.** It carries two clusters (mint/redeem/revoke
  vs. record/consume-handshake) and is an L3 Large Class candidate, but the split runs
  through `IEmbedSessionTokenService`, whose shape ADR-131 fixes on purpose ("a Find next to
  a MarkRedeemed would offer callers a way to lose the race"), and it would force edits to
  the concurrency fixture. Out of bounds for a behaviour-preserving pass.
- **Do not extract the token codec** (`HashSecret` / `SecretMatches` / `GenerateUrlSafeValue`
  / `Base64UrlEncode` / `TrySplit`) into its own class. A real L3 win, but it needs a new
  production file and the brief restricts edits to the listed files. Recommended for a
  follow-up with an explicit file grant.
- **Do not merge `ResolveTokenLifetimeSeconds` with `ResolveHandshakeOutcomeLifetimeSeconds`.**
  Structurally identical, but DQ-2 says in as many words that the two windows are different
  concepts that evolve apart. Their shared *default values* were the real duplication, and
  T2 fixed that instead.
- **Do not hoist the D31 unavailability rule out of `EmbedStartController` and
  `EmbedHandshakeController`.** It is genuinely one piece of knowledge in two places, and
  it is the single most valuable refactor left in the slice — but sharing it needs a new
  file (a `ControllerBase` extension or a small resolver). Recommended for a follow-up.
- **Do not merge the two `Referrer-Policy` writes or the three inline HTML documents.** The
  documents say three different things to three different readers; merging them is
  structural DRY across different business concepts.
- **Do not unify the two `EmbedHandshakeNonceReplayed` constants** in S13 and the concurrency
  fixture. Both restate the name on purpose — "a test that reads the name off the production
  constant survives every mutation of it" (D62/D67 contract).
- **Do not fold `EmbedSessionTokenRedemption.Refused`'s `ApiKeyId` from `0` to `null`.** The
  contract-shape test asserts `Is.Zero`; changing it is a behaviour change, not a refactor.
- **Do not touch `TrySplit`'s `token.Split(TokenSeparator)`.** Adding a count of 2 would make
  `a.b.c` parse as `a` + `b.c` instead of being rejected. Behaviour change.
- **Do not swap `getByText("LighthousePremium License")` for `BlockedPage.container`** in the
  E2E spec. It is the POM-rule-correct change, but it alters what the browser matches and
  Playwright cannot be run in this environment — "never commit a spec/POM you have not run".
- **Do not rename `EmbedSessionToken` or `ApiKeyPrincipalFactory`** (D63), **do not collapse
  any `Try*` conditional update into a read-then-write** (ADR-131), **do not make the three
  refusal responses distinguishable** (D45), **do not rename the `EmbedHandshakeNonceReplayed`
  event** (D62/D67), **do not drop `Referrer-Policy: no-referrer`** (D39), **do not change
  `UseSerilog(logger, true)`** (D72). None of these were approached.
- Migrations, `*.Designer.cs`, `LighthouseAppContextModelSnapshot.cs` and `feature-delta.md`
  were not touched.
