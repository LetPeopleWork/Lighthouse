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

---

# Refactoring log — Epic 5146 slice 03 (ADO #5694)

DELIVER Phase 3, `/nw-refactor`, RPP L1–L6 over `git diff 4d91ea229..HEAD`.
Behaviour-preserving only. The slice was a deletion — the API-key embed path went, and
separately the readable-scope conjunct on `/embed/start` was reversed — so the residue is
over-large seams, stale prose, and names chosen to contrast with something now absent.

Baseline: `dotnet build -v q --nologo` **0 warnings / 0 errors**. `dotnet test --no-build`
reported **4498 passed / 0 failed / 0 skipped** at every gate. The brief predicted 10
pre-existing failures in `LighthouseReleaseServiceIntegrationTest` (GitHub API rate limit);
that limit had reset on this machine, so all 4498 ran green. Nothing was suppressed.

## Transformations

| # | File | RPP | Smell removed | Transformation | Module gate |
|---|---|---|---|---|---|
| T1 | `Services/Implementation/Repositories/EmbedSessionTokenRepository.cs` | L4 — speculative generality | `Outstanding()` — added by slice 01's own T1 to serve `TryMarkRedeemedAsync` **and** `RevokeOutstandingForApiKeyAsync` — has one caller left, and its comment contrasts the expiry question with a caller that no longer exists | Inlined back into `TryMarkRedeemedAsync` as one `Where`. Predicate unchanged term for term, so ADR-131's affected-row verdict is unchanged; comment reduced to the one line naming ADR-131 | M1 |
| T2 | `Services/Implementation/Auth/EmbedSessionTokenService.cs` | L1 — comment narrating a deletion | The prune comment opened "Slice 03 deleted the API-key mint, which was the only thing that pruned", naming code the reader cannot go and look at | Rewritten to the invariant that survives: recording an outcome is the only write there is, so it prunes or the table is append-only. `PruneSpentAsync` stays exactly where it is | M2 |
| T3 | `API/EmbedHandshakeController.cs` | L1 — stale reference | Class comment justified itself as "a second controller … so `[AllowAnonymous]` never lands on the minting one". `EmbedSessionController`, the minting one, was deleted by the slice | Rewritten to what the attribute still buys: `[AllowAnonymous]` covers nothing but this one read verb | M3 |
| T4 | `API/EmbedHandshakeController.cs` | L1 — member ordering | `ResolveSessionLifetimeSeconds()` declared above the public action it serves; both sibling controllers on the prefix put private helpers below | Moved below `Poll` | M3 |
| T5 | `API/EmbedStartController.cs` | L1 — narration | Nine-line essay in `ResolveOutcomeAsync` arguing, in past tense, why the readable-scope conjunct was dropped — about a branch that is gone | Collapsed to three lines saying what is true now: a sign-in that worked and still names nobody is the only refusal here, because what a viewer may read is RBAC's answer on every request. Keeps the D49/D60 anchor and the "D31's ladder is a different sentence" caveat; the argument itself lives in the commit that made the change | M3 |
| T6 | `API/EmbedStartController.cs` | L1 — dated changelog in a comment | `NoProfileRefusalCode`'s comment carried "`no_access` retired 2026-08-06 with the readable-scope conjunct" | Dropped; DQ-1's actual rule (one class-level code, never prose, never anything about the viewer or the instance) kept verbatim | M3 |
| T7 | `Tests/TestHelpers/ViewerEmbedTestHost.cs` | L1 — member ordering | `WithEmbedRateLimit` / `WithTokenLifetime`, absorbed from the deleted `EmbedSessionTestHost`, landed in the middle of the static hop drivers and split them in two | Moved up beside the four host properties they belong with. No signature or body change | M4 |
| T8 | `Tests/Services/Implementation/Auth/EmbedSessionTokenServiceTest.cs` | L1 — name of an absent thing | `RecordingAHandshakeRefusal_PrunesTheRowsAlreadySpent` passed `"no_access"`, a refusal code retired with the conjunct | `"no_profile"`. The parameter is arbitrary to this test — the assertion is `PruneSpentAsync` `Times.Once`, carried over verbatim | M4 |
| T9 | `Tests/Services/Implementation/Auth/EmbedSessionTokenServiceTest.cs`, `Tests/API/Security/S12_EmbedSecurityReviewFindingsTests.cs` | L1 — narration | Two comments explained themselves by recounting what slice 03 removed | Trimmed to the standing fact (the prune has one home; F3's control lives in S14) | M4 |
| — | `Lighthouse.EndToEndTests/tests/specs/auth/ApiKeys.spec.ts` | — | Checked for the residue the brief predicted — a stale `mode: "serial"`, module-level state shared between tests that are now one test, imports for page objects it no longer opens | **None found.** The slice's own `e2a365f8c` had already removed the serial declaration, moved the three `let`s inside the surviving test, dropped the `EmbedEntryPage` import and made `LoginPage` type-only. `EmbedEntryPage` is still used by `ViewerEmbedSession.spec.ts`, so it is not orphaned | M5 |

## Gates

| Module | Build | Tests | Commit |
|---|---|---|---|
| M1 domain + persistence | 0 warnings, 0 errors | 4498 passed / 0 failed / 0 skipped | `86ffe4f28` |
| M2 application services | 0 warnings, 0 errors | 4498 passed / 0 failed / 0 skipped | `5539801e4` |
| M3 driving adapters | 0 warnings, 0 errors | 4498 passed / 0 failed / 0 skipped | `73dbba6de` |
| M4 tests + `ViewerEmbedTestHost` | 0 warnings, 0 errors | 4498 passed / 0 failed / 0 skipped | `a302e2a02` |
| M5 `ApiKeys.spec.ts` | Biome 0 errors / 0 warnings (104 files) | `pnpm exec tsc --noEmit` clean | no code change — verification only |

M2, M3 and M4 were edited before the M1 suite finished and were therefore covered by **one**
suite run rather than three; that run is reported once above rather than claimed three times.
M1 has its own run. Playwright was deliberately not run (Keycloak torn down by the orchestrator).

`dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes` reports **zero
findings in any file touched by this pass**. The ~35 hits it does report are the repo's known
pre-existing noise — CA1861/CA1825 in generated EF migration files, one S6561 in
`AzureDevOpsWorkTrackingConnectorTest` — all untouched here.

## Considered and deliberately NOT done

- **`EmbedEntryController.ResolveViewerPrincipalAsync` was not inlined.** The brief flagged it as
  possibly "a wrapper worth inlining" now that the API-key branch is gone. It is not a wrapper: it
  holds the F7 blank-subject guard, the D57 non-creating profile lookup, a refusal log and the
  principal construction. Inlining puts four exit paths into `Enter`, which already has three.
  Its name stays too — "viewer" is this feature's ubiquitous language (`ViewerEmbedTestHost`,
  `S14_ViewerEmbed*`, `ViewerEmbedSession.spec.ts`), not a marker contrasting with the API key.
