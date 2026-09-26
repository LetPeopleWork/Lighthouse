---
description: AFK mode — drive a feature autonomously through DESIGN → DEVOPS → DISTILL → DELIVER, then refactor → adversarial review → mutation, and HOLD. Stops only for a decision that genuinely needs the maintainer. Never pushes. Stays on until explicitly switched off.
argument-hint: "[feature-id] | off | status"
---

# /afk — carry on without me

The maintainer is away from the keyboard. Drive the named feature forward on your own, as far as it
can safely go, and leave a clean trail. **Stopping is the exception, not the default**; **pushing is
never done in this mode.**

## Arguments

Arguments for this run: `$ARGUMENTS`

- `/afk <feature-id>` — enter AFK mode for `docs/feature/<feature-id>/` and start (or resume) the run.
- `/afk` with no id — resume the feature named in the marker; if there is none, detect the single
  in-flight feature the way `nw-continue` does. If that is ambiguous, that is a stop (see below).
- `/afk status` — print the marker and the next action. Changes nothing.
- `/afk off` — leave AFK mode (see "Leaving AFK mode").

## The marker — AFK mode is state, not a mood

On entry write `.nwave/afk.json` in the project root (the `.nwave/` directory is gitignored, so it can
never be committed):

```json
{ "feature": "<feature-id>", "entered": "<ISO-8601>", "status": "running",
  "position": "<wave or step>", "hold_after": "mutation", "push": "never",
  "blocked_on": null, "log": [] }
```

Update `position` at every wave or step boundary and append one line to `log` per meaningful event
(wave done, gate result, decision taken, commit). Read the marker at the start of **every** turn while
it exists: if it exists and says `running` or `blocked`, you are in AFK mode, whatever the new message
says.

## Leaving AFK mode — only on an explicit word

The maintainer often answers from a phone while still busy. **A reply is not a return.** Stay in AFK
mode after any message except an explicit exit: `/afk off`, "exit afk", "leave afk", "stop afk",
"I'm back". Anything else — "ok", "yes", an answer to a question, a new instruction — is remote input:
apply it, log it, and keep going. When genuinely unsure whether a message means "I'm back", assume it
does not and say in one line that AFK mode is still on and how to leave it.

On exit: set `status` to `off`, summarise where things stand, and delete nothing.

## The sequence

Resume at the first incomplete item. Skip nothing silently; a wave whose artifacts already exist and
are reviewed is complete.

1. **Precondition — DISCUSS must be done.** AFK mode starts at DESIGN. DISCUSS is the maintainer's
   conversation (framing, stories, what the user sees); if the feature has no DISCUSS artifacts, stop.
2. **DESIGN** (`nw-design`), in *propose* mode: take the recommended option unless it changes what a
   user sees or is told. Record every choice in `feature-delta.md` as the wave expects.
3. **DEVOPS** (`nw-devops`), including the project's own checklist items (e.g. the usage-data event
   question in Lighthouse's `CLAUDE.md`).
4. **DISTILL** (`nw-distill`), including its mandatory multi-reviewer gate.
5. **DELIVER** (`nw-deliver`): roadmap → roadmap review → every step through DES → integrity check.
   Stop at the end of the steps — **do not** run DELIVER's own finalize or push.
6. **Quality gates, in this order**:
   1. **Refactor** (L1–L6, `nw-refactor`).
   2. **Adversarial review** — on a strong model, never a lightweight one; demand a verbatim quote for
      every cited line and spot-check any citation before acting on it. One revision pass for its
      findings; blockers must be fixed and proven (a test that fails before the fix and passes after).
   3. **Mutation testing** (`/mutation-testing`) — ≥ 80 % kill rate on each stack the feature touched.
      Kill real survivors with tests; justify equivalent ones in writing.
7. **HOLD.** Write the summary (what shipped locally, gate numbers, decisions taken, open questions,
   commits ahead of origin), set `status: "holding"`, send one push notification, and do nothing more.
   Do not finalize, do not push, do not start the next feature.

Wave-defined reviewers and gates run without asking — they are part of the wave, not an extra.

## When to stop — and then really stop

Stop only when continuing would mean guessing something that is the maintainer's to decide:

- A **product decision with no safe default** — it changes what users see, are told, or can do, and
  the artifacts do not settle it. (Engineering choices with a clear recommended option are not this:
  take the recommendation, record it, carry on.)
- A **contradiction between waves** the artifacts cannot resolve, or a DISTILL reconciliation gate
  that returns CLARIFICATION_NEEDED on a product question.
- A **gate still red after one revision cycle** (two for DISTILL's review), or the same failure twice.
- **Outward or irreversible action needed**: a push, a force/rebase, creating/removing/closing tracker
  items beyond the `/ado-sync` auto-transition rules (Active on start, Resolved on push, Closed on CI green), touching shared infrastructure.
- **Trunk moved underneath you** in a way that needs a rebase or merge — report, never resolve it
  yourself.
- Missing environment: credentials, a service that will not start, a tool that is not installed.
- Scope is ambiguous: no feature named and more than one in flight.

To stop: write the question as a short, self-contained message (the options, your recommendation, and
what happens under each), set `status: "blocked"` and `blocked_on` in the marker, send one push
notification, and **end the turn**. No further writes, dispatches or "while I wait" work. When the
answer arrives, record it and resume from `position` — still in AFK mode.

## Hard rules for the whole run

- **Never push**, never `git pull --rebase`, never force anything. Local commits are expected; say how
  many are ahead of origin at the hold.
- Commit at every step boundary through the project's conventions (DES `des-commit` inside DELIVER;
  conventional commits; the project's trailers). Write multi-line commit messages to a file and use
  `git commit -F` — inline `-m` can be rewritten by hooks.
- **Wait for each agent's completion notification** before dispatching the next agent that touches
  the same files or runs the same build — a hand-back message is not completion. Run in parallel only
  when the work is on separate stacks and builds cannot collide.
- After every agent: verify its claims that matter (`git log`, `git status`, the test numbers it
  quotes against a re-run when in doubt). Never report a result you have not seen.
- Follow `CLAUDE.md`, `docs/ci-learnings.md` and the project memory; they override this command
  where they are more specific.
- Keep the user-visible output lean: one or two lines per milestone, not per tool call.

## At the hold — what the summary contains

1. Waves and gates completed, with the key numbers (tests, mutation score per file, review verdict).
2. Decisions taken autonomously that the maintainer may want to revisit, each in one line.
3. Anything deferred or found in passing (flaky tests, stale docs, follow-up tickets worth raising).
4. `N commits ahead of origin, not pushed` — and the exact next action on their word ("push", or
   "finalize").
