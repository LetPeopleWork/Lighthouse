# Mutation testing — 6712 (Faster page loads)

Run 2026-11-28 against the worktree at HEAD `4ed057d89` plus two test files and two tests added by this pass. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET), feature code | **100 %** (every mutant in the feature's code killed) | 27 | 27 | 0 | 0 | about 3 m per run |
| Backend, headline incl. whole-file `LighthouseReleaseService.cs` | 33.16 % (not the feature's code, see below) | 67 in scope | 62 | 4 | 0 | 3 m |
| Frontend (StrykerJS 10.1.1), first run | 71.90 % | 242 | 174 | 58 (+10 no coverage) | 0 | 10 m 54 s |
| Frontend, after the kill round (incremental) | **83.06 %** | 242 | 201 | 41 | 0 | 4 m 10 s + 2 m 53 s incremental |

Configs: `stryker.6712.backend.json`, `stryker.6712.frontend.json`, `vitest.stryker.mutation.ts` (copy it into `Lighthouse.Frontend/` to run; it is gitignored there).

## Backend

| file | killed | survived | no coverage | note |
| --- | --- | --- | --- | --- |
| `Platform/Host/BreachSafeResponseCompressionProvider.cs` | 7 | 0 | 0 | 1 ignored |
| `Configuration/ResponseCompressionConfiguration.cs` | 1 | 0 | 0 | |
| `Platform/Host/NeverCompressResponseAttribute.cs` | 0 | 0 | 0 | no mutable code |
| `API/VersionController.cs` | 15 | 0 | 0 | after this pass; first run left 4 no-coverage mutants in `GetNewestVersion` |
| `Platform/Releases/LighthouseReleaseService.cs` (whole file) | 40 | 4 | 123 | the file holds the installer, which only the excluded integration tests reach; the feature's method (lines 80-85) has 2 killed, 0 survived |

The 4 survivors in `LighthouseReleaseService.cs` are old code: two `??` defaults on constructor fields (lines 19-20) and a log statement (line 256).

### Closed by this pass
- `VersionControllerTest.GetNewestVersion_ReleaseKnown_ReturnsTheVersion`: a known newest release answers 200 with that version.
- `VersionControllerTest.GetNewestVersion_NoReleaseKnown_ReturnsNotFound` (null and empty): no known release answers 404.

### Not mutated
Integration and acceptance suites (`ResponseCompressionWireContractTest`, `NewestVersionWireContractTest`) are left out of the filter: they start a host per test.

## Frontend

Final per-file (incremental run):

| file | score |
| --- | --- |
| `askSharedReadsAgainFor.ts`, `pageFreshQueryKey.ts`, `pageOfTheLatestAsk.ts`, `useLicenseRestrictions.ts` | 100 % |
| `PageMoveRefresh.tsx` | 87.50 % |
| `useUnitLists.ts` | 87.50 % |
| `LighthouseVersion.tsx` | 84.21 % |
| `useRbac.ts` (lines 36-62) | 81.82 % |
| `usePageFreshQuery.ts` | 77.22 % |
| `LatestReleaseInformationDialog.tsx` | 52.63 % |

### Closed by this pass
- `src/hooks/usePageFreshQuery.test.tsx` (new): loading, answer, disabled, failure, ask again (including a second failure), reuse on the same page, re-ask after a page move, no second ask while one is on its way, loader reuse and error pass-through, `pageOfTheLatestAsk` recording, `askSharedReadsAgainFor` leaving unshared reads alone, page counting in `PageMoveRefresh`, `listCouldNotBeLoaded`.
- `src/components/App/LetPeopleWork/LighthouseVersion.notification.test.tsx` (new): the update popup names the version, "don't show for this version" is remembered, no popup on a DEV build.

### Accepted survivors
- `LatestReleaseInformationDialog.tsx` (9): `sx` object and style-string mutants (margins, flex) and the React Query key of the release notes; MUI `sx` mutants are the dominant equivalent class here, and the query key only names a cache slot no test reads.
- `usePageFreshQuery.ts` (18): `useCallback`/`useState` dependency arrays (`[]`, `[page]`, `[refetch]`, `[queryClient, key, ask, page]`), optional-chaining removals on values that are defined whenever the branch runs (`query.data?.answer` only when ready, `query.error?.reason` only when failed), `setAskingAgain(true)` to false (the in-flight state already hides the failure through `fetchStatus`), and the `fetchStatus === "fetching"` guard variants whose effect needs a timing the test cannot pin without the hook's internals.
- `useUnitLists.ts` (4): dependency arrays of `useCallback` and the arrow body of `listCouldNotBeLoaded` replaced (covered through the direct test only for the result, not for the identity).
- `useRbac.ts` (2): `refetchType: "all"` string and the dependency array.
- `PageMoveRefresh.tsx` (2): the `page === 0` early return and its block (at page 0 nothing was asked on an earlier page, so the refresh finds nothing either way).
- `LighthouseVersion.tsx` (6): `Promise.resolve(false)` for non-internal owners, the console.error text, parts of the notification condition and its text split.

Stryker may report some of these as survivors falsely (see the repo's notes on StrykerJS false survivors); none was re-checked by hand.
