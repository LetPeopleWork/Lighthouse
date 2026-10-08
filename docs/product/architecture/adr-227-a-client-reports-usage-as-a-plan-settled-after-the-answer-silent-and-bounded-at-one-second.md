# ADR-227: A client reports usage as a plan its command returns and a reporter settles after the answer — silent, credential-free and bounded at one second

- **Status**: Proposed (2026-10-08, DESIGN wave for ADO Story #6193). Interaction mode = **propose**, maintainer AFK.
- **Feature**: `story-6193-usage-data-from-clients` — repo `lighthouse-clients` (packages `client`, `cli`, `mcp-core`)
- **Relies on**: ADR-225 (wire), ADR-226 (store), ADR-223 (readers and rules restated in `client`).

## Context

A usage-data send must never change a command's output in any format, its exit code or a tool's result, never print,
and never make a command wait more than one second (D10). `lh` asks its question **after** the first successful
command's output (M1, A3), and the command that prompted is not reported (D5). Events are reported only after
Lighthouse answered the action successfully (D8). `--json` and `--toon` are the CLI's contract (story #6218) and must
stay byte-identical. On `mcp-stdio`, stdout *is* the protocol stream, so a stray print breaks the server.

`runCliCommand` already returns a pure `CliCommandResult` (`exitCode`, `stdout`, `stderr`) that `bin.ts` prints. The
MCP tools return `McpToolResult` from `callTool`, and `registerMcpTools` hands it to the SDK.

## Decision

1. **The vocabulary and the rules live once, in `client/src/usageData.ts`, as pure code**: the client's event names
   (a closed string union of the 10 mapped `UsageDataEventName` members), the property unions (`sizingMoment`,
   `refinementVerdict`), `UsageDataSource`, a `UsageDataOccurrence` type that admits no free string, the web's
   `sizingMomentOf` and Refinement-day verdict rule restated with parity tests against the web's own cases, the
   `DO_NOT_TRACK` reading, and **`planUsageDataStep(facts) → plan`**: given the store entry, `state`, the source,
   whether the run may prompt, and what the command did, it returns one of *nothing*, *ask*, *confirm-then-send* or
   *send*. Every branch is a table test.
2. **The effects live in `client/src/usageDataReporter.ts`**: it executes a plan (state read, silent re-grant, send),
   reads and writes the ADR-226 store, and **never throws, never logs, never prints**. It returns an outcome value
   that tests and `lh config usage-data` read. Four `LighthouseClient` methods back it (`getUsageDataState`,
   `grantUsageData`, `revokeUsageData`, `handInUsageData`), on the existing request helpers with an `AbortSignal`
   threaded through `RequestOptions` and the connectivity check. They are not version-gated; ADR-225's
   `AcceptedSources` is the gate.
3. **One second, total, by one `AbortSignal`**, covering everything a send needs: the connectivity check, a `state`
   read, a re-grant and the post. On abort or any failure the reporter gives up silently; nothing is retried, and the
   events are lost, which costs nothing (ADR-190 §3's reasoning).
4. **No credential on usage-data calls.** The consent and ingest endpoints are anonymous (`[AllowAnonymous]`), so the
   reporter builds its requests with no API key and no bearer token. Usage data is never bound to an account, and on
   `mcp-http` a caller's token or the server's own key never travels for it.
5. **The CLI returns a plan value, and `bin.ts` settles it after printing.** Each group handler attaches
   `usage: { reached, occurrences }` to its `CliCommandResult`: `reached` is true when a Lighthouse call answered
   successfully; `occurrences` are the D8 events, computed from the answers the command already holds. `stdout`,
   `stderr` and `exitCode` are produced exactly as today, so every format's bytes are unchanged by construction.
   `config`, `connection` and help never set `reached`, so they never ask. `bin.ts` prints, then awaits the
   reporter, then returns the command's own exit code.
6. **The question goes to stderr, and only on a full terminal.** `lh` may ask only when stdin, stdout **and** stderr
   are TTYs and `CI` is unset. The prompt is written with `readline` on stderr, so stdout is the command's answer and
   nothing else even in a terminal recording. Ctrl-C and end of input during the question are **no answer**: the
   question's own handler catches them, nothing is recorded, and the process exits with the command's exit code.
   The state read before the question shares the one-second budget (a slow Lighthouse means no question this time);
   the grant after a yes has five seconds, because the person is waiting for it, and on failure prints the journey's
   one line on stderr. The run that asked reports nothing.
7. **A vote's moment comes from the read the vote already makes.** `lh refinement vote` and the MCP vote tool read
   the Team's Refinement before every vote (to learn `voterIdentity`). That read carries `nextRefinementDate` and
   `isRefinementDay`, so the moment is taken from it: no extra read, and a failed read already fails the vote, so a
   vote with no moment is never reported.
8. **MCP sends after the result is returned.** `registerMcpTools` hands the tool's occurrences to an optional
   usage-data port after `callTool` and returns the result without awaiting the send; the MCP processes outlive the
   call, so the send completes (or aborts at one second) in the background, and a tool result is never delayed by
   sending. With no port supplied, behaviour is exactly today's. The question (elicitation) is ADR-228.

## Alternatives considered

- **Report inside each group handler, before returning.** Rejected: the question must come after the output, and a
  handler cannot print first; it would also mix effects into code that is pure today.
- **Prompt on stdout.** Viable, since stdout is a terminal whenever the question is asked. Rejected: stderr is where
  interactive tools put questions, it keeps stdout strictly the answer, and requiring stderr to be a terminal too
  removes the one case where the question could wait unseen (`2>/dev/null`).
- **Open `/dev/tty` directly.** Rejected: not portable to Windows and to the Bun-compiled binaries.
- **Fire-and-forget in `lh` too.** Rejected: the process exits when the command returns, so an unawaited send is
  simply cut off; awaiting a bounded send is the honest version.
- **Reuse the server-version gate.** Rejected in ADR-225.

## Consequences

- Positive: output bytes and exit codes are unchanged by construction rather than by care, and a test can pin them
  per mapped command with usage data on and off.
- Positive: the rules are one pure function with a table of cases; the shell around it is small and injected, as the
  rest of the clients are (`RunCliCommandDependencies`, `McpCoreRuntimeDependencies`).
- Negative: a consenting `lh` run may take up to one second longer to exit when Lighthouse is slow or gone, and each
  send costs up to three requests (connectivity check, at most one `state` read a day, the post).
- Negative: `CliCommandResult` gains a field that is never printed. Every handler that reaches Lighthouse must
  set it; a characterisation test lists the mapped commands and fails on a new one that forgets.
- Earned Trust: a fake Lighthouse that accepts the connection and never answers must leave the command's exit within
  one second plus scheduling slack; a fake terminal that reports `isTTY` on stdin only must never be asked; Ctrl-C at
  the question must leave exit code 0 after a successful command.
