# Mutation testing — 5828 (Delivery date sync: what a Delivery can bind its date to)

Run 2026-08-23 against `main` — the backend pass at `2b4da8b9e`, the frontend pass at `b16ed25a7`. Gate
is 80 % kill rate per stack.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **95.83 %** | 72 | 67 | 3 | 0 | 2 | 6 m 06 s |
| Frontend (StrykerJS 9.6.1) | **89.88 %** | 346 | 311 | 32 | 3 | 0 | 8 m 38 s |

One further frontend mutant is reported `RuntimeError` and sits outside the denominator; it is a kill
wearing a different word, and the reason is written down under the accepted survivors.

Config: `stryker.5828.backend.json`. A first run of the same config scored **73.61 %** (51 killed,
19 survived, 2 timeout). Nine tests closed sixteen of those nineteen; the three that remain are accepted
below. No production code was changed by this pass.

## Backend — per file

| file | killed | survived | timeout |
| --- | --- | --- | --- |
| DeliverySourcesController.cs | 34 | 0 | 0 |
| JiraReleaseVersionReader.cs | 25 | 0 | 2 |
| DeliverySourceBindability.cs | 5 | 0 | 0 |
| DeliverySourceResolver.cs | 2 | 2 | 0 |
| DeliverySourceOption.cs | 1 | 0 | 0 |
| DeliverySourcePreviewDto.cs | 0 | 1 | 0 |

Thirty further mutants did not compile and are outside the denominator, as Stryker scores them.

## Backend — closed by this pass

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

## Backend — accepted survivors

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

## Backend — not mutated

**`JiraWorkTrackingConnector.cs` was excluded.** Its delivery-source code is roughly 390 lines inside a
2363-line file, and Stryker.NET honours neither line ranges nor a whole-file `mutate` glob narrower than
the file. Including it would have produced thousands of mutants in unrelated Jira code that this run's
test filter cannot kill, and a score that measures the filter rather than the tests. Its delivery-source
behaviour is covered by the 25 specifications in `JiraDeliverySourceProviderTest`, which drive the real
connector over a stub Jira.

## Backend config traps, for whoever copies these configs

**`concurrency: 1` is load-bearing.** Above 1, every mutant reports `Timeout`. Because Stryker scores a
timeout as a kill, the run then reports a clean **100 %** having tested nothing. A headline score is
therefore never enough on its own — read the per-file kills in
`StrykerOutput/*/reports/mutation-report.json`, because a file with zero kills and all timeouts is a run
that proved nothing wearing a pass.

**The `test-case-filter` names four unit fixtures explicitly, and must not be widened.** A broader
`~DeliverySource` sweeps in the `WebApplicationFactory` acceptance fixture, which hangs under a mutated
controller: 22 s per mutant and an all-timeout run, against 300 ms for 72 tests. Any new test written for
this feature has to live inside one of the four named fixtures or the mutation run will not execute it.

## Frontend — per file

| file | mutants | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| deliverySelectionTabs.ts | 123 | 121 | 2 | 0 |
| DeliverySourceTab.tsx | 119 | 95 | 23 | 0 |
| DeliveryCreateModal.tsx (five line ranges) | 77 | 67 | 7 | 3 |
| DeliverySource.ts | 18 | 18 | 0 | 0 |
| DeliveryService.ts (L278-315) | 10 | 10 | 0 | 0 |

A first run of the same config scored **60.74 %** — 164 killed, 81 survived, 25 no coverage over 270
mutants — and did not mutate `DeliveryCreateModal.tsx` at all. Fifty specifications closed 80 of those
106 unkilled mutants, including every one of the 25 that had no coverage; the modal ranges then added 77
mutants, of which 67 are killed. No production code was changed by this pass.

## Frontend — closed by this pass

### `deliverySelectionTabs.ts` — 61 unkilled mutants, all 61 closed

The module was reachable only through the modal, and the modal never drives most of it. It now has a
specification of its own: 34 scenarios against the pure functions.

