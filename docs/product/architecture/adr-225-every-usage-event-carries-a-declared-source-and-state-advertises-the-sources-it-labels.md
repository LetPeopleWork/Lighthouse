# ADR-225: Every usage event carries a declared `source`, read once per batch from the body, and `state` advertises the sources this Lighthouse labels

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO Story #6193). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6193-usage-data-from-clients` — repo Lighthouse (backend), consumed by `lighthouse-clients`
- **Amends**: [ADR-190](./adr-190-usage-data-events-detected-in-the-browser-forwarded-by-the-backend.md) ("nothing but
  our own page posts here" becomes "our own page and our own clients") and
  [ADR-191](./adr-191-analytics-identity-is-a-per-browser-pseudonym-never-on-the-wire.md) (the counted unit is a
  *consenting client*, no longer only a browser). Neither is rewritten; each carries a one-line pointer here.
- **Relies on**: ADR-216 §4 (a declared, unverified channel enum is analytics, not a security boundary).

## Context

Story #6193 lets `lh` and the two MCP servers send the events the web sends. Once they do, a
`TeamManualForecastRun` from `lh` and one from the Forecast tab are indistinguishable, and the browser's numbers are
inflated by the clients'. The maintainer decided (M10) that every event carries `source` = `Browser` | `Cli` | `Mcp`,
attached server-side from what the client declares, as a closed value.

Two constraints shape the wire:

- **Today's web must keep working unchanged**, including a tab still holding an old bundle after an upgrade. It sends
  no `source`.
- **A Lighthouse that predates this story must never receive a client event.** System.Text.Json ignores a property it
  does not know, so an older server would read a client batch without its `source` and count it as a browser one
  (DISCUSS S15). Only the server can say whether it labels sources, and it must say so before a client asks anyone.

## Decision

1. **A closed enum `UsageDataSource { Browser = 0, Cli = 1, Mcp = 2 }`** in `Models/UsageData`, append-only like
   `UsageDataEventName` (next integer, never renumbered). The usage data page lists every member; the disclosure test
   that already pins the event and property lists pins this one too.
2. **The batch body gains one optional field, `Source`** (`UsageDataSource?`, nullable per `csharpsquid:S6964`, never
   `[JsonRequired]`). Absent or `null` means `Browser`. A number that is not a member is refused `400` at the read
   site; a string that is not a member fails binding and is refused `400` by `[ApiController]`. That is the existing
   "unreadable body" rule, checked before the gate exactly as the events are, so a bad `source` never reaches the queue.
   Names arrive as strings from the clients and are read case-insensitively by the global `JsonStringEnumConverter`.
3. **`source` travels on the batch, not on each event and not with the instance facts.** `AcceptedUsageDataBatch`
   gains the resolved `UsageDataSource` beside its token and events. A batch comes from one client, so a per-event
   field would only add a way for two events of one batch to disagree. It is not an instance fact either:
   `UsageDataInstanceProperties` describes the server, and this describes the caller.
4. **The publisher writes `source` on every message**, always present, as the member's name. It is never omitted:
   an event without it would be read as unknown, which is the split this story exists to make.
5. **`GET state` gains `AcceptedSources`**: the list of `UsageDataSource` members, serialised as names, identical for
   every caller and derived from the enum itself so a future member is advertised by the same change that adds it. A
   client asks and sends only when the list contains its own source. A response without the field (an older server)
   or a `404` (a server with no usage data at all) means never ask and never send.
6. **Declared, not verified.** A hand-written request can claim any of the three, as ADR-216 §4 says of vote channels.
   No control depends on `source`: the gate, the rate limiter, the daily budget and the veto treat every source alike.

## Alternatives considered

- **A request header (`X-Lighthouse-UsageData-Source`)**. Viable. Rejected: it would be a second place a closed
  choice travels as a string, outside the body the controller already checks member by member, and it would need its
  own `400` path.
- **One route per source (`/events/cli`)**. It has one real advantage: an older server answers `404`, so it could
  never count a client event. Rejected, because the client still needs the `state` signal before it *asks* anyone,
  so the route would only duplicate that guarantee, and it multiplies the ingest wiring (rate-limit policy, gate,
  tests) by three.
- **A boolean `LabelsSources` on `state`**. Viable and smaller. Rejected for the list, which answers "is *my* source
  accepted" for any future source without a second flag.
- **Server-version gating in the client** (`FEATURE_REQUIRES_SERVER_NEWER_THAN`). Rejected: a version the client
  cannot parse (a dev build, a fork) is treated as "do not block", which here means sending to a server that may not
  label it. A capability the server states about itself cannot be wrong that way.
- **Infer the source from `User-Agent`**. Rejected: free text, and the web's agent string would have to be parsed.

## Consequences

- Positive: 100% of forwarded events carry `source` (KPI-1); the web and every old bundle are counted correctly with
  no frontend change; a client can never count itself as a browser on a server that cannot label it.
- Positive: the DTO still has no free-text field, so ADR-190 §1's "unrepresentable" claim holds unchanged.
- Negative: `state` discloses one more fact to an anonymous caller. It is a version fact (the same list on every
  instance of a release), names no tier and no person, and is no more than `/version/current` already reveals.
- Negative (residual, stated): after an **image rollback** to a release without this change, a client that confirmed
  the signal within the last day may send for up to 24 hours before its next `state` read (ADR-226 §5), and those
  events would be counted as `Browser`. Rollbacks are rare and short; making the window zero would cost a `state`
  read on every command.
- The usage data page gains the `Source` row (slice 01) and the browser-only promises are rewritten in the slice that
  first makes them false (slices 02 and 05).
