# DISTILL — slice 01, every event says its source (US-01)

Repository `/storage/repos/Lighthouse`, commit `42bcd476c`. NUnit 4.6 over `WebApplicationFactory`, the
usage data observation base `UsageDataCollectorObservationTest` (the collector host is answered locally;
`EverythingTheCollectorReceived` pumps the drain the integration host does not run).

## Files

| File | Pending `[Ignore]` | Active |
|---|---|---|
| `Lighthouse.Backend.Tests/Integration/UsageData/Story6193EveryEventSaysItsSourceScenarios.cs` | 18 | 6 |
| `…/Story6193EveryEventSaysItsSourceSpecifications.cs` | helpers only | — |
| `…/UsageData/Fixtures/batch-lh-sends-for-a-vote-that-made-ready.json` | contract fixture | — |
| `…/UsageData/Fixtures/state-a-client-reads-after-its-grant.json` | contract fixture | — |

Categories: `acceptance`, `epic-5733-opt-in-usage-data`, `story-6193-usage-data-from-clients`. The two
fixtures are byte-identical (same SHA-256) to `lighthouse-clients/test-support/usageDataContract/`; the
scenarios read them from the source tree, so the test project file is unchanged (a project-file edit would
force the full integration suite in CI).

## Scenarios

Pending until DELIVER slice 01:

- A browser batch that names no source (left out, or `null`) reaches the collector labelled `Browser`.
- A client batch reaches the collector labelled with the source it declared (`Cli`, `Mcp`); a source
  named in lower case is read as the source it names.
- Every message one batch becomes carries that batch's source.
- `@error` A source outside the three is refused with 400 and nothing reaches the collector: `Shell`, `7`,
  `""`, `true`, `["Cli"]`.
- The state tells any caller which sources this Lighthouse labels (`acceptedSources`), for an agreed
  browser, a browser never asked, and a vetoed instance; it adds that field and nothing else.
- Contract: the batch `lh` sends for a vote that made GR-061 Ready is taken in and forwarded as `lh`
  declared it; the state a client reads after its grant is, field for field, the state the clients parse.
- The usage data docs page lists the source among what every event carries.

Active guards (green today, green on every slice):

- Today's browser batch is still taken in and forwarded.
- `@security` The administrator's stop holds whatever the source (`Cli`, `Mcp`).
- `@security` A client without a live grant sends nothing, whatever source it declares (no token, a
  token never minted, a withdrawn token).

Every forwarded message is asserted on its closed fields only: event name, `source`, `sizing_moment`.

## DELIVER order

Source on the batch → collector label → refusal → `acceptedSources` on the state → the two contract
scenarios → the docs page row. `UsageDataEventPipeTests.EverythingTheStateAnswerCarries` pins today's five
state fields and has to gain `acceptedSources` in the same step.