| survivor group | what it did | what now pins it |
| --- | --- | --- |
| `delivery.mode === "or" ? "or" : "and"` — six mutants: both conditional inversions, the equality operator and all three string literals | Reads a stored delivery's match mode as the opposite of what was saved, or as an empty string the backend does not understand. | *Reopening the form on a stored delivery* — one hydration of an `"or"` delivery and one of an `"and"` one, each asserting the mode that comes back. |
| `?? []` on `delivery.features` and on `delivery.rules` | A delivery stored before rules existed hydrates carrying a value nobody put there. | *Reads a delivery stored before rules existed as one with nothing chosen and no rules.* |
| `isIncompleteRule` — nine mutants: the arrow body, both logical operators, both conditional inversions and all three `.trim()` calls | Without `.trim()`, a rule field holding only spaces reads as filled in and reaches the backend as a match on whitespace. Without the `\|\|` chain, only one of the three fields is ever checked. | Two scenarios: each of the three fields empty, and each of the three holding nothing but spaces. |
| `ruleInputError` — seven mutants, including both message literals and `.some` → `.every` | With `.every`, one filled-in rule among several half-typed ones silences the complaint entirely, and the half-typed rules go to the backend. | *Asks for a rule when there is not one yet* and *asks for the gaps to be filled when one rule of several is half typed*, each against its literal. |
| `manualBlockingError` — the message literal and `.toLowerCase()` → `.toUpperCase()` | The complaint could be blanked, or could shout the tenant's own word for a feature back at them mid-sentence. | *Blocks saving until something is chosen, in the tenant's own word for it* — pinned against `At least one deliverable must be selected`, with the term renamed away from the seeded default. |
| `ruleBasedTab.fieldErrors` — eight mutants, none of which had ever been executed | `??` → `&&` makes the tab complain about matching before any rule is typed; removing the block returns `undefined`, which the modal then spreads into its error state. | Three scenarios walking the tab from no rules, to typed-but-unmatched, to matched. |
| `claims` — four mutants across `\|\|`, `??` and both operands | `&&` stops a delivery saved as rule-based with no rules stored from reopening on its own tab; `?? 0` → `&& 0` stops one that carries rules but predates the stored mode. Either way the form reopens on Manual and a save wipes the rules. | *Which tab a stored delivery reopens on* — one scenario per shape, plus the neither case. |
| The premium gates — four mutants on the rule-based gate, one on the source gate | Two user-facing notices and a tooltip, each blankable unnoticed, plus the two-valued discriminator the modal branches on to decide whether to lock the tab or explain inside it. | Both notices pinned against their literals; the source notice also proves it says *launch* rather than *delivery*. |
| ``key: `source:${source.key}` `` | Every source tab gets the same key, so a connection offering two sources shows one tab twice and the second is unreachable. | *Gets a tab of its own per source* — asserts the keys are distinct rather than asserting the prefix, so the format stays free to change. |
| The source tab's `hydrate`, `fieldErrors` and `toPayload` — five mutants | All three could return `undefined`, and `toPayload` could carry features off a tab that is documented as writing nothing down. | *Starts from a blank selection whatever delivery the form was opened for*, *never puts a complaint on a field the reader cannot type into*, and *writes nothing down, even once something has been picked*. |

### `DeliverySourceTab.tsx` — 44 unkilled mutants, 20 killed and one turned into a runtime error

| survivor | what it did | what now pins it |
| --- | --- | --- |
| L44 `"No longer available"` | The second blocked-reason sentence, rendered by no specification. A Release deleted in Jira would have shown a blank caption where its reason belongs. | *Says a Release that is gone from the board is no longer available, rather than undated* — and asserts it is not the missing-date sentence. |
| L57 the second conditional, and L61 the fallback message | Every empty preview would have read as *the tagged work is out of this Portfolio's scope*, sending the reader to widen a Portfolio that is not the problem. | *Says the Release has nothing to show when neither of the two named reasons applies* — a preview with no work and `emptyBecause: "None"`. |
| L77 the cache key, and L109 the effect's dependency list | Both make a second source tab show the first one's Releases: one by collapsing the cache keys together, the other by never re-running the fetch. The reader picks a Release that is not on the source they are looking at. | *Asks the server again for a second source rather than showing the first one's list* — two sources, each answering with its own list. |
| L164 `isOptionEqualToValue` — four mutants | The picker marks every row as picked, or none of them. With two projects naming a Release the same thing, that mark is the only thing telling the reader which of the two they chose. | *Marks the Release that was picked, and only that one, when the list is reopened.* |
| L223 and L235 on `previewFailed`, and L281's three | The failure alert could show before anything was picked, could show on every pick, could never show, or could be inverted. | *Claims nothing has failed before anyone has asked for a preview*, and the recovery scenario below. |
| L246-248 — six mutants, none of which had ever been executed | The entire `.catch` of the preview request. A preview that never arrived left the panel silently blank, with nothing to retry and nothing said. | *Says so when the preview cannot be fetched, and takes it back when the next one arrives* — pinned against the literal `This Jira Release could not be previewed. Try again in a moment.` |
| L247 inverted to always-true | A failure belonging to an abandoned pick overwrites the panel for the pick still on screen — the same stale-response bug the success path already guards against, on the error path. | *Ignores a failure belonging to a Release the reader has already moved on from* — pick A, pick B, then let A's request fail. |

