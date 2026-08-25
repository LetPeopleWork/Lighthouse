# Mutation testing — 5831 (Say so when the bound Jira Release is gone)

Run 2026-08-25 against `main` at `7fd31318d`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **83.20 %** | 125 | 104 | 21 | 0 | 31 m 21 s |
| Frontend (StrykerJS) | **97.30 %** | 37 | 36 | 1 | 0 | 58 s |

Config: `stryker.5831.backend.json`, `stryker.5831.frontend.json` with `mutation-vitest.5831.config.ts`.

## Backend — per file

| file | killed | survived |
| --- | --- | --- |
| Delivery.cs | 89 | 17 |
| DeliverySourceSyncService.cs | 15 | 4 |

## The recorded number understates the suite, and that is not fixed here

Six of the seventeen `Delivery.cs` survivors — the guards and the change-detection inside
`SyncFromSource` and `ApplyWhatTheSourceNowSays` (L202, L204, L208, L232, L238, L243, L245) — are
**artifacts of the test filter, not gaps**. The filter read
`FullyQualifiedName~DeliverySourceSyncServiceTest`, which does not match
`DeliverySourceSyncInvariantTest`: the aggregate's own suite never ran, while its code was mutated.

Proven rather than assumed. Removing `RefuseWhenArchived()` from `SyncFromSource` by hand and running
`FullyQualifiedName~DeliverySourceSync` (46 specifications, both suites) fails one of them — the mutant
Stryker recorded as surviving is killed by the suite it never ran.

The filter in `stryker.5831.backend.json` is corrected to `FullyQualifiedName~DeliverySourceSync` for
the next run. This one was **deliberately not repeated** (maintainer decision): the gate is already met
at 83.20 %, the correction can only move the number up, and half an hour of wall clock to improve a
number that already passes buys nothing. The discrepancy is recorded here instead so nobody later
"fixes" a gap that is not one.

## Accepted survivors

### Two mutants that do not compile

`DeliverySourceSyncService.cs:206` — `IsPermanent`'s `is A or B or C` mutated to `is A and B and C`.
Applied by hand, the compiler rejects it: *CS8518: An expression of type
'DeliverySourceUnavailableReason' can never match the provided pattern.* A mutant the compiler refuses
is the strongest kill there is; Stryker's own report calls it survived. `:109` — block removal on the
read-failure catch — is the same shape: the block carries the `return null`, and removing it leaves the
method with a path that returns nothing.

### The null guards, again

`DeliverySourceSyncService.cs:16-17` and `Delivery.cs:202` — `ArgumentNullException.ThrowIfNull`.
Left open for the same reason as slice 02: this interface has one caller that cannot pass null, and a
test proving `ThrowIfNull` throws asserts what the runtime already promises while reading as coverage of
this slice's behaviour.

### Frontend — one styling mutant

`sx={{ borderRadius: 0 }}` → `sx={{}}` on the notice. jsdom neither resolves the theme nor gives the two
a different generated class, so no component test in this project can see it — the same limit that made
the delivery date's colour unpinnable in slice 02.

### Pre-existing `Delivery.cs` surface

The remaining eleven survivors sit on lines this slice never touched — the projection helpers and
forecast arithmetic around L322-L495, and the parameterless-constructor and `TeamsWithoutForecast`
lines at L37 and L68. Recorded rather than closed: closing them means writing tests for other slices'
behaviour under this slice's name.

## What the run changed

One diagnostic string in `MarkSourceUnavailable` is now marked `Stryker disable once all` with its
reason — the refusal is the behaviour and is asserted; the sentence explaining it is read by whoever is
debugging the caller.

Two frontend gaps the run did find and that were closed with tests: a source label that is only
whitespace printed as a gap in the middle of the sentence, and the fallback branch for a reason this
build has never heard of was reachable in production — the delivery payload is not schema-validated on
the way in and the backend enum is append-only — and reachable by no test at all.

## What the run did not find

No backend survivor pointed at a missing behavioural test. Everything that mattered in this slice was
found by adversarial review before this run: three integration scenarios that passed with the whole
transition table deleted, an AC-04.5 guard that measured nothing because the layer beneath it threw and
this one swallowed, and a self-satisfying date assertion. All were closed in `6de56fe0a`.
