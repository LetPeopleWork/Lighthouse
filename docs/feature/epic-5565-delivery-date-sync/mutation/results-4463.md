# Mutation testing — 4463 (Publish the forecast to the Release)

Run 2026-08-25 against `main` at `b373685f6`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.83 %** | 81 | 74 | 5 | 2 | 16 m 46 s |
| Frontend (StrykerJS) | **98.67 %** | 150 | 148 | 2 | 0 | 1 m 35 s |

Config: `stryker.4463.backend.json`, `stryker.4463.frontend.json` with `mutation-vitest.4463.config.ts`.

## The first run failed the gate, and what it found was real

The first backend run scored **78.31 %** — 63 killed, 18 survived, 2 timed out of 83. Nine of the
eighteen survivors sat on one expression: the two-character carriage-return separator in
`DeliveryForecastBlockRenderer.LinesOf`. Nothing exercised it by accident, because every fixture in this
slice separates its lines with whatever the build host uses and the build host is Linux.

It is not a theoretical branch. A description pasted out of a Windows editor separates its lines with
`\r\n`; read as two separators rather than one, every line lands one apart from where it is, the block
Lighthouse wrote stops being findable in its own text, and every publish appends — which is the
description-spam failure the markers exist to prevent. A blank line is the same shape (two separators,
not one long one) and a person leaving a blank line above their notes is the ordinary case.

Three specifications closed it: a description written with carriage returns, a description with a blank
line in it, and one ending in a bare carriage return. The score moved to 93.83 % on the re-run.

## Accepted survivors — backend

**Two message strings.** The `ArgumentException` text on a forecast carrying no percentiles, and the
`"0"` format on the likelihood. The first is a sentence for whoever is debugging the caller, not
behaviour. The second is an equivalent mutant: .NET reads an empty format string as the general format,
so `88.0.ToString("")` and `88.0.ToString("0")` produce the same characters.

**Three catch bodies.** Stryker empties the whole `catch { … }` block on `PublisherBehind`,
`WriteWithoutTakingTheRoundDown` and the handler's outer catch. Everything inside each of them is a log
line; what the branch *does* — publish nothing, leave this Delivery as it stands while the ones beside
it are still published, let the round finish — is asserted by name in the fixtures. The statements are
already marked `Stryker disable all` with that reasoning; the block node itself sits outside the marker.

**Two timeouts, both in the line reader.** Removing `index++` and turning `index += separatorLength`
into `index -= separatorLength` both produce a loop that never ends. A timeout is the suite noticing, not
a gap.

## Accepted survivors — frontend

Both are the selection-tab key constants, `MANUAL_SELECTION_TAB_KEY` and
`RULE_BASED_SELECTION_TAB_KEY`, mutated to the empty string. The constant is the only way any code names
that tab, so changing it changes the definition and every use together and nothing observable moves.
Equivalent mutants.

## Scope

Backend mutated `DeliveryForecastBlockRenderer.cs`, `DeliveryForecastPublishingService.cs` and
`DeliveryForecastPublishingHandler.cs`, with the test filter narrowed to the four fixtures that cover
them — the renderer specifications, the publishing service, the handler, and the through-the-refresh
acceptance scenarios. Frontend mutated `deliverySelectionTabs.ts`; the publishing switch itself is a
presentational component whose behaviour is the callback it raises, and that is covered by
`DeliverySourceTab.publishForecast.test.tsx` rather than by mutating JSX.