### `DeliveryCreateModal.tsx` — newly mutated, 67 of 77 killed

The auto-fill and the validation ordering had never been mutated at all. Most of the newly mutated range
was already held by the specifications written when the behaviour shipped; five gaps needed closing.

| survivor | what it did | what now pins it |
| --- | --- | --- |
| L381 `name.trim()` → `name` | A name of nothing but spaces counts as a name, and the delivery saves called nothing. | *Does not take a name of nothing but spaces for a name.* |
| L576 `setRulesValidated(false)` → `true` | Opening the rule-based tab marks the rules as already matched, so the form complains that nothing matched rules nobody has typed yet. | *Does not treat rules as matched just because the tab was opened.* |
| `dateInputValue`'s two `padStart` calls | A Release dated on a single-digit day writes `2027-01-5` into the date field, which the field cannot read. Every earlier specification used a two-digit day. | *Writes a single-digit month and day into the date field with a leading zero.* |
| `getFirstBlockingError`'s ladder — the three conditionals and their three message literals | No specification walked the complaints in the order the reader answers them, so any of them could have been reordered or blanked. | *Names one missing thing at a time, in the order the reader can fix them* — name, then date, then a date in the future, then the tab's own complaint, each against its literal. |
| L571 the `tab.key === selectedTabKey` early return | Clicking the tab you are already on forgets the Release picked on it, and the message reverts to asking for one. | *Leaves the picked Release alone when its own tab button is clicked again.* |
| `>` in `isValidFutureDate` | Today counts as the future. | *Refuses today, because a date to launch on has to be a day still to come* — the date is built from this browser's calendar rather than from UTC, so the scenario does not depend on the runner's time zone. |

### `DeliverySource.ts`

L24 `"RetiredAtSource"` in the blocked-reason enum. Nothing parsed a payload carrying it, so the schema
could have stopped accepting the word the backend sends and every affected Release would have failed to
parse — which the screens surface as a load failure, not as a Release that has gone. *Names a Release
that cannot be bound because it is gone from the board.*

## Frontend — accepted survivors

**Decoration — thirteen mutants.** The `sx` prop objects (`DeliverySourceTab` L137, L180, L181, L195,
L264, L272, L282), the layout strings inside them (L264 `"flex"` and `"center"`), the two `{" "}` JSX
spacers (L182, L256) and `getOptionKey` (L162). None of them change what the screen says. Pinning them
means asserting a class name or a spacing value, which is worth less than an accepted survivor with a
reason.

**The unmount guard — four mutants (L92, L97, L102, L103).** `stillMounted` exists so a fetch resolving
after the tab is gone does not set state. React 18 no longer warns about that, and the flag has no other
observable effect, so a specification could only pin the flag itself.

**Three initial values no render can see (L79 `failed`, L221 `selectedId`, L224 `awaitedOptionId`).**
Every path through the effect assigns `failed` before the first paint, and both string refs are only ever
compared after a pick has overwritten them. Testing Library flushes effects inside `act`, so no assertion
runs early enough to observe what they started as.

**L155 `?? null` → `&& null` is an equivalent mutant by accident, and the reason is worth keeping.** The
mutant yields `undefined` on the first render, because `options.find(...)` finds nothing while nothing is
selected. MUI's `useControlled` decides once, on that first render, whether the Autocomplete is
controlled, and `undefined` means uncontrolled — so the component manages the selection internally and
behaves identically from then on. Separating the two would mean reaching past the public behaviour into
MUI's own state.

**L157 reports `RuntimeError`, which is a kill wearing a different word.** Inverting the `picked !== null`
guard makes clearing the picker call `onSelect(null)` and dereference `option.id`. *Survives the picker
being emptied, and asks for no preview of nothing* makes the mutant throw rather than fail an assertion,
so Stryker files it as a runtime error and leaves it out of the denominator. The behaviour is pinned
either way, and the score understates the suite by that one mutant.

**L198 and L199 — the preview grid's props.** `selectedFeatureIds` and `storageKey` are observable only
through the real `FeatureGrid`, which this specification replaces with a stub. Asserting them on the stub
would pin the stub.

**L132 — the blocked-reason caption's conditional, inverted to always render.** `blockedReason[null]` is
`undefined`, so the mutant renders an empty caption beside every unblocked Release. The text on screen is
identical; only the element count differs, and an element count is not a behaviour.

**`deliverySelectionTabs.ts` L75 and L76 — the two built-in tab keys.** Internal identifiers, read through
the same constants everywhere, so blanking one keeps every comparison consistent and every tab distinct.
Nothing persists them and no user ever sees them.

