# Story 6193 — backend mutation results (slice 01)

- Tool: Stryker.NET, config `stryker.6193.backend.json` (this folder), run 2026-10-08 from
  `Lighthouse.Backend/Lighthouse.Backend.Tests`, 2 m 30 s.
- Scope: the four files slice 01 changed, whole files (Stryker.NET ignores line spans). Tests: the unit
  namespaces `Tests.Services.Implementation.UsageData` and `UsageDataControllerTests`; the
  WebApplicationFactory acceptance suites are excluded on purpose (they turn a 3-minute run into an hour).

## Headline: 72.63 % (69 killed / 12 survived of 81 tested, plus no-coverage)

The headline measures the whole of each file, most of which predates this story. What this story
added scores **100 %**:

| Line(s) added by this story | Mutants | Result |
|---|---|---|
| `UsageDataController.cs:166` — `batch.Source ?? UsageDataSource.Browser` | 1 testable | Killed |
| `UsageDataController.cs:140-141` — refuse a source outside the three (`!Enum.IsDefined`) | Stryker produced only compile errors (pattern variables) | Probed by hand: flipping the negation fails 4 of 13 `UsageDataControllerTests` → killed |
| `PostHogUsageDataPublisher.cs:211, 273` — write `source` on every message | none generated (a property argument and an attribute) | Pinned by `UsageDataPublishedMessageTests.EveryMessage_SaysWhichSourceItsBatchCameFrom` |
| `UsageDataConsentService.cs:33-36, 79-80` — `AcceptedSources` from the enum's names | none generated (a static field initialiser) | Pinned by `GetState_AcceptsEverySourceThisVersionKnows` |
| `UsageDataEventBatchDto.cs` — optional `Source` | none generated (a record) | — |

## Survivors and no-coverage (all in code this story did not change)

- `PostHogUsageDataPublisher.cs:193` (5) — the fallback to the shipped PostHog key when none is named;
  `:213` GeoIP flag; `:223` a block; `:287-296` DI registration (4). Covered only by the acceptance and
  composition tests excluded from this run.
- `UsageDataController.cs:52, 88-90, 116-118` (no coverage) and `UsageDataConsentService.cs:116, 126`
  (no coverage) — the revoke and "asked" endpoints, exercised only by the acceptance suites
  (`UsageDataEventPipeTests` and friends).
- `UsageDataController.cs:216` — `Fits(taken) ? taken : null` forced to `taken`: the shape check is also
  enforced by the gate the acceptance suite drives.

None of these is in scope for story 6193; they are listed so the headline is not read as weak testing
of the new code.
