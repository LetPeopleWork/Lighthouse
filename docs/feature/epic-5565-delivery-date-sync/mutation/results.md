# Mutation testing — 5828 (Delivery date sync: what a Delivery can bind its date to)

Run 2026-08-23 against `main` @ `2b4da8b9e`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **95.83 %** | 72 | 67 | 3 | 0 | 2 | 6 m 06 s |
| Frontend (StrykerJS) | pending | — | — | — | — | — | — |

The frontend row is **pending, because that run happens after this pass** — `stryker.5828.frontend.json`
and `vitest.stryker.mutation.ts` are in this folder ready for it.

Config: `stryker.5828.backend.json`. A first run of the same config scored **73.61 %** (51 killed,
19 survived, 2 timeout). Nine tests closed sixteen of those nineteen; the three that remain are accepted
below. No production code was changed by this pass.

## Per file

| file | killed | survived | timeout |
| --- | --- | --- | --- |
| DeliverySourcesController.cs | 34 | 0 | 0 |
| JiraReleaseVersionReader.cs | 25 | 0 | 2 |
| DeliverySourceBindability.cs | 5 | 0 | 0 |
| DeliverySourceResolver.cs | 2 | 2 | 0 |
| DeliverySourceOption.cs | 1 | 0 | 0 |
| DeliverySourcePreviewDto.cs | 0 | 1 | 0 |

Thirty further mutants did not compile and are outside the denominator, as Stryker scores them.

## Closed by this pass

Six new specifications in `JiraDeliverySourceProviderTest`, three in `DeliverySourcesControllerTest`,
and four assertions added to specifications that already existed.

### `JiraReleaseVersionReader.cs`

| survivor | what it did | what now pins it |
| --- | --- | --- |
| L78 `AssumeUniversal \| AdjustToUniversal` → `&` | `&` collapses the pair to `None`, so a bare `2027-02-26` parses in whatever zone the server sits in instead of UTC. Same ticks, different meaning, and nothing downstream can tell. | *A release date is read as the day Jira named in UTC rather than in whatever zone the server sits in* — asserts the parsed value **and its `Kind`**. Value alone cannot catch this: `DateTime` equality ignores `Kind`, which is why the mutant survived a suite that already asserted the date. |
| L81 `parsed ? date : null` → `true ? date : null` | An unreadable `releaseDate` yields `DateTime.MinValue` rather than no date, so a Release ends up dated to the first day of the calendar and a Delivery bound to it is forecast against that. | *A release date that cannot be read leaves the Release dateless rather than dated at the start of time* — a version dated `"sometime in the spring"` must come back with no date and blocked as `NoDateSet`. |
| L59 `ReadFlag(version, "released")` → `""` | Reads a property that does not exist, so every Release reads as unreleased and every shipped Release is offered up for binding again. | *A Release Jira says has shipped is read as shipped* — both directions, off the captured payload. |
| L38 `\|\|` → `&&` in the malformed-payload guard | `&&` lets a `values` that is not an array through to `EnumerateArray()`, which throws — one odd project takes the whole picker down instead of costing the reader that project. | *A page whose values are not a list is read as holding nothing rather than failing.* |
| L40 `return ([], true)` → `false` | A page carrying no readable list would report that more is coming. | *A page that announces more and carries no list ends the sweep rather than inviting another ask.* |
| L18 `"name"` → `""` and L85 ×2 `string.Empty` → marker | The project name read from the wrong property, and the two fallbacks for a field that is not there — an explicit `null`, and a key that never arrived. | *The projects a credential can see are read by key and by name* — one payload carrying all three shapes. |

### `DeliverySourcesController.cs`

| survivor | what it did | what now pins it |
| --- | --- | --- |
| L116 block removal | Drops the guard for a resolver answer that does not mention the reference at all, which then dereferences null. | *A resolver that says nothing at all about the Release is answered as unreadable rather than as gone* — an empty answer must reach the caller as 502, never as a deletion. |
| L167 `feature.Portfolios.Any(...)` → `All` | A Feature blocked on one board and not another would read as unblocked. | *A Feature blocked on one of the boards it sits on comes along blocked* — two Portfolios, blocked on one. |
| L193 `AvailableSources(...).Any(...)` → `All` | Over an empty descriptor list `All` is true, so a connection offering nothing would be treated as offering everything and the remote asked for a source nobody named. | *A connection that reads delivery sources but offers none is refused before the remote is asked.* See below — this one was triaged as an equivalent mutant and is not. |
| L127, L128, L137, L171, L174 string mutations | Five user-visible messages, each replaceable with an empty string unnoticed. | Each pinned against its literal once. L127 and L128 carry the difference between "go and date this Release" and "go and pick another one"; L137 is the 502 the caller must not read as a deletion; L171 and L174 name the Portfolio id and the source key a 404 is about. |