**`DeliveryCreateModal.tsx` L292 — the unregistered-tab fallback.** Deliberate future-proofing, and the
comment beside it says as much: a tab that neither saves a selection nor reads one from the work tracking
system shows nothing rather than taking the whole form down. No such tab exists, so the mutant is
unreachable.

**L343 and L344 — the empty-date guard in `isValidFutureDate`, three mutants of which two were never
executed.** Unreachable through the form: `getFirstBlockingError` answers an empty date before
`isValidFutureDate` is asked, and `validateForm` only runs from Save, which is disabled while a blocking
error stands. Removing the guard changes nothing anyway, because `new Date("")` is an Invalid Date and
compares false.

**L349 `setHours(0, 0, 0, 0)` → `setMinutes(0, 0, 0, 0)`.** Zeroing today's minutes instead of its hours
moves `today` from local midnight to the current hour. Both are still today, so a comparison against a
whole calendar day lands on the same side of the line either way.

**L586 and its string literal — the dateless branch of the auto-fill.** Reachable only if the server
offers an option with no date *and* marks it selectable. It marks exactly those `isSelectable: false`, and
the picker refuses to let a non-selectable row be clicked, so the handler never sees one. The refusal
itself is pinned, by *lists a dateless Release but refuses to let it be picked*.

**L577 `setMatchedFeatures([])`, L588 `setErrors(...)` and L590's `useCallback` dependency list.** The
first is unobservable on its own: the only reader of `matchedFeaturesLength` is the rule-based blocking
error, which the same handler gates behind `rulesValidated`, set false on the line above. The second needs
a failed Save on one tab, a pick on another and a switch back — six steps for one mutant. The third only
makes the callback new on each render, which nothing depends on.

## Frontend config traps, for whoever copies these configs

**A file missing from `mutate` leaves no trace in the report.** `DeliveryCreateModal.tsx` produced no
mutants because it was never named in `mutate`. There is no zero-mutant row to notice, so its absence
reads exactly like a file that was never part of the feature — the only check that catches it is reading
`mutate` against the diff. Ranges themselves work: StrykerJS line ranges are 1-based and inclusive, and a
ranged entry needs no accompanying plain-path entry. `DeliveryService.ts:278-315` proves it, producing
mutants only between lines 278 and 310 of a 315-line file. The config now names five ranges for the modal
— `271-292`, `335-353`, `371-392`, `444-459`, `570-591` — covering the licence gate and source routing,
the two date helpers, the blocking-error ladder, the source-tab field locking, and the tab switch with
the auto-fill.

**The config path is relative to Stryker's working directory, not the repo root.** Run from
`Lighthouse.Frontend`, the invocation is
`pnpm exec stryker run ../docs/feature/epic-5565-delivery-date-sync/mutation/stryker.5828.frontend.json`.
Without the `../` it fails with *Invalid config file … File does not exist!*. The shorter form worked only
while a `Lighthouse.Frontend/docs` symlink existed, and that symlink has to be removed before any build,
because Biome reformats the whole documentation tree through it.

**The Vitest runner config cannot be committed under the name Stryker reads it by.** `.gitignore:445` is
`**/vitest.stryker*.ts`, which matches at any depth, so a copy kept beside these configs as
`vitest.stryker.mutation.ts` is ignored too — an earlier pass left one there believing it was saved, and
it was never committed. The committed copy is `mutation-vitest.config.ts` in this folder; copy it to
`Lighthouse.Frontend/vitest.stryker.mutation.ts` before running.

**A specification missing from that runner's `include` list is indistinguishable from a coverage gap.**
Stryker reruns the suite per mutant, and sweeping all 330 spec files exhausts the node heap, so `include`
names only the specifications covering the mutated files. Anything left out reports every mutant in the
code it covers as `NoCoverage`, which reads like nobody wrote a test. Check it before believing a gap —
here the 18 `NoCoverage` mutants in `deliverySelectionTabs.ts` were **not** that. The module had no
specification of its own at all, and the modal specifications that do reach it never drive
`ruleInputError`, the rule-based field errors, or any of the source tab's callbacks. The list was
complete with respect to the specifications that existed.

**`disableTypeChecks` must stay `false`.** A run in this repository with it enabled wrote `@ts-nocheck`
into 661 source files. `inPlace: true` mutates the working tree, so `git status` belongs after every run,
not only after a crash.

## One thing this pass found and did not fix