- **`RedeemAsync`'s D63 comment was left naming the API key**, because `NamesAnIdentity` still
  reads `stored.ApiKeyId`. The column is deliberately kept until the contract-phase drop, so the
  code genuinely still asks that question and trimming the comment would leave the check
  unexplained. Narrowing the check to the subject alone is a database-predicate change — out of
  bounds.
- **`Referrer-Policy` stays a string-indexed header write** on both hops (D39). Slice 01 already
  established via the analyzer sweep that ASP0015 does not fire, so no typed property exists.
  Neither line was edited, so neither enters Sonar's new-code window.
- **The `/embed/start` refusal HTML was not reworded.** It still reads "no team or portfolio has
  been shared with you yet", which described the deleted readable-scope refusal; the surviving
  refusal is "signed in but no profile". A real defect, but the response body is user-visible
  output and changing it is a behaviour change, not a refactor. Raised as a finding below.
- **`ViewerEmbedTestHost` was not split.** The brief asked for a judgement. It is 598 lines and
  does carry two axes — building configured instances, and driving the three hops — and the seam
  is real: every hop driver is `static` and touches no instance state, so a `ViewerEmbedHops`
  companion would compile. Declined because the split buys naming, not decoupling.
  `GrantEmbedSessionAsync`, `MintTokenAsync` and `EstablishEmbedCookieAsync` compose the hops
  against a host the fixture owns, and the nested `HandshakeReading` record plus the
  `StartPath` / `HandshakePath` / `EntryPath` constants are used by both halves — so the
  extraction renames call sites in eight test files without removing a dependency. The absorbed
  members are each a variation on an axis the class already had. Re-raise if a third fixture ever
  needs the hops without needing the host.
- Every slice-01 refusal above still stands and none was approached: no rename of
  `EmbedSessionToken` / `ApiKeyPrincipalFactory` (D63); no `Try*` conditional update collapsed to
  read-then-write (ADR-131); the refusal responses stay identical (D45); the
  `EmbedHandshakeNonceReplayed` event name is untouched (D62/D67); `Referrer-Policy: no-referrer`
  stays on both hops (D39); the concurrency fixtures still build their clients above the barrier
  and warm the path first (D73); no nonce or nonce hash entered a log message (N1); pruning did
  not move (it stays in `RecordHandshakeOutcomeAsync`).

## Findings — outside the file list, reported not edited

Real deletion residue that sits in files the brief did not put in scope:

1. **`Models/Auth/EmbedSessionTokenMintResult.cs` is dead.** Zero production references. The only
   thing keeping it compiled is
   `EmbedSessionContractShapeTest.EmbedSessionTokenMintResult_UnsetToken_IsEmpty` — a shape test
   for a shape nobody produces, since `MintAsync` was its only producer and went with the slice.
   Removing it means deleting a test, which is not the crafter's call.
2. **`IApiKeyIdentityResolver` / `ApiKeyIdentityResolver` are registered but never resolved.**
   `Program.cs:1010` still runs `AddScoped<IApiKeyIdentityResolver, ApiKeyIdentityResolver>()`,
   and both call sites — the embed cookie validator's API-key branch and `EmbedEntryController`'s
   API-key principal branch — were deleted by this slice. The interface's own doc comment says it
   exists "so the embed redemption path can feed `ApiKeyPrincipalFactory`". It is in neither the
   brief's keep-list nor its scope list. `ApiKeyIdentityResolverTest` is now its only exerciser.
3. **`EmbedSessionTokenRedemption.ApiKeyId` is never read.** `EmbedEntryController` takes only
   `.Subject`. Slice 01 already ruled out changing `Refused`'s `0` (the contract-shape test
   asserts `Is.Zero`); the whole positional member is the wider question, and it rides the same
   contract-phase drop as D63's renames.
4. **`Models/Auth/EmbedSessionToken.cs:16` claims "the API-key path stays mintable this
   release".** It is not mintable — the mint is gone. Keeping the column is right and
   expand-only; the sentence justifying it is now false.
5. **`Tests/Integration/Containers/ViewerEmbedStorageGuaranteeTests.cs` seeds `'no_access'`** in
   three raw-SQL literals, and asserts a cascade whose message reads "must still revoke every
   token it minted while the API-key path is …". Same retired code as T8, in a file the slice did
   not touch.

Migrations, both `Lighthouse.Migrations.*` projects, `feature-delta.md` and
`/storage/repos/lighthouse-jira-app` were not touched.