### L193 is not an equivalent mutant

The triage for this run reasoned that exactly one descriptor is ever offered and that `Any` and `All`
coincide over a single-element sequence. The first half is true today and the conclusion still does not
hold, because the empty case is reachable and is documented as intentional. `JiraWorkTrackingConnector`
says so in as many words: *"a connection which one day cannot offer its Releases can say so by returning
nothing, without any other connection being affected."* Over an empty list `Any` is false and `All` is
true, so under the mutant a connection that offers nothing offers everything, and `GetOptions` would ask
the remote for a source the connector just declined to name. A second source would break it the other way
round. It is now killed.

## Accepted survivors

**`DeliverySourceResolver.cs:14` and `:15` — the two `ArgumentNullException.ThrowIfNull` guards.**
Unreachable through the public API. The only caller is `DeliverySourcesController`, which has already
returned 404 if the Portfolio is null and passes a single-element list it builds itself, so neither
argument can arrive null. Reaching them would mean calling the resolver from a test in a way no caller
does, which pins the guard rather than any behaviour.

**`DeliverySourcePreviewDto.cs:29` — `SourceReference { get; set; } = string.Empty`.** The initialiser
on a request DTO. Every specification sets the property explicitly, and on the wire the model binder
does; the initialiser only decides what an empty POST body binds to, which the route answers as 502
either way. No specification falls out of it that is about anything a reader would notice.

**The two `JiraReleaseVersionReader.cs:36` timeouts are kills, not gaps.** Both mutants invert the
`isLast` reading, so the connector's paging loop never terminates and Stryker times the mutant out.
A mutant that hangs the sweep is caught by the suite in the only way an infinite loop can be.

**One caveat on L40, recorded so nobody re-derives it.** The triage held that this boolean is the sweep's
completeness flag, and that an incomplete sweep resolves a missing reference as `Unavailable` rather than
`NotFound`. It is not: `ReadVisibleProjects` and `ReadReleasesOf` return `SawEverything: false` only on an
HTTP failure, and both loops already stop on `isLastPage || page.Count == 0`, so the empty page ends the
loop under either value. The new specification pins the reader's own contract, which is worth having,
but it is belt and braces rather than the second line of defence it was thought to be.

## Not mutated

**`JiraWorkTrackingConnector.cs` was excluded.** Its delivery-source code is roughly 390 lines inside a
2363-line file, and Stryker.NET honours neither line ranges nor a whole-file `mutate` glob narrower than
the file. Including it would have produced thousands of mutants in unrelated Jira code that this run's
test filter cannot kill, and a score that measures the filter rather than the tests. Its delivery-source
behaviour is covered by the 25 specifications in `JiraDeliverySourceProviderTest`, which drive the real
connector over a stub Jira.

## Two config traps, for whoever copies these configs

**`concurrency: 1` is load-bearing.** Above 1, every mutant reports `Timeout`. Because Stryker scores a
timeout as a kill, the run then reports a clean **100 %** having tested nothing. A headline score is
therefore never enough on its own — read the per-file kills in
`StrykerOutput/*/reports/mutation-report.json`, because a file with zero kills and all timeouts is a run
that proved nothing wearing a pass.

**The `test-case-filter` names four unit fixtures explicitly, and must not be widened.** A broader
`~DeliverySource` sweeps in the `WebApplicationFactory` acceptance fixture, which hangs under a mutated
controller: 22 s per mutant and an all-timeout run, against 300 ms for 72 tests. Any new test written for
this feature has to live inside one of the four named fixtures or the mutation run will not execute it.

## Gates

Full backend suite (live-connector categories excluded) **5898 passed, 0 failed, 7 skipped** — up nine
from the 5889 baseline, which is exactly the nine specifications added here. Build **0 warnings**.
`dotnet format analyzers --severity info` reports **37 diagnostics, unchanged from baseline** (35 ×
CA1861 in migrations, 1 × CA1825, 1 × S6561): these tests add none.

One earlier full run reported a single failure that did not reproduce and was not named in the output;
it ran immediately after the six-minute mutation pass, which is the load profile the known
`ReleaseServiceTest` contention appears under. The clean run above is the terminating one.
