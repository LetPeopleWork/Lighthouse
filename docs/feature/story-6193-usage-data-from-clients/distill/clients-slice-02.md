# DISTILL — slice 02, lh asks once (US-02, US-03)

Repository `/storage/repos/lighthouse-clients`, commit `96df092`. `lh` runs as one process through the
scaffold `runCliSession(args, io, { env, terminal, now })` (`packages/cli/src/cliSession.ts`), against
`aFakeLighthouse` on a real loopback port. The harness `packages/cli/test-support/lhSession.ts` gives each
scenario its own home (`aMachine`, under `aTempDirectory`), an explicit `CI` / `DO_NOT_TRACK` / `HOME`, a
terminal shape, what the person types and when, and a pinned clock.

## Files

| File | Pending | Active |
|---|---|---|
| `packages/cli/src/usageDataQuestion.test.ts` (US-02) | 61 | — |
| `packages/cli/src/usageDataConfig.test.ts` (US-03) | 27 | — |
| `packages/cli/src/usageDataGuards.test.ts` | — | 8 |

## Scenarios

`usageDataQuestion.test.ts`:

- Lena is asked once, after her forecast, on stderr, in the approved words, and her yes is kept for that
  Lighthouse; her next forecast is reported as `TeamManualForecastRun` from the command line and she is
  never asked again there.
- Yes / no / Ctrl-C: every accepted yes and no spelling; a No is final, kept and never posted; Ctrl-C
  leaves the question unanswered and it is asked on the next command.
- stdout is exactly what the forecast prints with nobody asked; the consent token is never shown.
- Where it may ask: only with all three terminals and `CI` unset; `DO_NOT_TRACK` set (`1`, `true`, `TRUE`,
  `yes`) asks nothing and makes no request; `0`, `false`, `""` are not a request to stop.
- `@version-skew` / `@error` A vetoed, young, predating, Cli-less, usage-data-less or failing Lighthouse:
  nothing asked, nothing sent, nothing extra printed. A Lighthouse that never answers about usage data:
  nothing asked this time, asked the next time, all within 2.5 s.
- No question after help, the output format setting, the config group, the connection status, or a
  refused command.
- A second Lighthouse is asked about separately; the first one's answer is left as it was.
- `@infrastructure-failure` A yes Lighthouse could not record: `Could not record your answer at <url>;
  nothing is sent. Try lh config usage-data on.`, nothing kept, the forecast's exit code untouched.
- Two answers at once: the first one given is the one kept.
- The answers file is readable by its owner only, beside the command line's config, and leaves the voter
  keys alone (pending as `it.skip`; DELIVER un-skips it as `it.skipIf(win32)`); a file it cannot read is
  left as it was, and nothing is asked or sent.
- Liveness: a yes confirmed 23 h ago sends without a state read; a day-old yes is checked, then sends; a
  grant Lighthouse let lapse after 31 days is renewed without asking again; a rollback to a Lighthouse that
  cannot label `lh` sends nothing; Marco's No on file sends nothing whatever he runs.
- `@kpi` The forecast prints in `--json`, `--toon`, `--pretty` exactly as with usage data off and waits at
  most a second for a Lighthouse that never takes the event.

`usageDataConfig.test.ts`:

- Status: the answer line and the instance line in each state (allows, stopped, predates, could not ask),
  `DO_NOT_TRACK` named when in force.
- `@error` An unreadable answers file is refused, naming the file, exit 1, file untouched (AFK copy
  default). Not connected → today's connect hint. Any other argument → the group's help, exit 1. The group
  help lists the command.
- `off` withdraws the grant at Lighthouse, forgets the token, sends nothing afterwards; offline it still
  turns off and says the yes lapses by itself (AFK copy default); it works before anyone asked.
- `on` switches Sofia's build agent on (no question, later forecasts reported as `Cli`), replaces an
  earlier No, records nothing under the veto (exit 0), under `DO_NOT_TRACK`, or against a predating
  Lighthouse; a Lighthouse that cannot be asked records nothing and exits 1 (AFK default).
- `@security` The token is never shown by status, `on` or `off`.

`usageDataGuards.test.ts` (active now, through today's `runCli` and the built bin):

- A CI run that never said yes makes no usage data request and asks nothing (in-process, and the built
  `lh` as a child process when `dist` exists).
- `--json` / `--toon`: the same bytes and exit code with and without a stored yes.
- The consent token is never printed by a forecast (`--json`, `--pretty`), `config usage-data` or
  `connection status`.

## DELIVER order

Replace `runCliSession`'s body (remove `__SCAFFOLD__`), then un-skip `usageDataQuestion.test.ts` top to
bottom, then `usageDataConfig.test.ts`. The guards stay active throughout; `runCli` has to become
`runCliSession` with the process's environment, terminal and clock, and the guards must still pass.

## Note for DELIVER

27 cases in `usageDataQuestion.test.ts` are claims of absence (asks nothing, sends nothing, leaves the
file as it was). They are red today only because the scaffold throws; once `runCliSession` exists they
pass on day one. Un-skip each next to the positive scenario in the same describe that proves the question
or the send happens, never alone. Every `usageDataConfig.test.ts` case is red on missing behaviour even
with the scaffold replaced.