The rule-based premium notice reads *"Rule-based delivery selection is a premium feature"*, with
**delivery** written into the sentence rather than taken from the tenant's terminology. A tenant who calls
these Launches is shown a word they have renamed. The source-tab notice beside it does it correctly. This
pass changed no production code, so the literal is pinned as it stands and the sentence is left for
whoever owns that copy.

## Gates

Full backend suite (live-connector categories excluded) **5898 passed, 0 failed, 7 skipped** — up nine
from the 5889 baseline, which is exactly the nine specifications added here. Build **0 warnings**.
`dotnet format analyzers --severity info` reports **37 diagnostics, unchanged from baseline** (35 ×
CA1861 in migrations, 1 × CA1825, 1 × S6561): these tests add none.

One earlier full run reported a single failure that did not reproduce and was not named in the output;
it ran immediately after the six-minute mutation pass, which is the load profile the known
`ReleaseServiceTest` contention appears under. The clean run above is the terminating one.

Full frontend suite `pnpm test` **330 files, 4514 tests passing** — up 50 from the 4464 baseline, and up
one file. That is exactly the 34 scenarios in the new `deliverySelectionTabs.test.ts`, 15 added to
`DeliverySourceTab.test.tsx` and 1 to `DeliverySource.test.ts`. `pnpm build` completes with **zero errors
and zero warnings**, which implies a clean Biome check on `./src` through the `prebuild` hook. `git
status` after the terminating mutation run shows only the four files this pass touched, so `inPlace: true`
left no mutant behind.

---

# Mutation testing - 5829 (Delivery date sync: creating a Delivery from a Release)

Run 2026-08-24 against `4b74684a8`, after the adversarial-review fixes and before the UI feedback
commits. Gate is 80 % kill rate per stack.

| stack | headline | slice-touched lines only | pre-existing lines |
| --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **70.36 %** (197 killed / 83 survived) | **80.37 %** (86 / 21) | 64.16 % (111 / 62) |
| Frontend (StrykerJS) | **71.80 %** (494 killed / 119 survived / 75 no-coverage) | **80.09 %** (181 / 45) | 80.88 % (313 / 74) |

**Read the second column, not the first.** Stryker.NET cannot mutate line ranges, so scoping to a file
mutates all of it: `DeliveriesController.cs` is 667 lines and `Delivery.cs` 385, and this slice touched a
fraction of each. The headline therefore grades code the slice never wrote. The split is computed by
intersecting mutant line numbers with `git diff fb2ef842a..HEAD`, so a mutant on a line that merely
shifted counts as pre-existing - the slice column is if anything pessimistic.

The frontend's 75 no-coverage mutants are an artefact of the same scoping: 71 of them sit in the four
files listed whole rather than by line range (`Delivery.ts`, `DeliveryService.ts`, `DeliveryModals.tsx`,
`WorkItemRules.ts`), on pre-existing code whose covering specs are not in the slice-scoped vitest
include list. Including the 4 in-slice ones, the frontend slice figure is 78.7 %.

## What the survivors bought

Backend, 16 of 21 slice-touched survivors closed (`5a2b6192d`, +6 specs). The three that mattered:
`MarkAsChanged()` could be deleted from `BindToSource` unnoticed; the absent-from-the-answer 503 path -
the one that stops a network blip reading as a deleted Release - was driven by nothing; and the
`DateTimeKind` branch had no case with a Kind other than `Unspecified`. Two existing specs turned out to
be vacuous: the token row unbound first, so the bind's own bump was invisible, and the UTC specs
asserted only `Kind == Utc` (true down either branch) plus a day the +1h Zurich shift does not move.

Frontend, the two weak files strengthened (`d50539d29`, +11 tests). `DeliverySection.tsx` 67.2 % and
`useDeliveryManagement.ts` 51.9 % each hid one real gap: `canreadonly &amp;&amp; onUnbind` could be forced true,
so a read-only viewer would be offered Stop following with nothing to catch it; and the
`catch { setDeliverySources([]) }` degradation path was covered by a test asserting `[]`, which is also
the initial state, so emptying the block changed nothing observable.

Deliberately left: console-only wordings, MUI `sx ` object literals, and `useCallback` dependency arrays -
pinning a dependency array pins an implementation detail and goes stale on the next refactor.

**Not re-measured after those commits**, by maintainer decision - the added specs were each verified by
hand-applying their mutant and confirming red, which is the evidence the score would have stood in for.

Configs: `stryker.5829.backend.json`, `stryker.5829.frontend.json`, `mutation-vitest.5829.config.ts`. The
frontend run needs its own vitest include list: the repo's `vitest.stryker.mutation.ts` is scoped to a
different feature and would report near-total survival. Frontend line ranges must be recomputed against
HEAD after any commit - they shift.

